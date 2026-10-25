using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Click any card to see it large in the middle of the screen. Click anywhere (or Esc) to close.
public class CardZoom : MonoBehaviour
{
    public static CardZoom Instance { get; private set; }

    public GameObject overlay;     // full-screen dim panel, starts hidden
    public CardView bigCard;       // a CardUI instance, scaled up
    public Button closeArea;       // the dim background

    void Awake()
    {
        Instance = this;
        if (overlay != null) overlay.SetActive(false);
        if (closeArea != null) closeArea.onClick.AddListener(Close);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
#endif
    }

    public bool IsOpen => overlay != null && overlay.activeSelf;

    public void Open(CardData card)
    {
        if (card == null || overlay == null || bigCard == null) return;
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
        bigCard.Show(card);
    }

    public void Close()
    {
        if (overlay != null) overlay.SetActive(false);
    }
}
