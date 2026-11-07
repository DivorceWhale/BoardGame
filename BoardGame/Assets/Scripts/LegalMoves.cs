using System.Collections.Generic;

public static class LegalMoves
{
    // Returns only moves that are legal right now. Illegal moves are never in the list.
    public static List<GameAction> Get(GameState s, PlayerState p)
    {
        // Build 7: rulesheet games list every candidate action and keep only those RuleCheck allows.
        if (s.useRulesheet) return GetFromRulesheet(s, p);

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
                    // A scenario can force where a player's first block goes (for example Four Corners).
                    int forced = -1;
                    if (p.placements.Count == 0)
                    {
                        foreach (FirstPlacement fp in s.scenario.firstPlacementSpaces)
                        {
                            if (fp.playerIndex == p.index) forced = fp.spaceId;
                        }
                    }

                    // Open spaces come from the board asset and the scenario's blocked list.
                    foreach (SpaceData space in s.board.spaces)
                    {
                        if (!s.scenario.IsOpen(space.id)) continue;
                        if (forced >= 0 && space.id != forced) continue;
                        for (int step = 0; step < TableNumbers.AngleSteps; step++)
                        {
                            list.Add(new GameAction
                            {
                                type = ActionType.PlaceBlock,
                                player = p.index,
                                card = p.heldBlock,
                                space = space.id,
                                spaceName = space.displayName,
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

    // ---------- Build 7: the rulesheet path ----------

    // Second path for callers that bypass the option list: a direct attempt is still checked.
    // Returns the result; the caller applies the action only when it is legal.
    public static RuleResult Validate(GameState s, GameAction a)
    {
        return RuleCheck.Validate(s, a);
    }

    public static bool IsLegal(GameState s, GameAction a)
    {
        return RuleCheck.Validate(s, a).legal;
    }

    static List<GameAction> GetFromRulesheet(GameState s, PlayerState p)
    {
        List<GameAction> candidates = new List<GameAction>();
        candidates.Add(new GameAction { type = ActionType.DrawEventCard, player = p.index });
        candidates.Add(new GameAction { type = ActionType.DrawBlockCard, player = p.index });
        candidates.Add(new GameAction { type = ActionType.StartShake, player = p.index });
        candidates.Add(new GameAction { type = ActionType.RedrawBlock, player = p.index });
        candidates.Add(new GameAction { type = ActionType.SwapOption, player = p.index });
        for (int i = 0; i < s.players.Count; i++)
            if (i != p.index) candidates.Add(new GameAction { type = ActionType.PickRival, player = p.index, target = i });

        if (s.board != null)
        {
            // Rulesheet v2: one shared tower base. Opening placement offers every block style;
            // the Build phase offers the style on the player's block card.
            List<CardData> styles = new List<CardData>();
            if (s.phase == TurnPhase.Opening && s.scenario != null)
            {
                foreach (DeckEntry e in s.scenario.blockDeck)
                    if (e.card != null && !styles.Contains(e.card)) styles.Add(e.card);
            }
            else if (p.heldBlock != null)
            {
                styles.Add(p.heldBlock);
            }

            foreach (CardData style in styles)
            {
                foreach (SpaceData space in s.board.spaces)
                {
                    candidates.Add(new GameAction
                    {
                        type = ActionType.PlaceBlock,
                        player = p.index,
                        card = style,
                        space = space.id,
                        spaceName = space.displayName,
                        angle = 0   // any angle is legal (physical); one entry per space
                    });
                }
            }
        }

        List<GameAction> legal = new List<GameAction>();
        foreach (GameAction a in candidates)
        {
            if (RuleCheck.Validate(s, a).legal) legal.Add(a);
        }
        return legal;
    }

    public static string Describe(GameState s, List<GameAction> list)
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
            HashSet<int> distinct = new HashSet<int>();
            foreach (GameAction a in list) if (a.type == ActionType.PlaceBlock) distinct.Add(a.space);
            int open = distinct.Count;
            string title = firstPlace.card != null ? firstPlace.card.title : "?";
            if (s.useRulesheet)
            {
                HashSet<int> spaces = new HashSet<int>();
                HashSet<CardData> kinds = new HashSet<CardData>();
                foreach (GameAction a in list)
                    if (a.type == ActionType.PlaceBlock) { spaces.Add(a.space); kinds.Add(a.card); }
                string what = kinds.Count > 1 ? "any of " + kinds.Count + " block styles" : title;
                parts.Add("PlaceBlock (" + what + ") on " + spaces.Count + " of the 16 spaces of the tower base");
                return string.Join("; ", parts.ToArray());
            }
            parts.Add("PlaceBlock x" + placeCount + " (" + title + ": any of " + open + " open spaces x "
                + TableNumbers.AngleSteps + " angles)");
        }
        return string.Join("; ", parts.ToArray());
    }
}
