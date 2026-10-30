using System.Collections.Generic;

public class PlayerState
{
    public int index;
    public string name;
    public bool inGame = true;
    public bool fellThisShake;
    public int matchPoints;
    public int peakHeight;       // highest tower height this game (for consolation points)
    public int baseSpace = -1;   // space of this player's first block this game (their tower's base)
    public CardData heldBlock;
    public List<GameAction> placements = new List<GameAction>();

    // Build 7 (rulesheet): per-round flags the legality checks read
    public bool drewThisRound;
    public bool placedThisRound;
    public bool skippedThisRound;
    public int blocksBeforeShake;

    // PLACEHOLDER: height = number of blocks placed. Replace when the table defines height.
    public int TowerHeight { get { return placements.Count; } }
}

public class GameState
{
    public List<PlayerState> players = new List<PlayerState>();
    public TurnPhase phase = TurnPhase.Event;
    public int round = 1;           // rounds since the match started
    public int roundInGame = 1;     // rounds since the current game started (used by the round limit)
    public int gameNumber = 1;
    public int activePlayer;
    public CardData eventCard;
    public bool eventDrawn;
    public DeckManager blockDeck;
    public DeckManager eventDeck;
    public ScenarioConfig scenario;
    public BoardLayout board;

    // Build 7: when true, every action is checked against the rulesheet (RuleCheck).
    // Build 5/6 scenes leave this off and keep their scenario rules.
    public bool useRulesheet;
    public bool ownBases;          // each player builds on their own 4x4 tower base (rulesheet section 2)
    public int buildTurn = -1;     // Build phase: index of the player whose turn it is to place
    public bool shakeDone;
    public bool deckShort;         // R-05: not enough block cards for every standing player this round
}
