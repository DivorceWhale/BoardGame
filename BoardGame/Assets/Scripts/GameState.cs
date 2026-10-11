using System.Collections.Generic;

public class PlayerState
{
    public int index;
    public string name;
    public bool inGame = true;        // false once the tower collapses in the current game
    public bool fellThisShake;
    public int matchPoints;
    public CardData heldBlock;
    public List<GameAction> placements = new List<GameAction>();

    // PLACEHOLDER: height = number of blocks placed. Replace when the table defines height.
    public int TowerHeight { get { return placements.Count; } }
}

public class GameState
{
    public List<PlayerState> players = new List<PlayerState>();
    public TurnPhase phase = TurnPhase.Event;
    public int round = 1;
    public int gameNumber = 1;
    public int activePlayer;          // reads the event card / starts the shake (unresolved at the table)
    public CardData eventCard;
    public bool eventDrawn;
    public DeckManager blockDeck;
    public DeckManager eventDeck;
}
