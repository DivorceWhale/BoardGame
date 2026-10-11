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

    public TurnStateMachine(DeckManager blockDeck, DeckManager eventDeck, int playerCount)
    {
        State.blockDeck = blockDeck;
        State.eventDeck = eventDeck;
        for (int i = 0; i < playerCount; i++)
        {
            State.players.Add(new PlayerState { index = i, name = "Player " + (i + 1) });
        }
    }

    public void StepPhase()
    {
        if (MatchOver) return;
        GameState s = State;

        Log("=== Game " + s.gameNumber + ", round " + s.round + ", phase " + s.phase
            + " (active player: " + s.players[s.activePlayer].name + ") ===");

        // Seat order starting from the active player. PLACEHOLDER decisions: a random legal move.
        for (int i = 0; i < s.players.Count; i++)
        {
            PlayerState p = s.players[(s.activePlayer + i) % s.players.Count];
            List<GameAction> legal = LegalMoves.Get(s, p);
            Log("  " + p.name + " legal actions: " + LegalMoves.Describe(legal));
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
                State.blockDeck.Discard(a.card);   // ASSUMPTION: used cards recycle (see DeckManager note)
                p.heldBlock = null;
                Log("    " + p.name + " places " + a);
                break;
        }
    }

    // PLACEHOLDER: the real shake is a physical motor. This stands in so the loop can run.
    private void DoShake()
    {
        foreach (PlayerState p in State.players) p.fellThisShake = false;
        int threshold = (int)(TableNumbers.PlaceholderCollapseChance * 1000f);
        foreach (PlayerState p in State.players)
        {
            if (p.inGame) p.fellThisShake = GameRandom.Instance.Next(1000) < threshold;
        }
        Log("  Platform shakes all towers.");
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

    private void DoEndCheck()
    {
        List<PlayerState> standing = new List<PlayerState>();
        foreach (PlayerState p in State.players) if (p.inGame) standing.Add(p);

        PlayerState winner = null;
        if (standing.Count == 1)
        {
            winner = standing[0];
        }
        else if (standing.Count == 0)
        {
            // Last towers fell together: the tallest of them wins. Ties go to seat order (PLACEHOLDER).
            foreach (PlayerState p in State.players)
            {
                if (p.fellThisShake && (winner == null || p.TowerHeight > winner.TowerHeight)) winner = p;
            }
        }

        if (winner == null)
        {
            Log("  Two or more towers still stand: the game continues.");
            return;
        }

        winner.matchPoints += TableNumbers.PointsPerGameWin;
        Log("  " + winner.name + " wins game " + State.gameNumber + " (match points: " + winner.matchPoints + ").");

        if (winner.matchPoints >= TableNumbers.PointsToWinMatch)
        {
            MatchOver = true;
            MatchWinner = winner;
            Log("  " + winner.name + " wins the match.");
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
        }
        State.gameNumber++;
        Log("  Towers rebuilt. Starting game " + State.gameNumber + ".");
    }

    // End-of-turn transition: clear the event, advance the round, pass play to the next player.
    private void EndOfTurn()
    {
        if (State.eventCard != null) State.eventDeck.Discard(State.eventCard);
        State.eventCard = null;
        State.eventDrawn = false;
        State.round++;

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
