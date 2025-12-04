using UnityEngine;
using UnityEngine.UI;

public class KeyboardColliderSetup : MonoBehaviour
{
    void Start()
    {
        SetupAllKeys();
    }

    [ContextMenu("Setup Keys Now")]
    public void SetupAllKeys()
    {
        Button[] allKeys = GetComponentsInChildren<Button>(true);

        int count = 0;

        foreach (Button btn in allKeys)
        {
            BoxCollider col = btn.GetComponent<BoxCollider>();

            if (col == null)
            {
                col = btn.gameObject.AddComponent<BoxCollider>();
                count++;
            }

            RectTransform rect = btn.GetComponent<RectTransform>();
            if (rect != null)
            {
                col.size = new Vector3(rect.rect.width, rect.rect.height, 0.02f);
            }
        }

        Debug.Log($"[AutoSetup] detailed scan complete. Added Colliders to {count} keys. Keyboard is ready for Physics Raycast.");
    }
}