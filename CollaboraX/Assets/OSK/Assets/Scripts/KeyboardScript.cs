using UnityEngine;
using UnityEngine.UI;

public class KeyboardScript : MonoBehaviour
{
    [Header("Target")]
    public InputField TextField; 

    [Header("Layouts")]
    public GameObject RusLayoutSml, RusLayoutBig, EngLayoutSml, EngLayoutBig, SymbLayout;

    void Start()
    {
        if (EngLayoutSml != null) ShowLayout(EngLayoutSml);
    }

    public void alphabetFunction(string alphabet)
    {
        if (TextField != null)
        {
            TextField.text = TextField.text + alphabet;
            TextField.ForceLabelUpdate(); 
        }
    }

    public void BackSpace()
    {
        if (TextField != null && TextField.text.Length > 0)
        {
            TextField.text = TextField.text.Remove(TextField.text.Length - 1);
        }
    }

    public void BackToEnglish()
    {
        ShowLayout(EngLayoutSml);
    }

    public void CloseAllLayouts()
    {
        if(RusLayoutSml) RusLayoutSml.SetActive(false);
        if(RusLayoutBig) RusLayoutBig.SetActive(false);
        if(EngLayoutSml) EngLayoutSml.SetActive(false);
        if(EngLayoutBig) EngLayoutBig.SetActive(false);
        if(SymbLayout) SymbLayout.SetActive(false);
    }

    public void ShowLayout(GameObject SetLayout)
    {
        if (SetLayout == null) 
        {
            Debug.LogWarning("[Keyboard] Button tried to open a missing layout!");
            return; 
        }

        CloseAllLayouts();
        SetLayout.SetActive(true);
    }

    public void Enter()
    {
        Debug.Log("[Keyboard] Enter Key Pressed");
        if (TextField != null)
        {
            TextField.text = TextField.text + "\n";
            TextField.ForceLabelUpdate();
        }
    }
}