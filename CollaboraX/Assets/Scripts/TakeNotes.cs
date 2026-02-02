using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI; 

public class TakeNotes : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject notesUI;
    public GameObject keyboardUI;
    public TMP_Text notesText;

    [Header("Player References")]
    public Transform playerCamera;
    public LocomotionSystem locomotionSystem; 

    [Header("World Objects")]
    public GameObject paperObject; 

    [Header("Settings")]
    public float distanceInFront = 0.8f; 
    public float activationDistance = 5.0f; 
    public float aimTolerance = 0.2f;
    
    // --- FIX 1: Adaugam o limita de timp intre click-uri ---
    [Header("Input Settings")]
    public float clickCooldown = 0.3f; // 0.3 secunde pauza intre click-uri
    private float lastClickTime = 0f;  // Cand am dat ultimul click

    [Header("Input Actions")]
    public InputActionProperty openAction;
    public InputActionProperty closeAction;

    [Header("Controller Setup")]
    public Transform controllerHand; 

    [Header("Keyboard Bridge")]
    public UnityEngine.UI.InputField keyboardBuffer;

    private bool notesOpen = false;

    void Start()
    {
        notesUI.SetActive(false);
        keyboardUI.SetActive(false);
        SetupText();
    }

    void Update()
    {
        // Verificam CLOSE action
        if (closeAction.action != null && closeAction.action.WasPressedThisFrame())
        {
            // --- FIX 2: Aplicam cooldown si aici ---
            if (Time.time - lastClickTime < clickCooldown) return;
            lastClickTime = Time.time;

            Debug.Log("[TakeNotes] Close Action Pressed");
            if (notesOpen) CloseNotes();
            return; // Iesim ca sa nu facem si Open in acelasi frame
        }

        // Verificam OPEN / CLICK action
        if (openAction.action != null && openAction.action.WasPressedThisFrame())
        {
            // --- FIX 3: THE MAGIC GUARD ---
            // Daca a trecut prea putin timp de la ultimul click, ignoram complet
            if (Time.time - lastClickTime < clickCooldown) return;
            
            // Actualizam timpul ultimului click valid
            lastClickTime = Time.time;

            if (!notesOpen)
            {
                CheckForPaper();
            }
            else
            {
                CheckForKeys();
            }
        }

        if (notesOpen) SyncKeyboardBuffer();
    }

    void CheckForPaper()
    {
        RaycastHit hit;
        // SphereCast e bun pentru obiecte mari
        if (Physics.SphereCast(controllerHand.position, 0.4f, controllerHand.forward, out hit, activationDistance))
        {
            if (hit.collider.gameObject == paperObject)
            {
                OpenNotes();
            }
        }
    }

    void CheckForKeys()
    {
        RaycastHit hit;
        // Raycast e mai precis pentru taste mici
        if (Physics.Raycast(controllerHand.position, controllerHand.forward, out hit, activationDistance))
        {
            Button btn = hit.collider.GetComponent<Button>();
            
            // Logica pentru a gasi butonul (direct sau in parinte)
            if (btn == null)
            {
                btn = hit.collider.GetComponentInParent<Button>();
            }

            if (btn != null)
            {
                Debug.Log($"[TakeNotes] Clicking Button: {btn.name} at time {Time.time}");
                btn.onClick.Invoke();
            }
        }
    }

    public void OpenNotes()
    {
        if (notesOpen) return;

        notesUI.SetActive(true);
        keyboardUI.SetActive(true);
        notesOpen = true;

        if (paperObject != null) paperObject.GetComponent<Collider>().enabled = false;
        
        // Dezactivam locomotia doar daca referinta exista
        if (locomotionSystem != null) locomotionSystem.gameObject.SetActive(false);

        PositionUI();

        if (keyboardUI != null && keyboardBuffer != null)
        {
            // Cautam scriptul tastaturii (presupunand ca exista un script KeyboardScript)
            // Folosim un try-catch sau null check simplu pentru siguranta
            var ks = keyboardUI.GetComponentInChildren<MonoBehaviour>(); 
            // Nota: Aici am lasat generic pentru ca nu am codul KeyboardScript, 
            // dar linia ta originala era buna daca ai acel script.
             KeyboardScript realKs = keyboardUI.GetComponentInChildren<KeyboardScript>();
             if (realKs != null) realKs.TextField = keyboardBuffer;
        }
    }

    public void CloseNotes()
    {
        notesOpen = false;
        notesUI.SetActive(false);
        keyboardUI.SetActive(false);

        if (paperObject != null) paperObject.GetComponent<Collider>().enabled = true;
        if (locomotionSystem != null) locomotionSystem.gameObject.SetActive(true);
    }
    
    void PositionUI()
    {
        if (!playerCamera || !notesUI) return;
        
        // Pozitionam UI-ul putin mai jos fata de camera ca sa fie confortabil
        Vector3 centerPos = playerCamera.position + playerCamera.forward * distanceInFront;
        // Il tinem orizontal (sa nu se incline sus-jos cu capul, e mai citibil)
        Vector3 lookPos = new Vector3(playerCamera.forward.x, 0, playerCamera.forward.z); 
        Quaternion faceCam = Quaternion.LookRotation(lookPos);
        
        notesUI.transform.SetPositionAndRotation(centerPos, faceCam);
    }

    void SyncKeyboardBuffer()
    {
        if (keyboardBuffer != null && notesText != null) notesText.text = keyboardBuffer.text;
    }

    void SetupText()
    {
        if (notesText == null) return;
        notesText.alignment = TextAlignmentOptions.TopLeft;
        notesText.enableWordWrapping = true;
    }
    
    private void OnEnable() 
    { 
        if (openAction.action != null) openAction.action.Enable(); 
        if (closeAction.action != null) closeAction.action.Enable(); 
    }
    private void OnDisable() 
    { 
        if (openAction.action != null) openAction.action.Disable(); 
        if (closeAction.action != null) closeAction.action.Disable(); 
    }
}