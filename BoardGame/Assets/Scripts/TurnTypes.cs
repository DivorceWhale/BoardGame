// RENAME THESE to match your verb wall and turn phase card exactly.

public enum TurnPhase
{
    Event,
    Draw,
    Place,
    Shake,
    CollapseCheck,
    EndCheck,
    Opening         // Rulesheet v2: each player places 1 block of their choice before round 1 of every game
}

public enum ActionType
{
    DrawEventCard,
    DrawBlockCard,
    PlaceBlock,
    StartShake,     // Build 7: the shake is a player action so the rules can refuse it (R-14, R-15)
    RedrawBlock,    // event 1: redraw your own block card once
    PickRival,      // event 9: the first player picks a rival, who redraws (target = the rival)
    SwapOption      // event 13: place your extra block card instead of the first one
}

public struct GameAction
{
    public ActionType type;
    public int player;
    public CardData card;     // PlaceBlock: the block being placed
    public int space;         // PlaceBlock: id of the board space (from the BoardLayout asset)
    public string spaceName;
    public int angle;         // PlaceBlock: degrees
    public int target;        // PickRival: the player who redraws
    public int baseOwner;     // Build 7, PlaceBlock: whose tower base the block goes on (rulesheet: own base only)

    // 3D play: where the physical block really lands
    public bool physical;     // true when placed in the 3D scene
    public int[] covers;      // every space of the tower base under the block's footprint
    public bool onBlock;      // the block rests on another block (event 6), not directly on the base
    public bool offBase;      // part of the block hangs outside the 16 spaces (R-09 refuses it)

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
