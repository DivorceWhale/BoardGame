using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Opens and closes a panel from a button or a key at any time during play.
// With Player Aid set, it shows the player aid text verbatim.
public class HelpPanel : MonoBehaviour
{
    public TextAsset playerAid;
    public GameObject panel;
    public TMP_Text aidText;
    public Button openButton;
    public Button closeButton;
#if ENABLE_INPUT_SYSTEM
    public Key toggleKey = Key.H;
#endif

    void Start()
    {
        if (aidText != null && playerAid != null)
            aidText.text = playerAid.text;
        if (openButton != null) openButton.onClick.AddListener(Toggle);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (panel != null) panel.SetActive(false);
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Typing()) return;   // don't toggle while someone is typing a name into a text field
        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame) Toggle();
#endif
    }

    static bool Typing()
    {
        GameObject sel = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        TMP_InputField field = sel != null ? sel.GetComponent<TMP_InputField>() : null;
        return field != null && field.isFocused;
    }

    public void Toggle()
    {
        if (panel != null) panel.SetActive(!panel.activeSelf);
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }
}
