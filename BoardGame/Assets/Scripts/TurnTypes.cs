// RENAME THESE to match your verb wall and turn phase card exactly.

public enum TurnPhase
{
    Event,
    Draw,
    Place,
    Shake,
    CollapseCheck,
    EndCheck
}

public enum ActionType
{
    DrawEventCard,
    DrawBlockCard,
    PlaceBlock,
    StartShake      // Build 7: the shake is a player action so the rules can refuse it (R-14, R-15)
}

public struct GameAction
{
    public ActionType type;
    public int player;
    public CardData card;     // PlaceBlock: the block being placed
    public int space;         // PlaceBlock: id of the board space (from the BoardLayout asset)
    public string spaceName;
    public int angle;         // PlaceBlock: degrees
    public int baseOwner;     // Build 7, PlaceBlock: whose tower base the block goes on (rulesheet: own base only)

    public override string ToString()
    {
        if (type == ActionType.PlaceBlock)
        {
            string title = card != null ? card.title : "?";
            return "PlaceBlock(" + title + ", base of P" + (baseOwner + 1) + ", space " + space + " " + spaceName + ", angle " + angle + ")";
        }
        return type.ToString();
    }
}
