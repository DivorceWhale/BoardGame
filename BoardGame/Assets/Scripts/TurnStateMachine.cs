using System;
using System.Collections.Generic;

public class TurnStateMachine
{
    public readonly GameState State = new GameState();
    public bool MatchOver { get; private set; }
    public PlayerState MatchWinner { get; private set; }
    public event Action<string> Log = delegate { };

    // Explicit transitions, in the order on the phase card.
    private static readonly Dictionary<TurnPhase, TurnPhase> Transitions = new Dictionary<TurnPhase, TurnPhase>
    {
        { TurnPhase.Event,         TurnPhase.Draw },
        { TurnPhase.Draw,          TurnPhase.Place },
        { TurnPhase.Place,         TurnPhase.Shake },
        { TurnPhase.Shake,         TurnPhase.CollapseCheck },
        { TurnPhase.CollapseCheck, TurnPhase.EndCheck },
        { TurnPhase.EndCheck,      TurnPhase.Event },   // end of turn: next round, next player
        { TurnPhase.Opening,       TurnPhase.Event },   // rulesheet v2: opening placement, then round 1
    };

    // Everything that varies comes from the scenario asset: players, board, decks, victory, limits.
    // Called with the number of the game about to start, just before its decks are shuffled.
    public event System.Action<int> BeforeNewGameShuffle;

    public TurnStateMachine(DeckManager blockDeck, DeckManager eventDeck, ScenarioConfig scenario, int playerCount = 0)
    {
        State.blockDeck = blockDeck;
        State.eventDeck = eventDeck;
        State.scenario = scenario;
        State.board = scenario.board;
        int count = playerCount > 0 ? playerCount : scenario.playerCount;
        for (int i = 0; i < count; i++)
        {
            State.players.Add(new PlayerState
            {
                index = i,
                name = "Player " + (i + 1),
                matchPoints = scenario.startingPointsPerPlayer
            });
        }
        ApplyStartingBlocks();
    }

    private void NoteHeight(PlayerState p, GameAction placed)
    {
        if (p.placements.Count == 1) p.baseSpace = placed.space;
        if (p.TowerHeight > p.peakHeight) p.peakHeight = p.TowerHeight;
    }

    private void ApplyStartingBlocks()
    {
        ScenarioConfig sc = State.scenario;
        foreach (StartingBlock sb in sc.startingBlocks)
        {
            if (sb.block == null || sb.playerIndex < 0 || sb.playerIndex >= State.players.Count) continue;
            if (!sc.IsOpen(sb.spaceId))
            {
                Log("  Starting block skipped: space " + sb.spaceId + " is blocked in this scenario.");
                continue;
            }
            SpaceData space = State.board != null ? State.board.Get(sb.spaceId) : null;
            PlayerState p = State.players[sb.playerIndex];
            GameAction placed = new GameAction
            {
                type = ActionType.PlaceBlock,
                player = sb.playerIndex,
                card = sb.block,
                space = sb.spaceId,
                spaceName = space != null ? space.displayName : "?",
                angle = sb.angle
            };
            p.placements.Add(placed);
            NoteHeight(p, placed);
        }
    }

    public void StepPhase()
    {
        if (MatchOver) return;
        GameState s = State;

        Log("=== Game " + s.gameNumber + ", round " + s.round + " (round " + s.roundInGame + " of this game), phase " + s.phase
            + " (active player: " + s.players[s.activePlayer].name + ") ===");

        // Seat order starting from the active player. PLACEHOLDER decisions: a random legal move.
        for (int i = 0; i < s.players.Count; i++)
        {
            PlayerState p = s.players[(s.activePlayer + i) % s.players.Count];
            List<GameAction> legal = LegalMoves.Get(s, p);
            Log("  " + p.name + " legal actions: " + LegalMoves.Describe(s, legal));
            if (legal.Count > 0)
            {
                Apply(legal[GameRandom.Instance.Next(legal.Count)]);
            }
        }

        switch (s.phase)
        {
            case TurnPhase.Shake: DoShake(); break;
            case TurnPhase.CollapseCheck: DoCollapseCheck(); break;
            case TurnPhase.EndCheck: DoEndCheck(); break;
        }

        TurnPhase finished = s.phase;
        s.phase = Transitions[finished];
        if (finished == TurnPhase.EndCheck && !MatchOver)
        {
            EndOfTurn();
        }
    }

    // Build 7: the direct path. Any caller (UI, AI, test) can try an action; it is checked against
    // the rulesheet first and refused with the rule id instead of being applied and corrected later.
    public RuleResult TryApply(GameAction a)
    {
        RuleResult r = MatchOver
            ? RuleResult.No("R-18", "The match is over. Start a new match to keep playing.")
            : LegalMoves.Validate(State, a);
        if (!r.legal)
        {
            Log("    " + r);
            return r;
        }
        Apply(a);
        return r;
    }

    private void Apply(GameAction a)
    {
        PlayerState p = State.players[a.player];
        switch (a.type)
        {
            case ActionType.DrawEventCard:
                State.eventCard = State.eventDeck.DrawData();
                State.eventDrawn = true;
                Log("    " + p.name + " draws event card: "
                    + (State.eventCard != null ? State.eventCard.title : "none"));
                break;

            case ActionType.DrawBlockCard:
                p.heldBlock = State.blockDeck.DrawData();
                p.drewThisRound = true;
                Log("    " + p.name + " draws block: "
                    + (p.heldBlock != null ? p.heldBlock.title : "none"));
                if (State.useRulesheet && State.phase == TurnPhase.Draw && RuleCheck.EventNumber(State) == 13 && State.blockDeck.TotalAvailable > 0)
                {
                    p.extraOption = State.blockDeck.DrawData();   // R-21: Permit Renewed, an extra block style option
                    Log("    R-21: " + p.name + " also draws an extra option: " + (p.extraOption != null ? p.extraOption.title : "none"));
                }
                break;

            case ActionType.RedrawBlock:
                State.blockDeck.Discard(p.heldBlock);
                p.heldBlock = State.blockDeck.DrawData();
                p.redrewThisRound = true;
                Log("    R-21: " + p.name + " redraws (Site Inspector) and gets " + (p.heldBlock != null ? p.heldBlock.title : "nothing") + ".");
                break;

            case ActionType.SwapOption:
                CardData first = p.heldBlock;
                p.heldBlock = p.extraOption;
                p.extraOption = first;
                Log("    R-21: " + p.name + " will place " + (p.heldBlock != null ? p.heldBlock.title : "?") + " instead.");
                break;

            case ActionType.PickRival:
                State.rivalPicked = true;
                PlayerState rival = State.players[a.target];
                if (rival.heldBlock != null && State.blockDeck.TotalAvailable > 0)
                {
                    State.blockDeck.Discard(rival.heldBlock);
                    rival.heldBlock = State.blockDeck.DrawData();
                    Log("    R-22: " + p.name + " picks " + rival.name + " as the rival, who redraws and gets " + (rival.heldBlock != null ? rival.heldBlock.title : "nothing") + ".");
                }
                else
                {
                    Log("    R-22: " + p.name + " picks " + rival.name + " as the rival, but " + (rival.heldBlock == null ? "they have no block card" : "the block deck is empty") + ", so nothing changes.");
                }
                break;

            case ActionType.PlaceBlock:
                p.placements.Add(a);
                NoteHeight(p, a);
                State.lastPlacer = a.player;
                State.tower.Add(a);
                if (State.phase == TurnPhase.Opening)
                {
                    p.placedOpening = true;            // R-01: chosen from the supply, no card involved
                    Log("    " + p.name + " makes their opening placement: " + a);
                    break;
                }
                State.blockDeck.Discard(a.card);   // used cards go to the discard pile (only recycled if refills are on)
                p.heldBlock = null;
                if (p.extraOption != null) { State.blockDeck.Discard(p.extraOption); p.extraOption = null; }
                p.placedThisRound = true;
                p.placementsThisRound++;
                Log("    " + p.name + " places " + a);
                break;

            case ActionType.StartShake:
                if (State.useRulesheet && State.phase == TurnPhase.Place)
                {
                    State.aftershocksDone = true;   // R-23: event 16, two shakes before any new placement
                    Log("    R-23: " + p.name + " runs the aftershocks: the board shakes " + BalanceConfig.Current.aftershockShakes + " times before anyone places.");
                    break;
                }
                State.shakeDone = true;
                foreach (PlayerState q in State.players) q.blocksBeforeShake = q.TowerHeight;
                Log("    " + p.name + " starts the shake.");
                break;
        }
    }

    // PLACEHOLDER: this text-only shake rolls for falls. The chances come from the scenario asset.
    private void DoShake()
    {
        ScenarioConfig sc = State.scenario;
        foreach (PlayerState p in State.players) p.fellThisShake = false;

        // Harder shake: once a tower on a trigger-tagged space has fallen, towers on target-tagged spaces face a boost.
        bool boostOn = false;
        if (!string.IsNullOrEmpty(sc.shakeBoostTriggerTag) && State.board != null)
        {
            foreach (PlayerState p in State.players)
            {
                if (!p.inGame && p.baseSpace >= 0 && State.board.HasTag(p.baseSpace, sc.shakeBoostTriggerTag)) boostOn = true;
            }
        }

        foreach (PlayerState p in State.players)
        {
            if (!p.inGame) continue;
            float chance = sc.placeholderCollapseChance;
            if (boostOn && p.baseSpace >= 0 && State.board.HasTag(p.baseSpace, sc.shakeBoostTargetTag))
            {
                chance = Math.Min(1f, chance + sc.shakeBoostChance);
            }
            p.fellThisShake = GameRandom.Instance.Next(1000) < (int)(chance * 1000f);
        }
        Log("  Platform shakes all towers." + (boostOn ? " " + sc.shakeBoostTargetTag + " towers face a harder shake." : ""));
    }

    private void DoCollapseCheck()
    {
        foreach (PlayerState p in State.players)
        {
            if (p.fellThisShake)
            {
                p.inGame = false;
                Log("  " + p.name + "'s tower collapsed: out for this game.");
            }
        }
    }

    private PlayerState Tallest(List<PlayerState> group)
    {
        PlayerState best = null;
        foreach (PlayerState p in group)
        {
            if (best == null || p.TowerHeight > best.TowerHeight) best = p;   // ties go to seat order (PLACEHOLDER)
        }
        return best;
    }

    private PlayerState LeaderByMargin(List<PlayerState> standing, int margin)
    {
        foreach (PlayerState candidate in standing)
        {
            bool leads = true;
            foreach (PlayerState other in standing)
            {
                if (other != candidate && candidate.TowerHeight < other.TowerHeight + margin) leads = false;
            }
            if (leads) return candidate;
        }
        return null;
    }

    private void DoEndCheck()
    {
        ScenarioConfig sc = State.scenario;
        List<PlayerState> standing = new List<PlayerState>();
        List<PlayerState> fellNow = new List<PlayerState>();
        foreach (PlayerState p in State.players)
        {
            if (p.inGame) standing.Add(p);
            if (p.fellThisShake) fellNow.Add(p);
        }

        PlayerState winner = null;
        bool limitReached = sc.roundLimit > 0 && State.roundInGame >= sc.roundLimit;

        switch (sc.victoryType)
        {
            case VictoryType.TargetHeight:
                if (sc.targetHeight > 0)
                {
                    List<PlayerState> reached = new List<PlayerState>();
                    foreach (PlayerState p in standing) if (p.TowerHeight >= sc.targetHeight) reached.Add(p);
                    winner = Tallest(reached);
                }
                break;

            case VictoryType.LeadByMargin:
                if (sc.leadMargin > 0 && standing.Count > 0) winner = LeaderByMargin(standing, sc.leadMargin);
                break;

            case VictoryType.TallestAtRoundLimit:
                if (limitReached && standing.Count > 0)
                {
                    winner = Tallest(standing);
                    Log("  Round limit reached: the tallest standing tower wins.");
                }
                break;
        }

        if (winner == null)
        {
            if (State.useRulesheet)
            {
                // Rulesheet v2: one shared tower, so a game ends only when it falls (R-16).
                if (State.towerFell) ResolveTowerFall();
                else Log("  The tower is still standing. The game continues.");
                return;
            }
            else if (standing.Count == 0)
            {
                winner = Tallest(fellNow);   // last towers fell together: the tallest of them wins
            }
            else if (standing.Count == 1 && sc.victoryType != VictoryType.TallestAtRoundLimit)
            {
                winner = standing[0];
            }
            else if (limitReached && sc.victoryType != VictoryType.TallestAtRoundLimit)
            {
                winner = Tallest(standing);  // PLACEHOLDER: the posts only define the limit for Fuse Lit
                Log("  Round limit reached.");
            }
            else if (!sc.refillWhenEmpty && State.blockDeck.TotalAvailable < standing.Count)
            {
                winner = Tallest(standing);  // PLACEHOLDER: no refills and not enough blocks for another round
                Log("  The block supply has run out.");
            }
        }

        if (winner == null)
        {
            Log("  The game continues.");
            return;
        }

        FinishGame(winner);
    }

    private List<PlayerState> MostBlocks(List<PlayerState> group)
    {
        int best = -1;
        foreach (PlayerState p in group) if (p.blocksBeforeShake > best) best = p.blocksBeforeShake;
        List<PlayerState> top = new List<PlayerState>();
        foreach (PlayerState p in group) if (p.blocksBeforeShake == best) top.Add(p);
        return top;
    }

    private void FinishGame(PlayerState winner, List<PlayerState> sharedWith = null)
    {
        ScenarioConfig sc = State.scenario;
        List<PlayerState> winners = sharedWith ?? new List<PlayerState> { winner };
        foreach (PlayerState w in winners)
        {
            w.matchPoints += sc.pointsPerGameWin;
            Log("  " + (State.useRulesheet ? "R-16: " : "") + w.name + " wins game " + State.gameNumber + " (match points: " + w.matchPoints + ").");
        }

        if (sc.consolationHeight > 0 && sc.consolationPoints > 0)
        {
            foreach (PlayerState p in State.players)
            {
                if (p.peakHeight >= sc.consolationHeight)
                {
                    p.matchPoints += sc.consolationPoints;
                    Log("  " + p.name + " banks " + sc.consolationPoints + " consolation point(s): reached "
                        + p.peakHeight + " blocks (match points: " + p.matchPoints + ").");
                }
            }
        }

        PlayerState leader = null;
        foreach (PlayerState p in State.players)
        {
            if (p.matchPoints >= sc.pointsToWinMatch && (leader == null || p.matchPoints > leader.matchPoints)) leader = p;
        }
        if (leader != null)
        {
            MatchOver = true;
            MatchWinner = leader;
            Log("  " + (State.useRulesheet ? "R-18: " : "") + leader.name + " wins the match.");
            return;
        }
        StartNewGame();
    }

    // ---------- Rulesheet v2: one shared tower ----------

    // Before round 1 of every game: each player, clockwise from the first player, places 1 block of their choice (R-01).
    public void BeginOpening()
    {
        State.phase = TurnPhase.Opening;
        State.towerFell = false;
        State.lastPlacer = -1;
        State.eventCard = null;
        State.eventDrawn = false;
        State.shakeDone = false;
        if (State.turnOrder.Count > 0) State.activePlayer = State.turnOrder[0];
        State.buildTurn = State.activePlayer;
        foreach (PlayerState p in State.players)
        {
            p.placedOpening = false;
            p.placementsThisRound = 0;
            p.placedThisRound = false;
            p.skippedThisRound = false;
            p.drewThisRound = false;
            p.redrewThisRound = false;
            p.heldBlock = null;
            p.extraOption = null;
        }
        Log("  Opening placement for game " + State.gameNumber + ": clockwise from the first player, 1 block of your choice each.");
    }

    // The table reports the tower has fallen. R-16: the most recent placer loses; everyone else scores.
    public void ResolveTowerFall()
    {
        ScenarioConfig sc = State.scenario;
        State.towerFell = true;
        PlayerState loser = State.lastPlacer >= 0 ? State.players[State.lastPlacer] : null;
        Log("  R-16: the tower has fallen. " + (loser != null ? loser.name + " placed the most recent block and loses game " + State.gameNumber + "." : "Nobody had placed a block."));

        foreach (PlayerState p in State.players)
        {
            if (p == loser) continue;
            p.matchPoints += sc.pointsPerGameWin;
            Log("    " + p.name + " scores " + sc.pointsPerGameWin + (sc.pointsPerGameWin == 1 ? " point" : " points") + " (" + p.matchPoints + ").");
        }
        if (loser != null && RuleCheck.EventNumber(State) == 20 && loser.matchPoints > 0)
        {
            int loss = BalanceConfig.Current.event20PointLoss;
            loser.matchPoints = Math.Max(0, loser.matchPoints - loss);
            Log("    R-20: event 20 is face up, so " + loser.name + " loses " + loss + " point" + (loss == 1 ? "" : "s") + " (" + loser.matchPoints + ").");
        }

        List<PlayerState> champions = new List<PlayerState>();
        foreach (PlayerState p in State.players) if (p.matchPoints >= sc.pointsToWinMatch) champions.Add(p);
        if (champions.Count > 0)
        {
            MatchOver = true;
            MatchWinner = champions[0];
            List<string> names = new List<string>();
            foreach (PlayerState p in champions) names.Add(p.name);
            Log("  R-18: " + string.Join(" and ", names.ToArray()) + (champions.Count > 1 ? " share the match win." : " wins the match."));
            return;
        }
        StartNewGame();
        BeginOpening();
    }

    // ---------- Rulesheet v2: live play (the Play scene) ----------
    // Play = TryApply, then move the round along (section 4). Build7RuleDemo drives phases by hand
    // and keeps calling TryApply directly.

    public RuleResult Play(GameAction a)
    {
        TurnPhase before = State.phase;
        RuleResult r = TryApply(a);
        if (!r.legal) return r;

        switch (a.type)
        {
            case ActionType.PlaceBlock:
                if (before == TurnPhase.Opening) NextOpeningTurn();
                else NextBuildTurn();
                break;
            case ActionType.DrawEventCard:
                ToDrawPhase();
                break;
            case ActionType.StartShake:
                if (before == TurnPhase.Place) break;   // event 16 aftershocks: the Build phase carries on
                State.phase = TurnPhase.CollapseCheck;
                Log("  Check phase: once the board is still, has the tower collapsed?");
                break;
        }
        Settle();
        return r;
    }

    // R-24 (event 14): the current builder ran out of their ten-count and skips their placement.
    public void MissedTenCount()
    {
        if (MatchOver || State.phase != TurnPhase.Place || State.buildTurn < 0) return;
        SkipPlacement(State.players[State.buildTurn], "R-24, the ten-count ran out.");
        Settle();
    }

    // Start of every round: the first player is about to flip the event card.
    public void BeginRound()
    {
        State.phase = TurnPhase.Event;
        State.buildTurn = -1;
        State.shakeDone = false;
        State.deckShort = false;
        State.rivalPicked = false;
        State.aftershocksDone = false;
        deadRedraws.Clear();
        if (State.turnOrder.Count > 0) State.activePlayer = State.turnOrder[0];

        // R-19 (team rule change): once the block deck is too short for everyone to draw, rounds are shake-only.
        // No event card is flipped either, so nothing changes the shake except the shake-only escalation.
        if (State.useRulesheet && State.blockDeck != null && State.blockDeck.TotalAvailable < State.players.Count)
        {
            RoundsUnderway++;   // the round is underway at once (there is no Event phase to start it)
            State.deckShort = true;
            State.phase = TurnPhase.Shake;
            Log("=== Game " + State.gameNumber + ", round " + State.roundInGame + ": shake-only round ===");
            Log("  R-19: the block deck has fewer cards than players, so there is no event card, no drawing and no placing. The first player starts the shake.");
            return;
        }
        Log("=== Game " + State.gameNumber + ", round " + State.roundInGame + ": Event phase ===");
    }

    // Check phase, tower still standing: cards to the discard piles and a new round (section 4).
    public void TowerStands()
    {
        if (MatchOver || State.phase != TurnPhase.CollapseCheck) return;
        foreach (PlayerState p in State.players)
        {
            if (p.heldBlock != null) State.blockDeck.Discard(p.heldBlock);
            if (p.extraOption != null) State.blockDeck.Discard(p.extraOption);
            p.heldBlock = null;
            p.extraOption = null;
        }
        Log("  The tower is still standing.");
        EndOfTurn();
        PassFirstPlayer();
        BeginRound();
    }

    // At the end of every round the first player passes clockwise: the turn order turns by one seat,
    // so the next player clockwise flips the event card, places first and runs the shake. It keeps
    // turning from game to game; the tallest player is only the first player in round 1 of the match.
    // Seats per round: BalanceConfig.firstPlayerSeatsPerRound (table change, playtest 2: 0 -> 1).
    public void PassFirstPlayer()
    {
        int seats = BalanceConfig.Current.firstPlayerSeatsPerRound;
        if (State.turnOrder.Count < 2 || seats <= 0) return;
        for (int k = 0; k < seats % State.turnOrder.Count; k++)
        {
            int first = State.turnOrder[0];
            State.turnOrder.RemoveAt(0);
            State.turnOrder.Add(first);
        }
        State.activePlayer = State.turnOrder[0];
        Log("  The first player passes clockwise to " + State.players[State.turnOrder[0]].name + ".");
    }

    // The physics check saw the tower fall, after a placement or after the shake. R-16: the most recent
    // placer loses, which is also the player who was placing if it fell right after their block (section 1).
    public void TowerFell()
    {
        if (MatchOver) return;
        // The round ends with the game, so the next game starts one seat on. A fall from the opening
        // placement comes before round 1 (the last opening block has already moved the phase to Event,
        // but no event card is flipped yet), so the first player stays put.
        bool roundUnderway = State.phase != TurnPhase.Opening && State.phase != TurnPhase.Event;
        if (roundUnderway) PassFirstPlayer();
        ResolveTowerFall();
        if (!MatchOver) Settle();
    }

    // R-26 (team rule change): a block card that can't be placed (none of that style left in the supply, or no
    // space fits it) is discarded and the player draws again. Each player gets at most MaxDeadRedraws of these per
    // round, so a round where nothing can fit doesn't burn through the whole block deck; after that they skip.
    public const int MaxDeadRedraws = 5;
    readonly Dictionary<int, int> deadRedraws = new Dictionary<int, int>();
    public string LastDeadRedraw { get; private set; }   // "Z-Block -> L-Block", for the UI banner

    public bool RedrawDeadCard(PlayerState p, string why)
    {
        if (MatchOver || p == null || p.heldBlock == null || State.phase != TurnPhase.Place) return false;
        int ev = RuleCheck.EventNumber(State);
        if (ev == 4 || ev == 18) return false;   // Coffee Break and Gas Line Scare: everyone skips, whatever card they hold
        if (State.blockDeck == null || State.blockDeck.TotalAvailable <= 0) return false;
        int used; deadRedraws.TryGetValue(p.index, out used);
        if (used >= MaxDeadRedraws) return false;
        deadRedraws[p.index] = used + 1;

        CardData dead = p.heldBlock;
        State.blockDeck.Discard(dead);
        if (p.extraOption != null) State.blockDeck.Discard(p.extraOption);
        p.extraOption = null;
        p.heldBlock = State.blockDeck.DrawData();
        LastDeadRedraw = dead.title + " -> " + (p.heldBlock != null ? p.heldBlock.title : "nothing");
        Log("    R-26: " + p.name + "'s " + dead.title + " can't be placed (" + why.TrimEnd('.') + "), so they discard it and draw "
            + (p.heldBlock != null ? p.heldBlock.title : "nothing") + ".");
        Settle();
        return true;
    }

    public void SkipPlacement(PlayerState p, string why)
    {
        p.skippedThisRound = true;
        if (p.heldBlock != null) State.blockDeck.Discard(p.heldBlock);
        if (p.extraOption != null) State.blockDeck.Discard(p.extraOption);
        p.heldBlock = null;
        p.extraOption = null;
        Log("    " + p.name + " skips their placement" + (string.IsNullOrEmpty(why) ? "." : ": " + why));
        NextBuildTurn();
    }

    // How many rounds have got past their Event phase (an event card was flipped) over the whole match.
    public int RoundsUnderway { get; private set; }

    void ToDrawPhase()
    {
        RoundsUnderway++;
        State.phase = TurnPhase.Draw;
        State.deckShort = State.blockDeck.TotalAvailable < State.players.Count;
        Log("  Draw phase: each player draws 1 block card.");
        if (State.deckShort)
            Log("  R-05 / R-19: the block deck has fewer cards than players, so nobody draws or places this round.");
    }

    void ToBuildPhase()
    {
        State.phase = TurnPhase.Place;
        State.buildTurn = State.turnOrder.Count > 0 ? State.turnOrder[0] : -1;
        Log("  Build phase: clockwise from the first player, each player places the block on their card.");
        NextBuildTurn();
    }

    void NextOpeningTurn()
    {
        State.buildTurn = -1;
        foreach (int i in State.turnOrder)
        {
            if (!State.players[i].placedOpening) { State.buildTurn = i; return; }
        }
    }

    // The builder keeps the turn while they still have a placement left (card 17), then it passes down the order.
    void NextBuildTurn()
    {
        int from = State.buildTurn >= 0 ? Math.Max(0, State.OrderPosition(State.buildTurn)) : 0;
        for (int k = from; k < State.turnOrder.Count; k++)
        {
            int i = State.turnOrder[k];
            PlayerState q = State.players[i];
            if (!q.skippedThisRound && q.placementsThisRound < RuleCheck.MaxPlacements(State, i))
            {
                State.buildTurn = i;
                return;
            }
        }
        State.buildTurn = -1;
    }

    // Moves past any step where nobody can legally act (section 7, "No legal move"; events 4 and 18; R-05).
    void Settle()
    {
        for (int guard = 0; guard < 100 && !MatchOver; guard++)
        {
            GameState s = State;
            switch (s.phase)
            {
                case TurnPhase.Opening:
                    if (s.buildTurn < 0) { Log("  Opening placement done."); BeginRound(); continue; }
                    if (LegalMoves.Get(s, s.players[s.buildTurn]).Count == 0)
                    {
                        s.players[s.buildTurn].placedOpening = true;
                        Log("    " + s.players[s.buildTurn].name + " has no legal opening placement and skips.");
                        NextOpeningTurn();
                        continue;
                    }
                    return;

                case TurnPhase.Draw:
                    bool allDrew = true, anyCanDraw = false;
                    foreach (PlayerState p in s.players)
                    {
                        if (!p.drewThisRound) allDrew = false;
                        if (LegalMoves.IsLegal(s, new GameAction { type = ActionType.DrawBlockCard, player = p.index })) anyCanDraw = true;
                    }
                    if (allDrew || !anyCanDraw) { ToBuildPhase(); continue; }
                    return;

                case TurnPhase.Place:
                    // Events 9 and 16: the first player acts before anyone places.
                    if (RuleCheck.RivalPending(s) || RuleCheck.AftershocksPending(s)) return;
                    if (s.buildTurn < 0)
                    {
                        s.phase = TurnPhase.Shake;
                        Log("  Shake phase: the first player starts the shake for " + BalanceConfig.Current.shakeSeconds + " seconds.");
                        continue;
                    }
                    PlayerState builder = s.players[s.buildTurn];
                    if (LegalMoves.Get(s, builder).Count == 0)
                    {
                        // R-26: a card that can't be placed is discarded and redrawn; skip only if that's impossible.
                        if (RedrawDeadCard(builder, WhyNoPlacement(builder))) continue;
                        SkipPlacement(builder, WhyNoPlacement(builder));
                        continue;
                    }
                    return;

                case TurnPhase.Shake:
                    if (!LegalMoves.IsLegal(s, new GameAction { type = ActionType.StartShake, player = s.activePlayer }))
                    {
                        Log("  No shake this round.");
                        s.phase = TurnPhase.CollapseCheck;
                        continue;
                    }
                    return;

                default:
                    return;
            }
        }
    }

    string WhyNoPlacement(PlayerState p)
    {
        if (p.heldBlock == null) return "no block card this round.";
        if (RuleCheck.SupplyLeft(State, p.heldBlock) <= 0) return "no " + p.heldBlock.title + " is left in the supply.";
        RuleResult r = RuleCheck.Validate(State, new GameAction { type = ActionType.PlaceBlock, player = p.index, card = p.heldBlock, space = 1 });
        return r.legal ? "no legal space for the block." : r.ruleId + ", " + r.reason;
    }

    private void StartNewGame()
    {
        foreach (PlayerState p in State.players)
        {
            p.inGame = true;
            p.fellThisShake = false;
            p.placements.Clear();
            p.heldBlock = null;
            p.peakHeight = 0;
            p.baseSpace = -1;
        }
        State.tower.Clear();
        if (State.useRulesheet)
        {
            // Rulesheet v2 setup, steps 4-5: both decks are shuffled fresh for every game.
            if (BeforeNewGameShuffle != null) BeforeNewGameShuffle(State.gameNumber + 1);   // e.g. a new random seed per game
            State.blockDeck.Configure(State.scenario.blockDeck);
            State.eventDeck.Configure(State.scenario.eventDeck);
        }
        ApplyStartingBlocks();
        State.gameNumber++;
        State.roundInGame = 1;
        Log("  Towers rebuilt. Starting game " + State.gameNumber + ".");
    }

    // End-of-turn transition: clear the event, advance the round, pass play to the next player.
    private void EndOfTurn()
    {
        if (State.eventCard != null) State.eventDeck.Discard(State.eventCard);
        State.eventCard = null;
        State.eventDrawn = false;
        State.round++;
        State.roundInGame++;
        State.shakeDone = false;
        foreach (PlayerState p in State.players)
        {
            p.drewThisRound = false;
            p.placedThisRound = false;
            p.skippedThisRound = false;
            p.redrewThisRound = false;
            p.extraOption = null;
        }

        foreach (PlayerState p in State.players)
        {
            p.placementsThisRound = 0;
            p.heldBlock = null;
        }
        if (State.useRulesheet && State.turnOrder.Count > 0)
        {
            State.activePlayer = State.turnOrder[0];   // rulesheet v2: whoever the first player has passed to
            return;
        }

        int n = State.players.Count;
        for (int i = 1; i <= n; i++)
        {
            int next = (State.activePlayer + i) % n;
            if (State.players[next].inGame)
            {
                State.activePlayer = next;   // R-20: first player passes clockwise to the next standing player
                break;
            }
        }
    }
}
