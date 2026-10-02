using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DeckManager : MonoBehaviour
{
    [Header("Card Data")]
    public List<CardData> cards = new List<CardData>();

    [Header("Deck Settings")]
    public int shuffleSeed = 12345;

    [Header("Card Timing")]
    public float discardDelay = 5f;

    [Header("3D Positions")]
    public Transform drawPosition;

    [Header("UI")]
    public TMP_Text drawPileText;
    public TMP_Text discardPileText;
    public TMP_Text drawnCardText;

    private List<CardData> drawPile = new List<CardData>();
    private List<CardData> discardPile = new List<CardData>();

    private Card currentDrawnCard;

    private System.Random random;

    void Start()
    {
        random = new System.Random(shuffleSeed);

        SetupDeck();

        UpdateUI();
    }

    void SetupDeck()
    {
        drawPile.Clear();
        discardPile.Clear();

        // Add each card type according to its copy count.
        foreach (CardData card in cards)
        {
            for (int i = 0; i < card.copies; i++)
            {
                drawPile.Add(card);
            }
        }

        Shuffle(drawPile);
    }

    void Shuffle(List<CardData> pile)
    {
        for (int i = pile.Count - 1; i > 0; i--)
        {
            int randomIndex = random.Next(i + 1);

            CardData temp = pile[i];
            pile[i] = pile[randomIndex];
            pile[randomIndex] = temp;
        }
    }

    public void DrawCard()
    {
        if (drawPile.Count == 0)
        {
            ReshuffleDiscardPile();
        }

        if (drawPile.Count == 0)
        {
            Debug.Log("No cards available to draw.");
            return;
        }

        // Discard the currently displayed card if another card is drawn.
        if (currentDrawnCard != null)
        {
            MoveCurrentCardToDiscard();
        }

        CardData cardData = drawPile[0];

        drawPile.RemoveAt(0);

        // Create the physical card only when it is drawn.
        if (cardData.cardPrefab == null)
        {
            Debug.LogError("Card prefab is missing for: " + cardData.title);
            return;
        }

        currentDrawnCard = Instantiate(
            cardData.cardPrefab,
            drawPosition.position,
            drawPosition.rotation
        );

        currentDrawnCard.cardData = cardData;

        if (drawnCardText != null)
        {
            drawnCardText.text = "Drawn Card: " + cardData.title;
        }

        UpdateUI();

        // Automatically discard after the selected delay.
        CancelInvoke(nameof(MoveCurrentCardToDiscard));
        Invoke(nameof(MoveCurrentCardToDiscard), discardDelay);
    }

    void MoveCurrentCardToDiscard()
    {
        if (currentDrawnCard == null)
        {
            return;
        }

        CardData cardData = currentDrawnCard.cardData;

        if (cardData != null)
        {
            discardPile.Add(cardData);
        }

        // Destroy the physical card.
        Destroy(currentDrawnCard.gameObject);

        currentDrawnCard = null;

        if (drawnCardText != null)
        {
            drawnCardText.text = "Drawn Card: None";
        }

        // Reshuffle the discard pile when the draw pile is empty.
        if (drawPile.Count == 0)
        {
            ReshuffleDiscardPile();
        }

        UpdateUI();
    }

    void ReshuffleDiscardPile()
    {
        if (discardPile.Count == 0)
        {
            return;
        }

        foreach (CardData card in discardPile)
        {
            drawPile.Add(card);
        }

        discardPile.Clear();

        Shuffle(drawPile);

        Debug.Log("Discard pile reshuffled into draw pile.");

        UpdateUI();
    }

    void UpdateUI()
    {
        if (drawPileText != null)
        {
            drawPileText.text = "Draw Pile: " + drawPile.Count;
        }

        if (discardPileText != null)
        {
            discardPileText.text = "Discard Pile: " + discardPile.Count;
        }
    }
}