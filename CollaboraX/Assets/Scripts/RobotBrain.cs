using UnityEngine;
using UnityEngine.InputSystem;
using Meta.WitAi.TTS.Utilities;
using Oculus.Voice;

public class RobotBrain : MonoBehaviour
{
    [Header("Robot Parts")]
    public AppVoiceExperience ears;
    public TTSSpeaker mouth;

    private void OnEnable()
    {
        // When the ears finish listening, call the "OnHeard" function
        ears.VoiceEvents.OnFullTranscription.AddListener(OnHeard);
    }

    private void OnDisable()
    {
        ears.VoiceEvents.OnFullTranscription.RemoveListener(OnHeard);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (!ears.Active)
            {
                Debug.Log("Listening... Speak now!");
                ears.Activate();
            }
        }
    }
    
    private void OnHeard(string text)
    {
        Debug.Log("I heard: " + text);
        mouth.Speak("I heard you say: " + text);
    }
}