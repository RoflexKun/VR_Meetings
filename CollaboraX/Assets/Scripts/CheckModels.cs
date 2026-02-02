using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class CheckModels : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(GetAvailableModels());
    }

    IEnumerator GetAvailableModels()
    {
        // Cerem lista oficiala de la Google
        string url = "https://generativelanguage.googleapis.com/v1beta/models?key=" + ApiKeys.Gemini_Key;

        Debug.Log("🕵️ [CHECK] Verific modelele disponibile...");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("❌ Eroare: " + request.error);
            }
            else
            {
                Debug.Log("✅ [LISTA OFICIALA] Uita-te mai jos in textul JSON:");
                
                // Afisam tot raspunsul
                string json = request.downloadHandler.text;
                Debug.Log(json);

                // Facem o verificare rapida sa vedem ce versiuni "Flash" ai
                if (json.Contains("gemini-1.5-flash")) Debug.Log("👉 AI ACCES LA: gemini-1.5-flash");
                if (json.Contains("gemini-2.0-flash")) Debug.Log("👉 AI ACCES LA: gemini-2.0-flash");
                if (json.Contains("gemini-pro")) Debug.Log("👉 AI ACCES LA: gemini-pro");
            }
        }
    }
}