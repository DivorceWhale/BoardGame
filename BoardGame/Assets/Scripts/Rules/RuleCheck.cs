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

// The legality checks. Every refusal names the rule id it comes from (see RuleBook).
public static class RuleCheck
{
    public static RuleResult Validate(GameState s, GameAction a)
    {
        if (!s.useRulesheet) return RuleResult.Ok();
        if (a.player < 0 || a.player >= s.players.Count) return RuleResult.No("R-01", "There is no player " + (a.player + 1) + ".");

        PlayerState p = s.players[a.player];
        int ev = EventNumber(s);

        switch (a.type)
        {
            case ActionType.DrawEventCard:
                if (!p.inGame) return RuleResult.No("R-01", p.name + " is out and does not flip event cards.");
                if (s.phase != TurnPhase.Event) return RuleResult.No("R-02", "Event cards are flipped only in the Event phase (now: " + PhaseName(s.phase) + ").");
                if (a.player != s.activePlayer) return RuleResult.No("R-03", "Only the first player (" + s.players[s.activePlayer].name + ") flips the event card.");
                if (s.eventDrawn) return RuleResult.No("R-03", "The event card for this round is already flipped.");
                return RuleResult.Ok();

            case ActionType.DrawBlockCard:
                if (!p.inGame) return RuleResult.No("R-01", p.name + " is out and does not draw.");
                if (s.phase != TurnPhase.Draw) return RuleResult.No("R-02", "Block cards are drawn only in the Draw phase (now: " + PhaseName(s.phase) + ").");
                if (ev == 18) return RuleResult.No("R-13", "Event 18 (Gas Line Scare): all play pauses this round.");
                if (p.drewThisRound) return RuleResult.No("R-04", p.name + " has already drawn a block card this round.");
                if (s.deckShort)   // decided once, at the start of the Draw phase
                    return RuleResult.No("R-05", "The block deck has fewer cards than standing players, so nobody draws this round.");
                return RuleResult.Ok();

            case ActionType.PlaceBlock:
                if (!p.inGame) return RuleResult.No("R-01", p.name + " is out and does not place.");
                if (s.phase != TurnPhase.Place) return RuleResult.No("R-02", "Blocks are placed only in the Build phase (now: " + PhaseName(s.phase) + ").");
                if (ev == 4) return RuleResult.No("R-13", "Event 4 (Coffee Break): everyone skips their placement this round.");
                if (ev == 18) return RuleResult.No("R-13", "Event 18 (Gas Line Scare): all play pauses this round.");
                if (p.placedThisRound || p.skippedThisRound) return RuleResult.No("R-08", p.name + " has already had their 1 placement this round.");
                if (s.buildTurn != a.player)
                    return RuleResult.No("R-06", "It is " + (s.buildTurn >= 0 ? s.players[s.buildTurn].name : "nobody") + "'s turn to place; the Build phase goes clockwise from the first player.");
                if (p.heldBlock == null) return RuleResult.No("R-07", p.name + " has no block card this round.");
                if (a.card != p.heldBlock) return RuleResult.No("R-07", p.name + "'s block card is " + p.heldBlock.title + ", so only a " + p.heldBlock.title + " may be placed.");
                if (s.ownBases && a.baseOwner != a.player) return RuleResult.No("R-09", p.name + " may place only on their own tower base, not on " + s.players[a.baseOwner].name + "'s.");
                if (a.space < 1 || a.space > 16) return RuleResult.No("R-10", "Space " + a.space + " is not one of the 16 spaces of a tower base.");
                SpaceData sp = s.board != null ? s.board.Get(a.space) : null;
                if (ev == 11 && IsCenterHub(sp)) return RuleResult.No("R-11", "Event 11 (Crane Swings Overhead): no placing on a Center Hub space this round.");
                if (ev == 7 && !IsGridEdge(sp)) return RuleResult.No("R-12", "Event 7 (Scaffolding Creaks): your block must go on a grid-edge space (Corner Zone or Edge Zone).");
                return RuleResult.Ok();

            case ActionType.StartShake:
                if (ev == 18) return RuleResult.No("R-13", "Event 18 (Gas Line Scare): all play pauses, so there is no shake this round.");
                if (s.phase == TurnPhase.Place)
                {
                    string waiting = WaitingOn(s);
                    return RuleResult.No("R-14", "Not every standing player has placed yet" + (waiting.Length > 0 ? " (waiting on " + waiting + ")" : "") + ".");
                }
                if (s.phase != TurnPhase.Shake) return RuleResult.No("R-02", "The board is shaken only in the Shake phase (now: " + PhaseName(s.phase) + ").");
                if (s.shakeDone) return RuleResult.No("R-02", "The board has already been shaken this round.");
                bool anyOut = false;
                foreach (PlayerState q in s.players) if (!q.inGame) anyOut = true;
                if (anyOut && p.inGame) return RuleResult.No("R-15", "A player is out, so an out player runs the shake, not " + p.name + ".");
                if (!anyOut && a.player != s.activePlayer) return RuleResult.No("R-15", "Only the first player (" + s.players[s.activePlayer].name + ") runs the shake.");
                return RuleResult.Ok();
        }
        return RuleResult.Ok();
    }

    public static int EventNumber(GameState s)
    {
        if (s.eventCard == null || s.eventCard.cardType != CardType.Event) return 0;
        int n;
        return int.TryParse(s.eventCard.id, out n) ? n : 0;
    }

    public static int StandingCount(GameState s)
    {
        int n = 0;
        foreach (PlayerState p in s.players) if (p.inGame) n++;
        return n;
    }

    public static bool IsCenterHub(SpaceData sp) { return sp != null && sp.tags.Contains("center"); }
    public static bool IsGridEdge(SpaceData sp) { return sp != null && sp.tags.Contains("edge"); }

    public static string PhaseName(TurnPhase ph)
    {
        switch (ph)
        {
            case TurnPhase.Place: return "Build";
            case TurnPhase.CollapseCheck: return "Check";
            default: return ph.ToString();
        }
    }

    static string WaitingOn(GameState s)
    {
        string list = "";
        foreach (PlayerState q in s.players)
        {
            if (q.inGame && !q.placedThisRound && !q.skippedThisRound)
                list += (list.Length > 0 ? ", " : "") + q.name;
        }
        return list;
    }
}
