using System.Collections.Generic;

// Result of checking one action against the rulesheet.
public struct RuleResult
{
    public bool legal;
    public string ruleId;
    public string reason;

    public static RuleResult Ok() { return new RuleResult { legal = true }; }
    public static RuleResult No(string id, string why) { return new RuleResult { legal = false, ruleId = id, reason = why }; }

    public override string ToString()
    {
        return legal ? "Legal" : "REFUSED " + ruleId + ": " + reason;
    }
}

// The legality checks for Quaker Rulesheet v2 (one shared tower).
// Every refusal names the rule id it comes from (see RuleBook).
public static class RuleCheck
{
    public static RuleResult Validate(GameState s, GameAction a)
    {
        if (!s.useRulesheet) return RuleResult.Ok();
        if (a.player < 0 || a.player >= s.players.Count) return RuleResult.No("R-06", "There is no player " + (a.player + 1) + ".");

        PlayerState p = s.players[a.player];
        int ev = EventNumber(s);
        bool lastInOrder = a.player == s.LastInOrder;

        switch (a.type)
        {
            case ActionType.DrawEventCard:
                if (s.phase != TurnPhase.Event) return RuleResult.No("R-02", "Event cards are flipped only in the Event phase (now: " + PhaseName(s.phase) + ").");
                if (a.player != s.activePlayer) return RuleResult.No("R-03", "Only the first player (" + s.players[s.activePlayer].name + ") flips the event card.");
                if (s.eventDrawn) return RuleResult.No("R-03", "The event card for this round is already flipped.");
                return RuleResult.Ok();

            case ActionType.DrawBlockCard:
                // Card 17: the last player draws a second card after their first placement.
                bool secondCard = ev == 17 && s.phase == TurnPhase.Place && lastInOrder
                    && p.placementsThisRound == 1 && p.heldBlock == null
                    && s.blockDeck != null && s.blockDeck.TotalAvailable > 0;
                if (s.phase != TurnPhase.Draw && !secondCard)
                    return RuleResult.No("R-02", "Block cards are drawn only in the Draw phase (now: " + PhaseName(s.phase) + ").");
                if (ev == 18) return RuleResult.No("R-13", "Event 18 (Gas Line Scare): all play pauses this round.");
                if (!secondCard && p.drewThisRound) return RuleResult.No("R-04", p.name + " has already drawn a block card this round.");
                if (s.deckShort) return RuleResult.No("R-05", "The block deck has fewer cards than players, so nobody draws this round.");
                return RuleResult.Ok();

            case ActionType.PlaceBlock:
                if (s.phase == TurnPhase.Opening) return CheckOpening(s, a, p);
                if (s.phase != TurnPhase.Place) return RuleResult.No("R-02", "Blocks are placed only in the Build phase (now: " + PhaseName(s.phase) + ").");
                if (ev == 4) return RuleResult.No("R-13", "Event 4 (Coffee Break): everyone skips their placement this round.");
                if (ev == 18) return RuleResult.No("R-13", "Event 18 (Gas Line Scare): all play pauses this round.");
                if (p.placementsThisRound >= MaxPlacements(s, a.player))
                    return RuleResult.No("R-08", p.name + " has already had their placement" + (MaxPlacements(s, a.player) > 1 ? "s" : "") + " this round.");
                if (s.buildTurn != a.player)
                    return RuleResult.No("R-06", "It is " + Name(s, s.buildTurn) + "'s turn; players place in turn order, clockwise from the first player.");
                if (ev == 9 && !s.rivalPicked) return RuleResult.No("R-22", "Event 9 (Second Crew Arrives): " + Name(s, s.activePlayer) + " picks a rival before anyone places.");
                if (ev == 16 && !s.aftershocksDone) return RuleResult.No("R-23", "Event 16 (Aftershock Cluster): the board shakes " + BalanceConfig.Current.aftershockShakes + " times before any new placement.");
                if (p.heldBlock == null) return RuleResult.No("R-07", p.name + " has no block card to place.");
                if (a.card != p.heldBlock) return RuleResult.No("R-07", p.name + "'s block card is " + p.heldBlock.title + ", so only a " + p.heldBlock.title + " may be placed.");
                if (SupplyLeft(s, a.card) <= 0) return RuleResult.No("R-07", "No " + a.card.title + " is left in the supply, so " + p.name + " discards the card and draws again (R-26).");
                return CheckSpace(s, a, ev);

            case ActionType.RedrawBlock:
                if (ev != 1) return RuleResult.No("R-21", "You may redraw your block card only when event 1 (Site Inspector) is face up.");
                if (s.phase != TurnPhase.Place) return RuleResult.No("R-02", "The redraw happens in the Build phase (now: " + PhaseName(s.phase) + ").");
                if (s.buildTurn != a.player) return RuleResult.No("R-06", "Redraw on your own turn; it is " + Name(s, s.buildTurn) + "'s turn.");
                if (p.heldBlock == null) return RuleResult.No("R-21", p.name + " has no block card to redraw.");
                if (p.redrewThisRound) return RuleResult.No("R-21", p.name + " has already used the Site Inspector redraw this round.");
                if (s.blockDeck == null || s.blockDeck.TotalAvailable == 0) return RuleResult.No("R-21", "The block deck is empty, so there is nothing to redraw.");
                return RuleResult.Ok();

            case ActionType.SwapOption:
                if (ev != 13) return RuleResult.No("R-21", "Only event 13 (Permit Renewed) lets everyone draw 2 block cards and place one of them.");
                if (s.phase != TurnPhase.Place || s.buildTurn != a.player) return RuleResult.No("R-06", "Choose between your block cards on your own Build turn.");
                if (p.extraOption == null) return RuleResult.No("R-21", p.name + " has no extra block card.");
                if (SupplyLeft(s, p.extraOption) <= 0)
                    return RuleResult.No("R-07", "No " + p.extraOption.title + " is left in the supply, so " + p.name + " can't switch to it.");
                return RuleResult.Ok();

            case ActionType.PickRival:
                if (ev != 9) return RuleResult.No("R-22", "Only event 9 (Second Crew Arrives) lets the first player pick a rival.");
                if (s.phase != TurnPhase.Place) return RuleResult.No("R-02", "The rival is picked at the start of the Build phase (now: " + PhaseName(s.phase) + ").");
                if (a.player != s.activePlayer) return RuleResult.No("R-22", "Only the first player (" + Name(s, s.activePlayer) + ") picks the rival.");
                if (s.rivalPicked) return RuleResult.No("R-22", "A rival has already been picked this round.");
                if (a.target < 0 || a.target >= s.players.Count || a.target == a.player) return RuleResult.No("R-22", "Pick one of the other players as the rival.");
                return RuleResult.Ok();

            case ActionType.StartShake:
                if (ev == 18) return RuleResult.No("R-13", "Event 18 (Gas Line Scare): all play pauses, so there is no shake this round.");
                if (s.phase == TurnPhase.Place && ev == 16 && !s.aftershocksDone)
                {
                    if (a.player != s.activePlayer) return RuleResult.No("R-15", "Only the first player (" + s.players[s.activePlayer].name + ") runs the shake.");
                    return RuleResult.Ok();   // R-23: the two aftershocks before any new placement
                }
                if (s.phase == TurnPhase.Place)
                {
                    string waiting = WaitingOn(s);
                    return RuleResult.No("R-14", "Not every player has placed yet" + (waiting.Length > 0 ? " (waiting on " + waiting + ")" : "") + ".");
                }
                if (s.phase != TurnPhase.Shake) return RuleResult.No("R-02", "The board is shaken only in the Shake phase (now: " + PhaseName(s.phase) + ").");
                if (s.shakeDone) return RuleResult.No("R-02", "The board has already been shaken this round.");
                if (a.player != s.activePlayer) return RuleResult.No("R-15", "Only the first player (" + s.players[s.activePlayer].name + ") runs the shake.");
                return RuleResult.Ok();
        }
        return RuleResult.Ok();
    }

    static RuleResult CheckOpening(GameState s, GameAction a, PlayerState p)
    {
        if (p.placedOpening) return RuleResult.No("R-01", p.name + " has already made their opening placement this game.");
        if (s.buildTurn != a.player)
            return RuleResult.No("R-06", "It is " + Name(s, s.buildTurn) + "'s turn; the opening placement goes clockwise from the first player.");
        if (a.card == null || a.card.cardType != CardType.Block) return RuleResult.No("R-01", "The opening placement must be one block from the supply.");
        if (SupplyLeft(s, a.card) <= 0) return RuleResult.No("R-01", "No " + a.card.title + " is left in the supply.");
        return CheckSpace(s, a, 0);   // no event card is face up before round 1
    }

    static RuleResult CheckSpace(GameState s, GameAction a, int ev)
    {
        if (a.space < 1 || a.space > 16) return RuleResult.No("R-09", "The block must go on the tower base: on one of its 16 spaces or on top of a block there.");
        if (a.physical && a.offBase) return RuleResult.No("R-09", "Part of the block hangs off the tower base; every part must be over the 16 spaces.");

        // 3D play checks every space under the block's footprint; otherwise just the chosen space.
        int[] cells = a.covers != null && a.covers.Length > 0 ? a.covers : new[] { a.space };
        bool anyCenter = false, anyCorner = false, anyEdge = false;
        foreach (int c in cells)
        {
            SpaceData sp = s.board != null ? s.board.Get(c) : null;
            if (IsCenterHub(sp)) anyCenter = true;
            if (IsCorner(sp)) anyCorner = true;
            if (IsGridEdge(sp)) anyEdge = true;
        }
        bool onTop = a.physical ? a.onBlock : BlocksOn(s, a.space) > 0;

        if (ev == 6 && !onTop) return RuleResult.No("R-10", "Event 6 (Union Rep Visits): every block this round must go on top of another block, not directly on the base.");
        if (ev == 11 && anyCenter) return RuleResult.No("R-11", "Event 11 (Crane Swings Overhead): no placing on a Center Hub space this round.");
        if (ev == 12 && anyCorner) return RuleResult.No("R-17", "Event 12 (Wind Picks Up): no block may be placed on a Corner Zone space this round.");
        if (ev == 7 && !anyEdge) return RuleResult.No("R-12", "Event 7 (Scaffolding Creaks): your block must touch the grid edge (Corner Zone or Edge Zone).");
        return RuleResult.Ok();
    }

    // R-23: how many times the board shakes at the end of this round, and how fast.
    public static int ShakeCount(GameState s)
    {
        int ev = EventNumber(s);
        return ev == 5 || ev == 10 || ev == 19 ? BalanceConfig.Current.doubleShakeCount : 1;
    }

    public static float ShakeSpeed(GameState s) { return EventNumber(s) == 15 ? BalanceConfig.Current.fastShakeSpeed : 1f; }

    public static bool AftershocksPending(GameState s) { return EventNumber(s) == 16 && !s.aftershocksDone; }
    public static bool RivalPending(GameState s) { return EventNumber(s) == 9 && !s.rivalPicked; }
    public static bool Blind(GameState s) { return EventNumber(s) == 3; }         // R-25
    public static bool TenCount(GameState s) { return EventNumber(s) == 14; }     // R-24

    public static int MaxPlacements(GameState s, int player)
    {
        BalanceConfig b = BalanceConfig.Current;
        return EventNumber(s) == 17 && player == s.LastInOrder ? b.lastPlayerPlacements : b.placementsPerRound;
    }

    // Blocks of one style still in the supply: the copies on its card (5) minus those already on the tower this game.
    public static int SupplyLeft(GameState s, CardData style)
    {
        int used = 0;
        foreach (PlayerState q in s.players)
            foreach (GameAction g in q.placements) if (g.card == style) used++;
        return (style != null ? style.copies : 0) - used;
    }

    public static int BlocksOn(GameState s, int space)
    {
        int n = 0;
        foreach (PlayerState q in s.players)
            foreach (GameAction g in q.placements) if (g.space == space) n++;
        return n;
    }

    public static int EventNumber(GameState s)
    {
        if (s.phase == TurnPhase.Opening) return 0;
        if (s.eventCard == null || s.eventCard.cardType != CardType.Event) return 0;
        int n;
        return int.TryParse(s.eventCard.id, out n) ? n : 0;
    }

    public static bool IsCenterHub(SpaceData sp) { return sp != null && sp.tags.Contains("center"); }
    public static bool IsCorner(SpaceData sp) { return sp != null && sp.tags.Contains("corner"); }
    public static bool IsGridEdge(SpaceData sp) { return sp != null && sp.tags.Contains("edge"); }

    public static string PhaseName(TurnPhase ph)
    {
        switch (ph)
        {
            case TurnPhase.Place: return "Build";
            case TurnPhase.CollapseCheck: return "Check";
            case TurnPhase.Opening: return "Opening placement";
            default: return ph.ToString();
        }
    }

    static string Name(GameState s, int i) { return i >= 0 && i < s.players.Count ? s.players[i].name : "nobody"; }

    static string WaitingOn(GameState s)
    {
        List<string> names = new List<string>();
        foreach (int i in s.turnOrder)
        {
            PlayerState q = s.players[i];
            if (!q.skippedThisRound && q.placementsThisRound < MaxPlacements(s, i)) names.Add(q.name);
        }
        return string.Join(", ", names.ToArray());
    }
}
