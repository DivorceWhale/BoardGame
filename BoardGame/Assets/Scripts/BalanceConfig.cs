using UnityEngine;

// Build 8: every shared balance number in Quaker lives on ONE asset, Assets/Resources/QuakerBalance.asset.
// Change a value there in the Inspector and the game uses it the next time it reads it; no code edits.
//
// Values that belong to one card stay on that card (CardData.copies: 5 of each block style).
// Values that belong to one scenario stay on the ScenarioConfig asset.
// The numbers below are the defaults for a brand-new asset only; the asset holds the real values.
//
// Balance decisions are made at the table. Change a value here only after the table has changed it,
// and log it in the team changelog with the date, the reason, and what the playtest showed.
[CreateAssetMenu(fileName = "QuakerBalance", menuName = "QUAKER/Balance Config")]
public class BalanceConfig : ScriptableObject
{
    [Header("Turn order")]
    [Tooltip("Seats the first player moves clockwise at the end of every round (after the shake), and when a game ends mid-round.\n"
        + "Table change, playtest 2: 0 -> 1. Before, the tallest player stayed first player all match.")]
    [Min(0)] public int firstPlayerSeatsPerRound = 1;

    [Header("Collapse (when the tower has fallen)")]
    [Tooltip("The tower has collapsed once this share of the game's tallest tower has come down, counted over the whole game.\n"
        + "Table change, playtest 2: 1.0 -> 0.5. Before, the tower only counted as fallen when every block had come down.")]
    [Range(0.1f, 1f)] public float collapseShare = 0.5f;
    [Tooltip("...and at least this many blocks came down, so one block tipping over on a small tower isn't a collapse.")]
    [Min(1)] public int collapseMinBlocks = 2;

    [Header("Scoring")]
    [Tooltip("Points every player except the loser scores when the tower falls.")]
    [Min(1)] public int pointsPerGameWin = 1;
    [Tooltip("Points needed to win the match.")]
    [Min(1)] public int pointsToWinMatch = 3;

    [Header("Decks")]
    [Tooltip("Copies of each event card in the event deck. (Block cards: set copies on each block card asset.)")]
    [Min(1)] public int eventCardCopies = 1;

    [Header("Shake")]
    [Tooltip("Seconds each shake lasts.")]
    [Min(0.5f)] public float shakeSeconds = 5f;
    [Tooltip("Shake strength: peak acceleration as a fraction of gravity. Block friction holds up to about 0.6.")]
    [Range(0.05f, 1.5f)] public float shakeStrength = 0.6f;
    [Tooltip("Back-and-forth movements per second.")]
    [Range(0.5f, 8f)] public float shakeHz = 2.5f;
    [Tooltip("Seconds for each shake to build up at the start and die down at the end.")]
    [Min(0f)] public float shakeRamp = 0.5f;
    [Tooltip("Block deck short (shake-only rounds): each of those rounds adds this much to the shake strength.")]
    [Range(0f, 0.5f)] public float shakeOnlyStep = 0.15f;
    [Tooltip("The strongest a shake-only round can get.")]
    [Range(0.1f, 2f)] public float shakeMaxStrength = 1.5f;

    [Header("Event card effects")]
    [Tooltip("Events 5, 10 and 19: how many times the tower shakes that round.")]
    [Min(1)] public int doubleShakeCount = 2;
    [Tooltip("Event 15: how much faster the shake runs.")]
    [Min(1f)] public float fastShakeSpeed = 2f;
    [Tooltip("Event 16 (aftershocks): shakes before anyone places.")]
    [Min(1)] public int aftershockShakes = 2;
    [Tooltip("Event 14: seconds each player has to place their block.")]
    [Min(1f)] public float tenCountSeconds = 10f;
    [Tooltip("Event 17: placements the last player in turn order gets that round.")]
    [Min(1)] public int lastPlayerPlacements = 2;
    [Tooltip("Event 20: points the loser loses if the tower falls while it is face up.")]
    [Min(0)] public int event20PointLoss = 1;
    [Tooltip("Placements every other player gets in a round.")]
    [Min(1)] public int placementsPerRound = 1;

    [Header("Physics feel")]
    [Tooltip("Gravity multiplier: the blocks are 4 units across, so normal gravity looks slow.")]
    [Min(0.1f)] public float gravityScale = 3f;
    [Min(0.01f)] public float blockMass = 1f;
    [Tooltip("Longest wait, in seconds, for the blocks to stop moving before the collapse check.")]
    [Min(1f)] public float settleTimeout = 6f;

    // ---------- the one copy every script reads ----------
    public const string ResourceName = "QuakerBalance";
    static BalanceConfig current;

    public static BalanceConfig Current
    {
        get
        {
            if (current == null) current = Resources.Load<BalanceConfig>(ResourceName);
            if (current == null)
            {
                Debug.LogWarning("[Quaker] Assets/Resources/" + ResourceName + ".asset is missing; using the default balance values.");
                current = CreateInstance<BalanceConfig>();
            }
            return current;
        }
        set { if (value != null) current = value; }
    }
}
