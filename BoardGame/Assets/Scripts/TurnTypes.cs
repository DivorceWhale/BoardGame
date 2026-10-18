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
    PlaceBlock
}

public struct GameAction
{
    public ActionType type;
    public int player;
    public CardData card;     // PlaceBlock: the block being placed
    public int space;         // PlaceBlock: id of the board space (from the BoardLayout asset)
    public string spaceName;
    public int angle;         // PlaceBlock: degrees

    public override string ToString()
    {
        if (type == ActionType.PlaceBlock)
        {
            string title = card != null ? card.title : "?";
            return "PlaceBlock(" + title + ", space " + space + " " + spaceName + ", angle " + angle + ")";
        }
        return type.ToString();
    }
}
