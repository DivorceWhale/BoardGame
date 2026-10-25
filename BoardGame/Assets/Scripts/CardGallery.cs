using System.Collections.Generic;
using UnityEngine;

// Renders every listed card through the single CardUI prefab.
public class CardGallery : MonoBehaviour
{
    public CardView cardPrefab;
    public Transform grid;
    public List<CardData> cards = new List<CardData>();

    void Start()
    {
        if (cardPrefab == null || grid == null)
        {
            Debug.LogError("CardGallery: Card Prefab or Grid is not assigned.");
            return;
        }
        Debug.Log("CardGallery: rendering " + cards.Count + " cards through one prefab.");

        foreach (CardData card in cards)
        {
            if (card == null) continue;
            CardView view = Instantiate(cardPrefab, grid);
            view.name = "Card_" + card.title;
            view.Show(card);
        }
    }
}
