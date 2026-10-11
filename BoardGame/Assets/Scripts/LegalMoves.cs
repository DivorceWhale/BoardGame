using System.Collections.Generic;

public static class LegalMoves
{
    // Returns only moves that are legal right now. Illegal moves are never in the list.
    public static List<GameAction> Get(GameState s, PlayerState p)
    {
        List<GameAction> list = new List<GameAction>();
        if (!p.inGame) return list;

        switch (s.phase)
        {
            case TurnPhase.Event:
                if (!s.eventDrawn && p.index == s.activePlayer && s.eventDeck.TotalAvailable > 0)
                {
                    list.Add(new GameAction { type = ActionType.DrawEventCard, player = p.index });
                }
                break;

            case TurnPhase.Draw:
                if (p.heldBlock == null && s.blockDeck.TotalAvailable > 0)
                {
                    list.Add(new GameAction { type = ActionType.DrawBlockCard, player = p.index });
                }
                break;

            case TurnPhase.Place:
                if (p.heldBlock != null)
                {
                    // PLACEHOLDER: any cell, any angle. Add restrictions only when the table decides them
                    // (for example, whether each tower owns a cell).
                    int cells = TableNumbers.GridSize * TableNumbers.GridSize;
                    for (int cell = 0; cell < cells; cell++)
                    {
                        for (int step = 0; step < TableNumbers.AngleSteps; step++)
                        {
                            list.Add(new GameAction
                            {
                                type = ActionType.PlaceBlock,
                                player = p.index,
                                card = p.heldBlock,
                                cell = cell,
                                angle = step * TableNumbers.AngleStepDegrees
                            });
                        }
                    }
                }
                break;

            // Shake, CollapseCheck and EndCheck have no player verbs.
        }
        return list;
    }

    public static string Describe(List<GameAction> list)
    {
        if (list.Count == 0) return "none";

        List<string> parts = new List<string>();
        int placeCount = 0;
        GameAction firstPlace = default(GameAction);

        foreach (GameAction a in list)
        {
            if (a.type == ActionType.PlaceBlock)
            {
                if (placeCount == 0) firstPlace = a;
                placeCount++;
            }
            else
            {
                parts.Add(a.ToString());
            }
        }

        if (placeCount > 0)
        {
            string title = firstPlace.card != null ? firstPlace.card.title : "?";
            parts.Add("PlaceBlock x" + placeCount + " (" + title + ": any of "
                + (TableNumbers.GridSize * TableNumbers.GridSize) + " cells x "
                + TableNumbers.AngleSteps + " angles)");
        }
        return string.Join("; ", parts.ToArray());
    }
}
