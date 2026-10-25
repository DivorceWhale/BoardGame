using System.Collections.Generic;
using UnityEngine;

public enum CardType
{
    Block,
    Event
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

    [Header("Card Face (Build 6)")]
    public Sprite art;
    public EventWeight weight = EventWeight.None;
    public List<CardIcon> icons = new List<CardIcon>();

    [Header("3D Card")]
    public Card cardPrefab;
}