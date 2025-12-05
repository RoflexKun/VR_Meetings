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
    
    [Header("Input")]
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
        if (openAction.action != null && openAction.action.WasPressedThisFrame())
        {
            if (!notesOpen)
            {
                CheckForPaper();
            }
            else
            {
                CheckForKeys();
            }
        }

        if (closeAction.action != null && closeAction.action.WasPressedThisFrame())
        {
            Debug.Log("[TakeNotes] A BUTTON PRESSED! (Close Action)");
            
            if (notesOpen)
            {
                CloseNotes();
            }
        }
        if (notesOpen) SyncKeyboardBuffer();
    }

    void CheckForPaper()
    {
        RaycastHit hit;
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
        if (Physics.Raycast(controllerHand.position, controllerHand.forward, out hit, activationDistance))
        {
            Button btn = hit.collider.GetComponent<Button>();
            
            if (btn != null)
            {
                Debug.Log($"[TakeNotes] Force Clicking Button: {hit.collider.name}");
                btn.onClick.Invoke();
            }
            else
            {
                btn = hit.collider.GetComponentInParent<Button>();
                if (btn != null)
                {
                    Debug.Log($"[TakeNotes] Force Clicking Parent Button: {btn.name}");
                    btn.onClick.Invoke();
                }
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
        if (locomotionSystem != null) locomotionSystem.gameObject.SetActive(false);

        PositionUI();

        if (keyboardUI != null && keyboardBuffer != null)
        {
            KeyboardScript ks = keyboardUI.GetComponentInChildren<KeyboardScript>();
            if (ks != null) ks.TextField = keyboardBuffer;
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
        Vector3 centerPos = playerCamera.position + playerCamera.forward * distanceInFront;
        Quaternion faceCam = Quaternion.LookRotation(playerCamera.forward, playerCamera.up);
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
        if (closeAction.action != null) closeAction.action.Enable(); // NEW
    }
    private void OnDisable() 
    { 
        if (openAction.action != null) openAction.action.Disable(); 
        if (closeAction.action != null) closeAction.action.Disable(); // NEW
    }
}