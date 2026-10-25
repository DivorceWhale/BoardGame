using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Fills every region of the card UI prefab from a CardData asset. No card text lives in the prefab.
public class CardView : MonoBehaviour, IPointerClickHandler
{
    public CardData data;
    public IconSet iconSet;

    [Header("Regions")]
    public Image frame;
    public TMP_Text titleText;
    public Image artImage;
    public Transform iconRow;
    public Image iconTemplate;      // disabled child of Icon Row
    public TMP_Text bodyText;
    public TMP_Text footerText;
    public Image weightIcon;

    [Header("Frame colours")]
    public Color blockColour = new Color(0.82f, 0.90f, 1f);
    public Color eventColour = new Color(1f, 0.88f, 0.75f);

    private bool shown;

    void Start()
    {
        if (data != null && !shown) Show(data);
    }

    public void Show(CardData card)
    {
        if (card == null) return;
        data = card;
        shown = true;
        bool isEvent = card.cardType == CardType.Event;

        if (frame != null) frame.color = isEvent ? eventColour : blockColour;
        if (titleText != null) titleText.text = card.title;
        if (artImage != null)
        {
            artImage.sprite = card.art;
            artImage.enabled = card.art != null;
        }
        if (bodyText != null) bodyText.text = card.bodyText;
        if (footerText != null)
            footerText.text = isEvent ? "EVENT #" + card.id : "BLOCK x" + card.copies;

        // Icon row: one sprite per icon listed on the asset
        if (iconRow != null && iconTemplate != null)
        {
            iconTemplate.gameObject.SetActive(false);
            for (int i = iconRow.childCount - 1; i >= 0; i--)
            {
                Transform child = iconRow.GetChild(i);
                if (child != iconTemplate.transform) Destroy(child.gameObject);
            }
            foreach (CardIcon icon in card.icons)
            {
                Image img = Instantiate(iconTemplate, iconRow);
                img.name = "icon_" + icon;
                img.sprite = iconSet != null ? iconSet.Get(icon) : null;
                img.gameObject.SetActive(true);
            }
        }

        // Weight mark in the footer (events only)
        if (weightIcon != null)
        {
            weightIcon.enabled = card.weight != EventWeight.None && iconSet != null;
            if (iconSet != null)
            {
                if (card.weight == EventWeight.Light) weightIcon.sprite = iconSet.Get(CardIcon.Light);
                if (card.weight == EventWeight.Medium) weightIcon.sprite = iconSet.Get(CardIcon.Medium);
                if (card.weight == EventWeight.Heavy) weightIcon.sprite = iconSet.Get(CardIcon.Heavy);
            }
        }

        // Parity check: flag text that fits the paper card but not this one
        Canvas.ForceUpdateCanvases();
        if (bodyText != null) bodyText.ForceMeshUpdate();
        if (titleText != null) titleText.ForceMeshUpdate();
        if (titleText != null && titleText.isTextOverflowing)
            Debug.LogWarning("PARITY: " + card.title + " - title overflows the digital card.");
        if (bodyText != null && bodyText.isTextOverflowing)
            Debug.LogWarning("PARITY: " + card.title + " - body text overflows the digital card.");
    }

    // Click a card to open it large; click the large card to close it.
    public void OnPointerClick(PointerEventData eventData)
    {
        CardZoom zoom = CardZoom.Instance;
        if (zoom == null || data == null) return;
        if (zoom.bigCard == this) zoom.Close();
        else zoom.Open(data);
    }
}
