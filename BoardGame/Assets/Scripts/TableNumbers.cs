// Every number the game uses, in one readable place.
// FROM THE TABLE = decided in class. PLACEHOLDER = the table has not decided it yet:
// replace with the Dice Lab / rules decision and delete the PLACEHOLDER tag.
public static class TableNumbers
{
    // ---- FROM THE TABLE (Quaker rules) ----
    public const int BlockStyles = 6;
    public const int CopiesPerStyle = 5;
    public const int BlockDeckSize = BlockStyles * CopiesPerStyle; // 30 cards
    public const int GridSize = 4;                                 // 4x4 placement area
    public const int PointsPerGameWin = 1;
    public const int PointsToWinMatch = 3;
    public const int MinPlayers = 2;                               // check against the GDD
    public const int MaxPlayers = 4;                               // check against the GDD

    // ---- PLACEHOLDERS (not decided at the table) ----
    public const int AngleSteps = 4;                               // PLACEHOLDER: 0/90/180/270
    public const int AngleStepDegrees = 90;                        // PLACEHOLDER
    public const float PlaceholderCollapseChance = 0.15f;          // PLACEHOLDER: real shake is physical
}
