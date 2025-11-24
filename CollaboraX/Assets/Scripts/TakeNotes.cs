using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class TakeNotes : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject notesUI;     
    public GameObject paperPanel;  
    public TMP_Text notesText;
    public GameObject keyboardUI;

    [Header("Player References")]
    public Transform xrRig;
    public Transform playerCamera;

    [Header("World Objects")]
    public GameObject paperObject;

    [Header("Settings")]
    public float activationDistance = 1.5f;
    public float distanceInFront = 0.6f;

    [Header("Keyboard Bridge")]
    public UnityEngine.UI.InputField keyboardBuffer;

    public float horizontalOffset = 0.30f;
    public float verticalExtraOffset = 0.15f;

    private bool isPlayerNear = false;
    private bool notesOpen = false;

    private Vector3 frozenPosition;
    private Quaternion frozenRotation;

    void Start()
    {
        notesUI.SetActive(false);
        keyboardUI.SetActive(false);

        RectTransform rt = notesText.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0, 1);

        notesText.alignment = TextAlignmentOptions.TopLeft;
        notesText.enableWordWrapping = true;
    }

    void Update()
    {
        UpdateProximity();
        HandleInput();
        FreezePlayerIfNeeded();

        if (notesOpen)
            SyncKeyboardBuffer();
    }

    void UpdateProximity()
    {
        if (!playerCamera || !paperObject) return;

        float dist = Vector3.Distance(playerCamera.position, paperObject.transform.position);
        isPlayerNear = dist <= activationDistance;
    }

    void HandleInput()
    {
        if (isPlayerNear && Keyboard.current?.nKey.wasPressedThisFrame == true)
            OpenNotes();

        if (notesOpen && Keyboard.current?.qKey.wasPressedThisFrame == true)
            CloseNotes();
    }

    void FreezePlayerIfNeeded()
    {
        if (notesOpen && xrRig)
        {
            xrRig.position = frozenPosition;
            xrRig.rotation = frozenRotation;
        }
    }

    void OpenNotes()
    {
        notesUI.SetActive(true);
        keyboardUI.SetActive(true);
        notesOpen = true;

        if (xrRig)
        {
            frozenPosition = xrRig.position;
            frozenRotation = xrRig.rotation;
        }

        PositionUI();
    }

    public void CloseNotes()
    {
        notesOpen = false;
        notesUI.SetActive(false);
        keyboardUI.SetActive(false);
    }

    void PositionUI()
    {
        if (!playerCamera || !notesUI) return;

        Vector3 centerPos =
            playerCamera.position + playerCamera.forward * distanceInFront;

        Quaternion faceCam =
            Quaternion.LookRotation(-playerCamera.forward, playerCamera.up);

        notesUI.transform.SetPositionAndRotation(centerPos, faceCam);

    }

    void SyncKeyboardBuffer()
    {
        if (keyboardBuffer == null || notesText == null)
            return;

        notesText.text = keyboardBuffer.text;
    }
}
