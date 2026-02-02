using UnityEngine;
using UnityEngine.InputSystem; // Pentru controllere VR
using Meta.WitAi.TTS.Utilities;
using Oculus.Voice;
using UnityEngine.Networking;
using System.Text;
using System.Collections;
using TMPro; // Pentru Textul de pe ecran

public class RobotBrain : MonoBehaviour
{
    [Header("Robot Parts")]
    public AppVoiceExperience ears;
    public TTSSpeaker mouth;

    [Header("UI Feedback")]
    public TextMeshProUGUI subtitleText; // Trage textul creat aici
    public float textDisplayTime = 3.0f; // Cat timp ramane mesajul pe ecran

    [Header("VR Input")]
    // Aici vom pune butonul (ex: Grip sau Trigger)
    public InputActionProperty talkButton; 

    [Header("AI Personality")]
    [TextArea(3, 10)]
    public string systemPrompt = "You are a helpful teaching assistant in a VR classroom. Your goal is to educate the user clearly and briefly. Keep your answers UNDER 5 SENTENCES. Do not give long lectures.";

    private bool isProcessing = false;
    private bool isHoldingButton = false;

    private void OnEnable()
    {
        ears.VoiceEvents.OnFullTranscription.AddListener(OnFullTranscription);
        ears.VoiceEvents.OnPartialTranscription.AddListener(OnPartialTranscription); // Vedem in timp real ce zici
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
    }

    void Update()
    {
        // 1. Detectam cand APESI butonul
        if (talkButton.action.WasPressedThisFrame() && !isProcessing)
        {
            isHoldingButton = true;
            Debug.Log("🎤 [HOLD] Buton apasat -> Ascult...");
            ears.Activate(); // Porneste microfonul
            UpdateSubtitle("Listening...", Color.yellow);
        }

        // 2. Detectam cand DAI DRUMUL la buton
        if (talkButton.action.WasReleasedThisFrame() && isHoldingButton)
        {
            isHoldingButton = false;
            Debug.Log("🛑 [RELEASE] Buton eliberat -> Opresc si trimit...");
            ears.Deactivate(); // Opreste microfonul fortat si declanseaza procesarea
        }
    }

    // Se apeleaza continuu in timp ce vorbesti
    private void OnPartialTranscription(string text)
    {
        // Afisam ce intelege robotul in timp real
        UpdateSubtitle(text + "...", Color.cyan); 
    }

    // Se apeleaza cand ai terminat (dupa ce ai luat degetul de pe buton)
    private void OnFullTranscription(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (isProcessing) return;

        Debug.Log($"📝 [FINAL] Mesaj: '{text}'");
        
        // Afisam textul final curat
        UpdateSubtitle(text, Color.green);
        
        // Pornim timer-ul sa dispara textul dupa cateva secunde
        StartCoroutine(HideSubtitleAfterDelay());

        // Trimitem la Gemini
        StartCoroutine(AskGemini(text));
    }

    private IEnumerator HideSubtitleAfterDelay()
    {
        yield return new WaitForSeconds(textDisplayTime);
        // Stergem textul doar daca nu vorbim iar intre timp
        if (!isHoldingButton) 
        {
            subtitleText.text = "";
        }
    }

    private void UpdateSubtitle(string msg, Color color)
    {
        if (subtitleText != null)
        {
            subtitleText.text = msg;
            subtitleText.color = color;
        }
    }
    
    private IEnumerator AskGemini(string userMessage)
    {
        isProcessing = true;
        
        // Folosim Gemma 3 27B (30 RPM)
        string url = "https://generativelanguage.googleapis.com/v1beta/models/gemma-3-27b-it:generateContent?key=" + ApiKeys.Gemini_Key;
        
        GeminiRequest requestData = new GeminiRequest();
        requestData.contents = new GeminiContent[]
        {
            new GeminiContent
            {
                parts = new GeminiPart[]
                {
                    new GeminiPart { text = systemPrompt + "\n\nUser says: " + userMessage }
                }
            }
        };

        string jsonToSend = JsonUtility.ToJson(requestData);
        
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonToSend);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ [GEMINI ERROR] {request.error}");
                mouth.Speak("Connection error.");
                UpdateSubtitle("Error connecting to AI.", Color.red);
                isProcessing = false; // Deblocam daca e eroare
            }
            else
            {
                string jsonResponse = request.downloadHandler.text;
                GeminiResponse responseData = JsonUtility.FromJson<GeminiResponse>(jsonResponse);

                if (responseData.candidates != null && responseData.candidates.Length > 0)
                {
                    string aiAnswer = responseData.candidates[0].content.parts[0].text;
                    Debug.Log($"🤖 [AI] Raspuns: '{aiAnswer}'");

                    // AICI ESTE SCHIMBAREA: Nu mai dam Speak direct, ci pornim "felierea"
                    StartCoroutine(SpeakLongText(aiAnswer));
                }
                else
                {
                    isProcessing = false; // Deblocam daca raspunsul e gol
                }
            }
        }
    }
    
    private IEnumerator SpeakLongText(string fullText)
    {
        // 1. Curatam textul
        fullText = fullText.Replace("*", "");

        // 2. Il spargem in propozitii
        string[] sentences = fullText.Split(new char[] { '.', '?', '!', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        foreach (string sentence in sentences)
        {
            if (string.IsNullOrWhiteSpace(sentence)) continue;

            string cleanSentence = sentence.Trim();
            
            // --- MODIFICARE 1: NU mai afisam textul robotului pe ecran ---
            // UpdateSubtitle(cleanSentence, Color.white); // <--- Linia asta e stearsa/comentata
            
            // Robotul vorbeste
            mouth.Speak(cleanSentence);

            // --- MODIFICARE 2: PAUZE MAI SCURTE ---
            // Formula veche: cleanSentence.Length * 0.08f + 0.5f; (Prea lenta)
            // Formula noua: 0.06 secunde per litera + doar 0.1 secunde pauza intre fraze
            float estimatedDuration = cleanSentence.Length * 0.06f + 0.1f;
            
            // Siguranta: Daca propozitia e prea scurta (ex: "Ok."), asteptam minim 1 secunda sa nu se incalece
            if (estimatedDuration < 1.0f) estimatedDuration = 1.0f;
            
            yield return new WaitForSeconds(estimatedDuration);
        }

        // La final curatam ecranul si deblocam
        UpdateSubtitle("", Color.white);
        isProcessing = false; 
        Debug.Log("✅ [ROBOT] A terminat de vorbit tot.");
    }
}

// --- STRUCTURI JSON ---
[System.Serializable] public class GeminiRequest { public GeminiContent[] contents; }
[System.Serializable] public class GeminiContent { public GeminiPart[] parts; }
[System.Serializable] public class GeminiPart { public string text; }
[System.Serializable] public class GeminiResponse { public GeminiCandidate[] candidates; }
[System.Serializable] public class GeminiCandidate { public GeminiContent content; }