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
    };

    // Everything that varies comes from the scenario asset: players, board, decks, victory, limits.
    public TurnStateMachine(DeckManager blockDeck, DeckManager eventDeck, ScenarioConfig scenario)
    {
        State.blockDeck = blockDeck;
        State.eventDeck = eventDeck;
        State.scenario = scenario;
        State.board = scenario.board;
        for (int i = 0; i < scenario.playerCount; i++)
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
                Log("    " + p.name + " draws block: "
                    + (p.heldBlock != null ? p.heldBlock.title : "none"));
                break;

            case ActionType.PlaceBlock:
                p.placements.Add(a);
                NoteHeight(p, a);
                State.blockDeck.Discard(a.card);   // used cards go to the discard pile (only recycled if refills are on)
                p.heldBlock = null;
                Log("    " + p.name + " places " + a);
                break;
        }
    }

    // PLACEHOLDER: the real shake is a physical motor. The chances come from the scenario asset.
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
            if (standing.Count == 0)
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

    private void FinishGame(PlayerState winner)
    {
        ScenarioConfig sc = State.scenario;
        winner.matchPoints += sc.pointsPerGameWin;
        Log("  " + winner.name + " wins game " + State.gameNumber + " (match points: " + winner.matchPoints + ").");

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
            Log("  " + leader.name + " wins the match.");
            return;
        }
        StartNewGame();
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

        int n = State.players.Count;
        for (int i = 1; i <= n; i++)
        {
            int next = (State.activePlayer + i) % n;
            if (State.players[next].inGame)
            {
                State.activePlayer = next;
                break;
            }
        }
    }
}
