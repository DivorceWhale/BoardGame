// RENAME THESE to match your verb wall and turn phase card exactly.
// The wall and the card win over anything written here.

// One entry per phase, in the order printed on the phase card.
public enum TurnPhase
{
    Event,
    Draw,
    Place,
    Shake,
    CollapseCheck,
    EndCheck
}

// One entry per verb kept on the verb wall. Shake is not a player verb, so it is not here.
public enum ActionType
{
    DrawEventCard,
    DrawBlockCard,
    PlaceBlock
}

public struct GameAction
{
    public ActionType type;
    public int player;
    public CardData card;   // PlaceBlock: the block being placed
    public int cell;        // PlaceBlock: 0..15 in the 4x4 area
    public int angle;       // PlaceBlock: degrees

    public override string ToString()
    {
        if (type == ActionType.PlaceBlock)
        {
            int x = cell % TableNumbers.GridSize;
            int y = cell / TableNumbers.GridSize;
            string title = card != null ? card.title : "?";
            return "PlaceBlock(" + title + ", cell (" + x + "," + y + "), angle " + angle + ")";
        }
        return type.ToString();
    }
}
