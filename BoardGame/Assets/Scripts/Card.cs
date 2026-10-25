using TMPro;
using UnityEngine;

// 3D card in the world. If Label is set, its text comes from Card Data,
// so one prefab serves every event card.
public class Card : MonoBehaviour
{
    public CardData cardData;

    [Tooltip("Optional. Filled from Card Data at Start.")]
    public TMP_Text label;

    void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (label == null || cardData == null) return;
        if (cardData.cardType == CardType.Block)
            label.text = cardData.title;
        else
            label.text = cardData.bodyText.Replace("\n", "\n\n\n");
    }
}
