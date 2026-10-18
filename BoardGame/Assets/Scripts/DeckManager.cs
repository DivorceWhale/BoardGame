using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DeckManager : MonoBehaviour
{
    [Header("Card Data (used only when no scenario configures the deck)")]
    public List<CardData> cards = new List<CardData>();

    [Header("Deck Settings")]
    [Tooltip("Warn if the total copies differ from this. 0 = no check.")]
    public int expectedDeckSize = 0;

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
    private bool isSetup;

    [Tooltip("Off = used cards never return to the draw pile (no refills).")]
    public bool allowRefill = true;

    public int TotalAvailable
    {
        get
        {
            EnsureSetup();
            return drawPile.Count + (allowRefill ? discardPile.Count : 0);
        }
    }

    void Start()
    {
        EnsureSetup();
        UpdateUI();
    }

    void EnsureSetup()
    {
        if (isSetup) return;
        isSetup = true;
        SetupDeck();
    }

    void SetupDeck()
    {
        drawPile.Clear();
        discardPile.Clear();

        foreach (CardData card in cards)
        {
            for (int i = 0; i < card.copies; i++)
            {
                drawPile.Add(card);
            }
        }

        if (expectedDeckSize > 0 && drawPile.Count != expectedDeckSize)
        {
            Debug.LogWarning(name + ": deck has " + drawPile.Count + " cards, expected " + expectedDeckSize);
        }

        GameRandom.Instance.Shuffle(drawPile);
    }

    // Build the deck from a scenario asset: which cards are in play and how many copies of each.
    public void Configure(List<DeckEntry> entries)
    {
        isSetup = true;
        drawPile.Clear();
        discardPile.Clear();

        foreach (DeckEntry entry in entries)
        {
            if (entry.card == null) continue;
            for (int i = 0; i < entry.copies; i++)
            {
                drawPile.Add(entry.card);
            }
        }

        GameRandom.Instance.Shuffle(drawPile);
        UpdateUI();
    }

    public CardData DrawData()
    {
        EnsureSetup();
        if (drawPile.Count == 0) ReshuffleDiscardPile();
        if (drawPile.Count == 0) return null;

        CardData cardData = drawPile[0];
        drawPile.RemoveAt(0);
        UpdateUI();
        return cardData;
    }

    public void Discard(CardData cardData)
    {
        if (cardData == null) return;
        discardPile.Add(cardData);
        UpdateUI();
    }

    public void DrawCard()
    {
        EnsureSetup();

        if (drawPile.Count == 0)
        {
            ReshuffleDiscardPile();
        }

        if (drawPile.Count == 0)
        {
            Debug.Log("No cards available to draw.");
            return;
        }

        CardData cardData = drawPile[0];
        if (cardData.cardPrefab == null)
        {
            Debug.LogError("Card prefab is missing for: " + cardData.title);
            return;
        }

        if (currentDrawnCard != null)
        {
            MoveCurrentCardToDiscard();
        }

        drawPile.RemoveAt(0);

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

        Destroy(currentDrawnCard.gameObject);
        currentDrawnCard = null;

        if (drawnCardText != null)
        {
            drawnCardText.text = "Drawn Card: None";
        }

        if (drawPile.Count == 0)
        {
            ReshuffleDiscardPile();
        }

        UpdateUI();
    }

    // NOTE: the table has not written an empty-deck rule yet. This reshuffle is the code's assumption.
    void ReshuffleDiscardPile()
    {
        if (!allowRefill || discardPile.Count == 0)
        {
            return;
        }

        foreach (CardData card in discardPile)
        {
            drawPile.Add(card);
        }

        discardPile.Clear();

        GameRandom.Instance.Shuffle(drawPile);

        Debug.Log(name + ": discard pile reshuffled into draw pile.");

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
