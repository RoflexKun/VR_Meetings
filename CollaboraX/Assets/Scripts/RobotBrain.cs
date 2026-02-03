using UnityEngine;
using UnityEngine.InputSystem; 
using Meta.WitAi.TTS.Utilities;
using Oculus.Voice;
using UnityEngine.Networking;
using System.Text;
using System.Collections;
using TMPro; 
using Unity.Services.Vivox;
// 1. ADD THIS to access the Mic script
using Meta.WitAi.Lib; 

public class RobotBrain : MonoBehaviour
{
    [Header("Conflict Fix")]
    [Tooltip("Drag the 'Mic' component from this object here")]
    public Mic witMicComponent; // <--- NEW SLOT

    [Header("Robot Parts")]
    public AppVoiceExperience ears;
    public TTSSpeaker mouth;

    [Header("UI Feedback")]
    public TextMeshProUGUI subtitleText; 
    public float textDisplayTime = 3.0f; 

    [Header("VR Input")]
    public InputActionProperty talkButton; 

    [Header("AI Personality")]
    [TextArea(3, 10)]
    public string systemPrompt = "You are a helpful teaching assistant. Keep answers short.";

    private bool isProcessing = false;
    private bool isHoldingButton = false;
    private bool wasVivoxMutedBefore = false;

    private void OnEnable()
    {
        ears.VoiceEvents.OnFullTranscription.AddListener(OnFullTranscription);
        ears.VoiceEvents.OnPartialTranscription.AddListener(OnPartialTranscription);
        ears.VoiceEvents.OnError.AddListener(OnWitError);
    }

    private void OnDisable()
    {
        ears.VoiceEvents.OnFullTranscription.RemoveListener(OnFullTranscription);
        ears.VoiceEvents.OnPartialTranscription.RemoveListener(OnPartialTranscription);
        ears.VoiceEvents.OnError.RemoveListener(OnWitError);
    }

    void OnWitError(string error, string message)
    {
        Debug.LogError($"❌ [WIT ERROR] {error}: {message}");
        UpdateSubtitle("Error: " + error, Color.red);
        RestoreVivox();
    }

    void Update()
    {
        if (talkButton.action.WasPressedThisFrame() && !isProcessing)
        {
            isHoldingButton = true;
            StartCoroutine(KickstartListening());
        }

        if (talkButton.action.WasReleasedThisFrame())
        {
            isHoldingButton = false; 
            Debug.Log("🛑 [RELEASE] Button released.");
            ears.Deactivate(); 
            RestoreVivox();
        }
    }

    // --- THE KICKSTART FIX ---
    private IEnumerator KickstartListening()
    {
        Debug.Log("🔄 [1/4] Starting Audio Handoff...");

        // 1. Mute Vivox
        if (VivoxService.Instance != null && !VivoxService.Instance.IsInputDeviceMuted)
        {
            wasVivoxMutedBefore = false;
            VivoxService.Instance.MuteInputDevice();
        }
        else wasVivoxMutedBefore = true;

        // 2. FORCE KILL the Microphone driver
        // This breaks Vivox's "Exclusive Mode" grip on the hardware
        Microphone.End(null);
        
        yield return new WaitForSeconds(0.1f);

        // 3. RESTART the Wit Mic
        // This forces it to re-initialize at 48000Hz immediately
        if (witMicComponent != null)
        {
            Debug.Log("🔌 [2/4] Restarting Mic Component...");
            witMicComponent.enabled = false; 
            yield return null; // Wait one frame
            witMicComponent.enabled = true; // This triggers OnEnable -> StartRecording
        }

        yield return new WaitForSeconds(0.2f); // Wait for recording to stabilize

        // 4. Activate Wit
        if (isHoldingButton)
        {
            Debug.Log("🎤 [3/4] Mic Fresh & Ready! Activating Wit...");
            ears.Activate(); 
            UpdateSubtitle("Listening...", Color.yellow);
        }
        else
        {
            RestoreVivox();
        }
    }

    private void RestoreVivox()
    {
        if (!wasVivoxMutedBefore && VivoxService.Instance != null)
        {
             // Give the mic back to Vivox
             VivoxService.Instance.UnmuteInputDevice();
        }
    }

    // --- STANDARD RESPONSE CODE ---
    private void OnPartialTranscription(string text) { UpdateSubtitle(text + "...", Color.cyan); }

    private void OnFullTranscription(string text)
    {
        if (string.IsNullOrEmpty(text)) { RestoreVivox(); return; }
        if (isProcessing) return;

        Debug.Log($"📝 [FINAL] Message: '{text}'");
        UpdateSubtitle(text, Color.green);
        StartCoroutine(HideSubtitleAfterDelay());
        StartCoroutine(AskGemini(text));
    }

    private IEnumerator HideSubtitleAfterDelay()
    {
        yield return new WaitForSeconds(textDisplayTime);
        if (!isHoldingButton) subtitleText.text = "";
    }

    private void UpdateSubtitle(string msg, Color color)
    {
        if (subtitleText != null) { subtitleText.text = msg; subtitleText.color = color; }
    }
    
    // --- GEMINI ---
    private IEnumerator AskGemini(string userMessage)
    {
        isProcessing = true;
        string url = "https://generativelanguage.googleapis.com/v1beta/models/gemma-3-27b-it:generateContent?key=" + ApiKeys.Gemini_Key;
        
        GeminiRequest requestData = new GeminiRequest();
        requestData.contents = new GeminiContent[] { new GeminiContent { parts = new GeminiPart[] { new GeminiPart { text = systemPrompt + "\n\nUser says: " + userMessage } } } };

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestData));
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ [GEMINI ERROR] {request.error}");
                mouth.Speak("Connection error.");
                isProcessing = false; 
            }
            else
            {
                GeminiResponse responseData = JsonUtility.FromJson<GeminiResponse>(request.downloadHandler.text);
                if (responseData.candidates != null && responseData.candidates.Length > 0)
                {
                    StartCoroutine(SpeakLongText(responseData.candidates[0].content.parts[0].text));
                }
                else isProcessing = false; 
            }
        }
    }
    
    private IEnumerator SpeakLongText(string fullText)
    {
        fullText = fullText.Replace("*", "");
        string[] sentences = fullText.Split(new char[] { '.', '?', '!', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (string sentence in sentences)
        {
            if (string.IsNullOrWhiteSpace(sentence)) continue;
            mouth.Speak(sentence.Trim());
            yield return new WaitForSeconds(sentence.Length * 0.06f + 0.1f);
        }
        UpdateSubtitle("", Color.white);
        isProcessing = false; 
    }
}

[System.Serializable] public class GeminiRequest { public GeminiContent[] contents; }
[System.Serializable] public class GeminiContent { public GeminiPart[] parts; }
[System.Serializable] public class GeminiPart { public string text; }
[System.Serializable] public class GeminiResponse { public GeminiCandidate[] candidates; }
[System.Serializable] public class GeminiCandidate { public GeminiContent content; }