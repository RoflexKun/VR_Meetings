using UnityEngine;
using UnityEngine.UI;

public class KeyboardScript : MonoBehaviour
{
    [Header("Target")]
    public InputField TextField; 

    [Header("Layouts")]
    public GameObject RusLayoutSml, RusLayoutBig, EngLayoutSml, EngLayoutBig, SymbLayout;

    // --- FIX: DEBOUNCE TIMER ---
    // Asta e "bariera" care opreste dublu-click-ul
    private float lastTypeTime = 0f;
    private float typeCooldown = 0.15f; // Asteapta 0.15 secunde intre taste

    void Start()
    {
        if (EngLayoutSml != null) ShowLayout(EngLayoutSml);
    }

    public void alphabetFunction(string alphabet)
    {
        // VERIFICARE: Daca a trecut prea putin timp de la ultima tasta, ignoram!
        if (Time.time - lastTypeTime < typeCooldown) return;
        lastTypeTime = Time.time; // Resetam cronometrul

        if (TextField != null)
        {
            TextField.text = TextField.text + alphabet;
            // Mutam cursorul la final ca sa nu scrie aiurea
            TextField.caretPosition = TextField.text.Length;
            TextField.ForceLabelUpdate(); 
        }
    }

    public void BackSpace()
    {
        // Si Backspace-ul are nevoie de protectie sa nu stergi 2 litere odata
        if (Time.time - lastTypeTime < typeCooldown) return;
        lastTypeTime = Time.time;

        if (TextField != null && TextField.text.Length > 0)
        {
            TextField.text = TextField.text.Remove(TextField.text.Length - 1);
        }
    }

    public void Enter()
    {
        if (Time.time - lastTypeTime < typeCooldown) return;
        lastTypeTime = Time.time;

        Debug.Log("[Keyboard] Enter Key Pressed");
        if (TextField != null)
        {
            TextField.text = TextField.text + "\n";
            TextField.ForceLabelUpdate();
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
}