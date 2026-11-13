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
    public int placementsThisRound;   // rulesheet v2: card 17 lets the last player place twice
    public bool placedOpening;        // rulesheet v2: opening placement done this game
    public bool redrewThisRound;      // event 1: the one redraw has been used
    public CardData extraOption;      // event 13: the second block card drawn this round

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
    public bool deckShort;         // R-05: not enough block cards for every player this round
    public bool fuseOut;           // SC-3 (Fuse Lit): the build rounds are used up, so this round is shake-only

    // Rulesheet v2 (one shared tower)
    public List<int> turnOrder = new List<int>();   // player indices clockwise from the first player ([0]); turns by one seat every round
    public int lastPlacer = -1;                      // who placed the most recent block (loses if the tower falls)
    public bool towerFell;
    public List<GameAction> tower = new List<GameAction>();   // every block on the shared tower this game, in placement order
    public bool rivalPicked;       // event 9: the first player has picked a rival this round
    public bool aftershocksDone;   // event 16: the two shakes before any new placement have run

    public int OrderPosition(int player) { return turnOrder.IndexOf(player); }
    public int LastInOrder { get { return turnOrder.Count > 0 ? turnOrder[turnOrder.Count - 1] : -1; } }
}
