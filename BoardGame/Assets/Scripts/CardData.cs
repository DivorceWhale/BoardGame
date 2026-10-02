using UnityEngine;

public enum CardType
{
    Block
}

[CreateAssetMenu(fileName = "NewCard", menuName = "QUAKER/Card")]
public class CardData : ScriptableObject
{
    [Header("Card Information")]
    public string id;
    public string title;

    [TextArea(3, 5)]
    public string bodyText;

    public CardType cardType;

    [Header("Deck")]
    public int copies = 1;

    [Header("3D Card")]
    public Card cardPrefab;
}