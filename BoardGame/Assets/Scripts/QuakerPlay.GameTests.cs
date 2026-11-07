using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Full-game tests: QUAKER > Play > Run Game Tests (Play mode, Play.unity, on the setup screen).
// Plays 10 matches through the real game: the setup screen, the buttons players click (Draw Event, Draw Block,
// Ready, Start the shake, the action bar, Next game) and the same place-a-block path the mouse uses
// (Place() with an aimed action checked by the rules). Each test lines up the event cards it needs and, where
// needed, stages a situation (points, a short deck). After every step it checks the rules, the cards, the turn
// order, the scores and the screen, and writes every mismatch as a CONFLICT.
// Report: the Console and Logs/QuakerGameTests.txt.
public partial class QuakerPlay
{
    [Header("Game tests (QUAKER > Play > Run Game Tests, in Play mode)")]
    public float gameTestTimeScale = 6f;
    [HideInInspector] public bool gameTestRunning;
    [HideInInspector] public string gameTestReport = "";

    class GameTest
    {
        public string name, purpose;
        public int players = 2, seed = 1, maxRounds = 30;
        public int[] events = new int[0];        // event cards drawn first, in this order
        public int[] points;                     // staged points at the start, per player
        public int blockCardsLeft = -1;          // staged: block cards left in the deck at the start
        public int eventCardsLeft = -1;          // staged: event cards left in the deck at the start
        public bool expireTenCount;              // let the first ten-count run out
        public bool newSeedEachGame;
        public bool stableUntilEventsDone = true; // tidy tower while the listed events play, then risky to end it
        public bool alwaysRisky;
    }

    readonly List<string> gtConflicts = new List<string>();
    readonly List<string> gtNotes = new List<string>();
    readonly HashSet<string> gtCovered = new HashSet<string>();
    string gtName = "";
    string gtStableSig = "";
    int gtErrors;
    bool gtTenCountExpired;

    public void RunGameTests()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[Quaker] The game tests only run in Play mode. Press Play first."); return; }
        if (gameTestRunning) return;
        StartCoroutine(GameTests());
    }

    static List<GameTest> GameTestList()
    {
        return new List<GameTest>
        {
            new GameTest { name = "1. Shakes", purpose = "events 2, 8, 5, 10, 19, 15: no effect, two shakes, double speed", players = 2, seed = 11, events = new[] { 2, 8, 5, 10, 19, 15 } },
            new GameTest { name = "2. Card choices", purpose = "events 1 (redraw), 13 (two cards), 9 (rival swap), 17 (second block)", players = 3, seed = 12, events = new[] { 1, 13, 9, 17 } },
            new GameTest { name = "3. Placement limits", purpose = "events 6, 7, 11, 12: on top, outer space, no middle, no corners", players = 4, seed = 13, events = new[] { 6, 7, 11, 12 } },
            new GameTest { name = "4. Ready prompts", purpose = "event 3 (blind), event 14 (ten-count, first player lets it run out)", players = 2, seed = 14, events = new[] { 3, 14 }, expireTenCount = true },
            new GameTest { name = "5. Skipped rounds", purpose = "events 16 (aftershocks), 4 (coffee break), 18 (gas line scare)", players = 3, seed = 15, events = new[] { 16, 4, 18 } },
            new GameTest { name = "6. Event 20 penalty", purpose = "event 20 face up when the tower collapses; loser starts with 1 point", players = 2, seed = 16, events = new[] { 20 }, points = new[] { 1, 1 }, alwaysRisky = true, stableUntilEventsDone = false },
            new GameTest { name = "7. Shared win", purpose = "two players on 2 points; the third loses, so both reach 3 together", players = 3, seed = 17, points = new[] { 2, 2, 0 }, alwaysRisky = true, stableUntilEventsDone = false },
            new GameTest { name = "8. Block deck runs short", purpose = "only 5 block cards left with 4 players: shake-only rounds getting stronger", players = 4, seed = 18, blockCardsLeft = 5, stableUntilEventsDone = false, maxRounds = 25 },
            new GameTest { name = "9. Event deck runs out", purpose = "only 2 event cards left: the used ones are reshuffled", players = 2, seed = 19, eventCardsLeft = 2, maxRounds = 8 },
            new GameTest { name = "10. Full match", purpose = "4 players, new seed every game, normal play until someone wins", players = 4, seed = 20, newSeedEachGame = true, stableUntilEventsDone = false, maxRounds = 60 },
        };
    }

    IEnumerator GameTests()
    {
        gameTestRunning = true;
        gtConflicts.Clear(); gtNotes.Clear(); gtCovered.Clear();
        gtErrors = 0;
        Application.logMessageReceived += GtLog;
        float oldScale = Time.timeScale;
        bool oldSeedMode = newSeedEachGame;
        string[] oldNames = new string[4];
        int[] oldColors = (int[])setupColor.Clone();
        for (int i = 0; i < 4; i++) oldNames[i] = i < nameFields.Count ? nameFields[i].text : "";

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("QUAKER full-game tests  (shake " + shakeStrength + "x gravity, collapse " + Mathf.RoundToInt(collapseShare * 100) + "% / " + collapseMinBlocks + " blocks)");
        sb.AppendLine();
        foreach (GameTest t in GameTestList())
        {
            int before = gtConflicts.Count;
            Time.timeScale = gameTestTimeScale;
            string summary = "";
            yield return RunGameTest(t, r => summary = r);
            sb.AppendLine(t.name + " - " + t.purpose);
            sb.AppendLine("   " + summary);
            int n = gtConflicts.Count - before;
            sb.AppendLine("   " + (n == 0 ? "no conflicts" : n + " conflict(s):"));
            for (int i = before; i < gtConflicts.Count; i++) sb.AppendLine("     - " + gtConflicts[i]);
            sb.AppendLine();
        }

        // Coverage
        string[] wanted =
        {
            "event 1", "event 2", "event 3", "event 4", "event 5", "event 6", "event 7", "event 8", "event 9", "event 10",
            "event 11", "event 12", "event 13", "event 14", "event 15", "event 16", "event 17", "event 18", "event 19", "event 20",
            "redraw used", "other card used", "rival swapped", "second block placed", "blind placement", "ready prompt (timed)",
            "ten-count ran out", "aftershocks run", "coffee break skipped", "gas line round skipped", "double-speed shake", "two shakes",
            "collapse during shake", "collapse while placing", "blocks set aside", "event 20 point lost", "shared match win",
            "match won", "deck short shake-only round", "shake-only escalation", "event deck reshuffled", "next game started",
            "first player rotated", "new seed each game", "turn banner", "event banner", "rival banner", "coffee banner",
        };
        sb.AppendLine("COVERAGE");
        List<string> missing = new List<string>();
        foreach (string w in wanted) if (!gtCovered.Contains(w)) missing.Add(w);
        sb.AppendLine("   tested: " + (wanted.Length - missing.Count) + " of " + wanted.Length + (missing.Count > 0 ? "   not reached: " + string.Join(", ", missing.ToArray()) : ""));
        sb.AppendLine("   errors/exceptions logged during the run: " + gtErrors);
        if (gtNotes.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("NOTES (not conflicts)");
            foreach (string n in gtNotes) sb.AppendLine("   - " + n);
        }
        sb.AppendLine();
        sb.AppendLine("TOTAL CONFLICTS: " + gtConflicts.Count);

        Application.logMessageReceived -= GtLog;
        Time.timeScale = oldScale;
        newSeedEachGame = oldSeedMode;
        for (int i = 0; i < 4 && i < nameFields.Count; i++) nameFields[i].text = oldNames[i];
        for (int i = 0; i < 4; i++) setupColor[i] = oldColors[i];
        for (int i = 0; i < 4; i++) PlayerPrefs.SetString("QuakerName" + i, oldNames[i]);
        SaveColors();
        ShowSetupColors();
        ClearBlocks();
        machine = null;
        ShowSetup();

        gameTestReport = sb.ToString();
        Debug.Log(gameTestReport);
        try
        {
            string path = System.IO.Path.Combine(Application.dataPath, "..", "Logs", "QuakerGameTests.txt");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            System.IO.File.WriteAllText(path, gameTestReport);
        }
        catch (System.Exception e) { Debug.LogWarning("Game tests: could not write the report: " + e.Message); }
        gameTestRunning = false;
    }

    void GtLog(string msg, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        gtErrors++;
        Conflict("Unity " + type + ": " + msg.Split('\n')[0]);
    }

    void Conflict(string what)
    {
        string where = s != null ? " [game " + s.gameNumber + " round " + s.roundInGame + ", " + s.phase + "]" : "";
        string line = gtName + where + ": " + what;
        if (!gtConflicts.Contains(line)) gtConflicts.Add(line);
    }

    void Cover(string what) { gtCovered.Add(what); }

    // ---------------- one test match ----------------

    IEnumerator RunGameTest(GameTest t, System.Action<string> done)
    {
        gtName = t.name;
        gtTenCountExpired = false;
        System.Random rnd = new System.Random(t.seed);
        newSeedEachGame = t.newSeedEachGame;
        GameRandom.Instance.seed = t.seed * 1000 + 7;

        // Setup screen, through its own buttons
        ShowSetup();
        yield return null;
        if (setupPanel == null || !setupPanel.activeSelf) Conflict("the setup screen did not open");
        Press(countButtons[t.players - 2]);
        for (int i = 0; i < 4; i++) nameFields[i].text = "P" + (i + 1);
        Press(startButton);
        yield return null;
        if (machine == null) { Conflict("Start match did not start a match"); done("did not start"); yield break; }
        if (setupPanel.activeSelf) Conflict("the setup screen stayed open after Start match");
        if (!canvasRoot.transform.Find("Actions").gameObject.activeInHierarchy) Conflict("the action bar is hidden during the match");
        if (!canvasRoot.transform.Find("TopBar").gameObject.activeInHierarchy) Conflict("the top bar is hidden during the match");
        if (t.newSeedEachGame) Cover("new seed each game");

        // Staging
        if (t.points != null) for (int i = 0; i < t.points.Length && i < s.players.Count; i++) s.players[i].matchPoints = t.points[i];
        StageEvents(t.events);
        if (t.blockCardsLeft >= 0) StageDeck(s.blockDeck, t.blockCardsLeft);
        if (t.eventCardsLeft >= 0) StageDeck(s.eventDeck, t.eventCardsLeft);

        int rounds = 0, games = 0, placements = 0, lastRound = -1, lastGame = s.gameNumber, stuck = 0;
        string lastSig = "";
        List<int> orderAtLastEvent = null;
        int eventsSeen = 0;
        int lastShakeOnly = 0, lastShakeGame = -1;
        bool sawEventPhase = false;
        int roundsUnderwayAtLastEvent = machine.RoundsUnderway;   // rounds that got past their Event phase, as the game counts them
        int[] pointsBefore = new int[s.players.Count];
        bool lastWasShake = false;

        while (!machine.MatchOver && rounds < t.maxRounds)
        {
            yield return null;
            if (!gameOverPanel.activeSelf && !machine.MatchOver)
                for (int i = 0; i < s.players.Count; i++) pointsBefore[i] = s.players[i].matchPoints;
            if (busy || shaking) continue;
            if (machine.MatchOver) break;

            // Game over box: check the score, then Next game
            if (gameOverPanel.activeSelf)
            {
                games++;
                Cover("next game started");
                Cover(lastWasShake ? "collapse during shake" : "collapse while placing");
                CheckScores(pointsBefore);
                if (!lastCollapse.Contains("come down") && !lastCollapse.Contains("No block")) Conflict("game ended without a collapse reason: '" + lastCollapse + "'");
                Press(nextButton);
                if (t.eventCardsLeft >= 0) StageDeck(s.eventDeck, t.eventCardsLeft);
                yield return null;
                if (placed.Count != 0 || setAside.Count != 0) Conflict("Next game left blocks on the table (" + placed.Count + " on the tower, " + setAside.Count + " set aside)");
                foreach (CardData style in blockStyles) if (RuleCheck.SupplyLeft(s, style) != style.copies) Conflict("the supply was not refilled for the new game (" + style.title + ": " + RuleCheck.SupplyLeft(s, style) + ")");
                continue;
            }

            int who = Actor();
            Safely("checking the cards", CheckCards);
            string sigNow = s.gameNumber + "/" + s.roundInGame + "/" + s.phase + "/" + who + "/" + s.buildTurn + "/" + (s.eventCard != null) + "/" + readyConfirmed;
            if (sigNow == gtStableSig) Safely("checking the screen", () => CheckScreen(who));
            gtStableSig = sigNow;

            // New round: the first player must have moved exactly one seat clockwise since the last Event phase,
            // unless the tower fell from the opening placement before any round got going (then nobody moves).
            if (s.phase == TurnPhase.Event && (s.roundInGame != lastRound || s.gameNumber != lastGame))
            {
                bool roundUnderway = machine.RoundsUnderway != roundsUnderwayAtLastEvent;
                roundsUnderwayAtLastEvent = machine.RoundsUnderway;
                if (orderAtLastEvent != null && sawEventPhase)
                {
                    List<int> expected = new List<int>(orderAtLastEvent);
                    if (roundUnderway) { expected.Add(expected[0]); expected.RemoveAt(0); }
                    if (string.Join(",", expected) != string.Join(",", s.turnOrder))
                        Conflict(roundUnderway
                            ? "first player did not move one seat clockwise: was " + string.Join(",", orderAtLastEvent) + ", now " + string.Join(",", s.turnOrder)
                            : "the tower fell from the opening placement but the first player moved: was " + string.Join(",", orderAtLastEvent) + ", now " + string.Join(",", s.turnOrder));
                    else Cover(roundUnderway ? "first player rotated" : "opening collapse keeps first player");
                }
                orderAtLastEvent = new List<int>(s.turnOrder);
                sawEventPhase = true;
                lastRound = s.roundInGame;
                lastGame = s.gameNumber;
                rounds++;
            }

            // Stuck?
            string sig = s.gameNumber + "/" + s.roundInGame + "/" + s.phase + "/" + who + "/" + s.buildTurn + "/" + placements + "/" + (s.eventCard != null) + "/" + s.blockDeck.DrawPileCount;
            if (sig == lastSig) { if (++stuck > 400) { Conflict("the game is stuck: " + PromptText()); break; } }
            else { stuck = 0; lastSig = sig; }

            bool stable = t.stableUntilEventsDone && eventsSeen < t.events.Length && !t.alwaysRisky;
            Bot bot = t.alwaysRisky ? Bot.Risky : stable ? Bot.Showcase : (rnd.Next(3) == 0 ? Bot.Risky : Bot.Builder);

            switch (s.phase)
            {
                case TurnPhase.Opening:
                {
                    List<CardData> left = new List<CardData>();
                    foreach (CardData c in blockStyles) if (RuleCheck.SupplyLeft(s, c) > 0) left.Add(c);
                    openingStyle = left[rnd.Next(left.Count)];
                    lastWasShake = false;
                    if (SafePlace(openingStyle, bot, rnd)) placements++;
                    break;
                }
                case TurnPhase.Event:
                    if (!drawEventButton.interactable) { Conflict("Draw Event is disabled in the Event phase"); break; }
                    int pileBefore = s.eventDeck.DrawPileCount;
                    Press(drawEventButton);
                    if (pileBefore == 0 && s.eventCard != null) Cover("event deck reshuffled");
                    if (pileBefore == 0 && s.eventCard == null) Conflict("the event deck was empty and was not reshuffled");
                    if (s.eventCard != null)
                    {
                        int ev = RuleCheck.EventNumber(s);
                        Cover("event " + ev);
                        eventsSeen++;
                        yield return null;
                        if (!BannerHas(s.eventCard.title)) Conflict("no banner for event " + ev + " (" + s.eventCard.title + ")");
                        else Cover("event banner");
                        if (ev == 18)
                        {
                            int r0 = s.roundInGame, g0 = s.gameNumber, tower0 = s.tower.Count, waited = 0;
                            while (s.roundInGame == r0 && s.gameNumber == g0 && !machine.MatchOver && waited++ < 900)
                            {
                                if (shaking) Conflict("Gas Line Scare: the board shook");
                                yield return null;
                            }
                            if (s.tower.Count != tower0) Conflict("Gas Line Scare: blocks were placed in the skipped round");
                            else if (waited >= 900) Conflict("Gas Line Scare: the round never ended");
                            else Cover("gas line round skipped");
                        }
                    }
                    break;

                case TurnPhase.Draw:
                    if (s.deckShort) break;   // Settle moves on by itself
                    if (!drawBlockButton.interactable) { Conflict("Draw Block is disabled for " + s.players[who].name + " in the Draw phase"); break; }
                    Press(drawBlockButton);
                    break;

                case TurnPhase.Place:
                {
                    if (RuleCheck.RivalPending(s))
                    {
                        int target = -1;
                        foreach (int i in s.turnOrder) if (i != s.activePlayer) { target = i; break; }
                        CardData beforeCard = target >= 0 ? s.players[target].heldBlock : null;
                        if (!ClickAction("Rival: " + s.players[target].name)) { Conflict("no Rival button for " + s.players[target].name); break; }
                        yield return null;
                        Cover("rival swapped");
                        if (s.players[target].heldBlock == beforeCard && s.blockDeck.TotalAvailable > 0) gtNotes.Add(gtName + ": the rival happened to draw the same block style again");
                        if (!BannerHas("card was swapped")) Conflict("no banner telling " + s.players[target].name + " their card was swapped");
                        else Cover("rival banner");
                        break;
                    }
                    if (RuleCheck.AftershocksPending(s))
                    {
                        if (!ClickAction("Run the aftershocks")) { Conflict("no Run the aftershocks button"); break; }
                        lastWasShake = true;
                        Cover("aftershocks run");
                        break;
                    }
                    if (s.buildTurn < 0) break;   // nobody's turn yet: the round is moving on
                    PlayerState p = s.players[s.buildTurn];
                    if (p.heldBlock == null)
                    {
                        // Structural Audit second draw
                        if (!drawBlockButton.interactable) { Conflict("Structural Audit: Draw Block is disabled for the second card"); break; }
                        if (!BannerHas("second block")) { yield return null; if (!BannerHas("second block")) Conflict("no Structural Audit banner for the second draw"); }
                        Press(drawBlockButton);
                        break;
                    }
                    int ev = RuleCheck.EventNumber(s);
                    if (NeedsReady() && readyConfirmed != ReadyKey())
                    {
                        if (!readyPanel.activeSelf) { Conflict("no Ready box for event " + ev); }
                        bool timed = RuleCheck.TenCount(s);
                        Press(readyButton);
                        if (timed) Cover("ready prompt (timed)");
                        if (timed && t.expireTenCount && !gtTenCountExpired)
                        {
                            gtTenCountExpired = true;
                            CardData held = p.heldBlock;
                            int who0 = s.buildTurn;
                            float t0 = Time.time;
                            yield return null;
                            if (tenCountEndsAt < 0f) Conflict("the ten-count did not start after Ready");
                            while (Time.time - t0 < 11f && !s.players[who0].skippedThisRound) yield return null;
                            if (!s.players[who0].skippedThisRound) Conflict("the ten-count ran out but the player was not skipped");
                            else Cover("ten-count ran out");
                            break;
                        }
                        yield return null;
                    }
                    if (ev == 1 && !p.redrewThisRound && rnd.Next(2) == 0 && s.blockDeck.TotalAvailable > 0)
                    {
                        if (!ClickAction("Redraw")) Conflict("Site Inspector: no Redraw button");
                        else Cover("redraw used");
                        yield return null;
                        break;
                    }
                    bool mustSwitch = p.extraOption != null && RuleCheck.SupplyLeft(s, p.heldBlock) <= 0 && RuleCheck.SupplyLeft(s, p.extraOption) > 0;
                    bool canSwitch = LegalMoves.IsLegal(s, new GameAction { type = ActionType.SwapOption, player = s.buildTurn });
                    if (canSwitch && (mustSwitch || rnd.Next(2) == 0))
                    {
                        CardData other = p.extraOption;
                        if (!ClickAction("Use ")) Conflict("Permit Renewed: no 'Use ... instead' button");
                        else if (s.players[s.buildTurn].heldBlock != other) Conflict("Permit Renewed: 'Use ... instead' did not switch the card");
                        else Cover("other card used");
                        yield return null;
                    }
                    bool second = p.placementsThisRound > 0;
                    bool blind = RuleCheck.Blind(s);
                    lastWasShake = false;
                    if (SafePlace(p.heldBlock, bot, rnd))
                    {
                        placements++;
                        if (second) Cover("second block placed");
                        if (blind) Cover("blind placement");
                        CheckLimit(ev);
                        // let it settle, then the tower must be visible again after a blind placement
                        while (busy) yield return null;
                        yield return null;
                        if (blind)
                            foreach (Rigidbody rb in placed)
                                if (rb != null) foreach (Renderer r in rb.GetComponentsInChildren<Renderer>()) if (!r.enabled) { Conflict("blocks stayed invisible after the blind placement"); break; }
                    }
                    break;
                }
                case TurnPhase.Shake:
                {
                    int count = RuleCheck.ShakeCount(s);
                    float speed = RuleCheck.ShakeSpeed(s);
                    int ev = RuleCheck.EventNumber(s);
                    bool shortRound = s.deckShort;
                    if (ev == 4) { if (BannerHas("Nobody places")) Cover("coffee banner"); else Conflict("no Coffee Break banner"); Cover("coffee break skipped"); }
                    if (!ClickAction("Start the shake")) { Conflict("no Start the shake button"); break; }
                    lastWasShake = true;
                    if (count > 1) Cover("two shakes");
                    if (speed > 1f) Cover("double-speed shake");
                    if (shortRound)
                    {
                        Cover("deck short shake-only round");
                        if (s.gameNumber != lastShakeGame) { lastShakeOnly = 0; lastShakeGame = s.gameNumber; }
                        if (shakeOnlyRounds != lastShakeOnly + 1) Conflict("shake-only round number jumped from " + lastShakeOnly + " to " + shakeOnlyRounds);
                        if (shakeOnlyRounds > 1 && shakeExtra > 0f) Cover("shake-only escalation");
                        if (Mathf.Abs(shakeExtra - (ShakeOnlyStrength(shakeOnlyRounds) - shakeStrength)) > 0.001f) Conflict("shake-only round " + shakeOnlyRounds + " is not stronger by the right amount");
                        lastShakeOnly = shakeOnlyRounds;
                    }
                    break;
                }
                case TurnPhase.CollapseCheck:
                    if (RuleCheck.EventNumber(s) == 18) Cover("gas line round skipped");
                    break;
            }
        }

        // Match over checks
        string result;
        if (machine.MatchOver)
        {
            Cover("match won");
            Cover(lastWasShake ? "collapse during shake" : "collapse while placing");
            CheckScores(pointsBefore);
            yield return null;
            if (!overPanel.activeSelf) Conflict("the match-over screen did not appear");
            List<string> winners = new List<string>();
            foreach (PlayerState p in s.players) if (p.matchPoints >= 3) winners.Add(p.name);
            if (winners.Count == 0) Conflict("the match ended but nobody has 3 points");
            if (winners.Count > 1) Cover("shared match win");
            result = "match won by " + string.Join(" and ", winners.ToArray()) + " after " + (games + 1) + " game(s), " + rounds + " round(s)";
        }
        else result = "stopped after " + rounds + " round(s) (limit " + t.maxRounds + "), " + (games + 1) + " game(s) played";
        string pts = "";
        foreach (int i in s.turnOrder) pts += s.players[i].name + " " + s.players[i].matchPoints + "  ";
        done(result + "; " + placements + " blocks placed; points: " + pts.Trim());
    }

    // Puts the listed event cards on top of the event deck, in order.
    void StageEvents(int[] ids)
    {
        if (ids == null || ids.Length == 0) return;
        List<CardData> pile = (List<CardData>)typeof(DeckManager).GetField("drawPile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(s.eventDeck);
        for (int k = ids.Length - 1; k >= 0; k--)
        {
            CardData c = pile.Find(x => x.id == ids[k].ToString());
            if (c == null) { Conflict("could not stage event " + ids[k]); continue; }
            pile.Remove(c); pile.Insert(0, c);
        }
    }

    // Moves cards from the draw pile to the discard pile until `left` remain.
    void StageDeck(DeckManager deck, int left)
    {
        var f = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        List<CardData> pile = (List<CardData>)typeof(DeckManager).GetField("drawPile", f).GetValue(deck);
        List<CardData> discard = (List<CardData>)typeof(DeckManager).GetField("discardPile", f).GetValue(deck);
        while (pile.Count > left) { discard.Add(pile[pile.Count - 1]); pile.RemoveAt(pile.Count - 1); }
    }

    // ---------------- placing like a player does ----------------

    // Finds every physical spot for the block (the aiming rules), keeps the ones the rules accept, picks one by
    // the bot's habit, then places it through Place() exactly as a mouse click would.
    bool TestPlace(CardData style, Bot bot, System.Random rnd)
    {
        if (style == null) return false;
        SetGhost(style);
        if (ghost == null) { Conflict("no aiming preview for " + style.title); return false; }
        List<Candidate> options = new List<Candidate>();
        List<GameAction> actions = new List<GameAction>();
        int tried = 0, turnsMeasured = 0;
        Dictionary<string, int> refused = new Dictionary<string, int>();
        int who = s.phase == TurnPhase.Opening ? s.buildTurn : s.buildTurn;
        foreach (Quaternion turn in TestTurns())
        {
            ghost.transform.rotation = turn;
            Bounds gb = RenderBounds(ghost);
            Vector3 pivotOffset = ghost.transform.position - gb.center;
            int wx = Mathf.Max(1, Mathf.RoundToInt(gb.size.x / cell)), wz = Mathf.Max(1, Mathf.RoundToInt(gb.size.z / cell));
            if (wx > 4 || wz > 4) continue;
            turnsMeasured++;
            List<Bounds> cubes = new List<Bounds>();
            foreach (Renderer r in ghost.GetComponentsInChildren<Renderer>())
            {
                Bounds b = r.bounds;
                b.center = new Vector3(b.center.x - gb.center.x, b.center.y - gb.min.y, b.center.z - gb.center.z);
                cubes.Add(b);
            }
            for (int sx = 0; sx + wx <= 4; sx++)
                for (int sz = 0; sz + wz <= 4; sz++)
                {
                    Vector3 c = new Vector3(gridMin.x + (sx + wx * 0.5f) * cell, 0f, gridMin.z + (sz + wz * 0.5f) * cell);
                    foreach (Level lv in RestingLevels(gb, c))
                    {
                        HashSet<int> covers = new HashSet<int>();
                        int off = 0;
                        foreach (Bounds cube in cubes)
                        {
                            int id = NearestTile(new Vector3(c.x + cube.center.x, 0f, c.z + cube.center.z), cell * 0.6f);
                            if (id > 0) covers.Add(id); else off++;
                        }
                        int space = -1;
                        foreach (int id in covers) { space = id; break; }
                        SpaceData sd = space > 0 ? towerBase.Get(space) : null;
                        GameAction a = new GameAction
                        {
                            type = ActionType.PlaceBlock, player = who, card = style, space = space,
                            spaceName = sd != null ? sd.displayName : "off the base", angle = Mathf.RoundToInt(turn.eulerAngles.y),
                            physical = true, onBlock = lv.onBlock, covers = new List<int>(covers).ToArray(), offBase = off > 0
                        };
                        tried++;
                        RuleResult vr = LegalMoves.Validate(s, a);
                        if (!vr.legal)
                        {
                            string why = vr.ruleId + " " + vr.reason;
                            int seen; refused.TryGetValue(why, out seen); refused[why] = seen + 1;
                            continue;
                        }
                        Candidate o = new Candidate
                        {
                            turn = turn, position = new Vector3(c.x, lv.bottom + gb.extents.y + 0.15f, c.z) + pivotOffset,
                            bottom = lv.bottom, top = lv.bottom + gb.size.y, height = gb.size.y, onBlock = lv.onBlock,
                            centreDistance = new Vector2(c.x - baseCentre.x, c.z - baseCentre.z).magnitude
                        };
                        Support(cubes, c, lv.bottom, out o.unsupported, out o.balanced);
                        options.Add(o);
                        actions.Add(a);
                    }
                }
        }
        if (options.Count == 0)
        {
            string reasons = "";
            foreach (var kv in refused) reasons += " [" + kv.Value + "x " + kv.Key + "]";
            PlayerState sp = s.players[who];
            gtNotes.Add(gtName + " DIAGNOSIS: " + sp.name + " placing " + style.title + "; holds " + (sp.heldBlock != null ? sp.heldBlock.title : "none")
                + (sp.extraOption != null ? " (option " + sp.extraOption.title + ")" : "") + "; phase " + s.phase + ", buildTurn " + s.buildTurn + ", who " + who
                + ", busy " + busy + ", ghost renderers " + ghost.GetComponentsInChildren<Renderer>().Length + ", ghost style " + (ghostStyle != null ? ghostStyle.title : "none")
                + "; turns measured " + turnsMeasured + ", spots tried " + tried + "; refused:" + reasons);
            Conflict(s.players[who].name + " has no physical spot for " + style.title + (RuleCheck.EventNumber(s) > 0 ? " under event " + RuleCheck.EventNumber(s) : "")
                + ", but the game expects a placement and has no skip button");
            if (s.phase == TurnPhase.Place) { machine.MissedTenCount(); AfterMove(); }   // skips the placement and moves the round on
            else if (s.phase == TurnPhase.Opening) { s.players[who].placedOpening = true; machine.TowerFell(); AfterMove(); }
            return false;
        }
        int pick;
        if (bot == Bot.Showcase)
        {
            Candidate best = ShowcasePick(options, rnd);
            pick = options.FindIndex(o => o.position == best.position && o.turn == best.turn);
        }
        else
        {
            // Like a person: only spots that look safe (every part resting on something, weight over what holds it
            // up). If nothing looks fully safe, the least risky spots. Then the player's habit decides among them.
            int fewest = int.MaxValue;
            foreach (Candidate o in options) fewest = Mathf.Min(fewest, o.unsupported + (o.balanced ? 0 : 1));
            pick = 0; float bestScore = float.MaxValue;
            for (int i = 0; i < options.Count; i++)
            {
                Candidate o = options[i];
                if (o.unsupported + (o.balanced ? 0 : 1) > fewest) continue;
                float noise = (float)rnd.NextDouble();
                float score = bot == Bot.Risky
                    ? -o.top + noise * 3f                                                   // as tall as it safely goes
                    : bot == Bot.Careful
                        ? o.top + o.centreDistance * 0.1f + noise * 2f                      // low and wide
                        : (o.onBlock ? 0f : 8f) + o.centreDistance * 1.2f - o.top * 0.3f + noise * 5f;   // build up in the middle
                if (score < bestScore) { bestScore = score; pick = i; }
            }
        }

        // Place exactly like a click: the preview at the spot, the aimed action, Place().
        ghostTurn = options[pick].turn;
        ghost.transform.rotation = options[pick].turn;
        ghost.transform.position = options[pick].position;
        aimed = actions[pick];
        int placedBefore = placed.Count;
        string noticeBefore = notice;
        Place();
        if (placed.Count == placedBefore)
        {
            Conflict("the rules accepted a spot but placing it was refused: " + notice);
            return false;
        }
        return true;
    }

    // ---------------- checks ----------------

    // Event limits must hold for the block just placed (checked from the action the rules saw).
    void CheckLimit(int ev)
    {
        if (s.tower.Count == 0) return;
        GameAction a = s.tower[s.tower.Count - 1];
        if (a.covers == null) return;
        foreach (int id in a.covers)
        {
            SpaceData sd = towerBase.Get(id);
            if (sd == null) continue;
            if (ev == 11 && RuleCheck.IsCenterHub(sd)) Conflict("event 11: a block covers a middle space");
            if (ev == 12 && RuleCheck.IsCorner(sd)) Conflict("event 12: a block covers a corner space");
        }
        if (ev == 6 && !a.onBlock) Conflict("event 6: a block was placed on the base, not on another block");
        if (ev == 7)
        {
            bool edge = false;
            foreach (int id in a.covers) if (RuleCheck.IsGridEdge(towerBase.Get(id))) edge = true;
            if (!edge) Conflict("event 7: a block doesn't cover an outer space");
        }
    }

    // When a game ends: exactly one loser (no point, or -1 with event 20), every other player +1.
    void CheckScores(int[] before)
    {
        int losers = 0;
        string d = "";
        for (int i = 0; i < s.players.Count; i++)
        {
            int delta = s.players[i].matchPoints - before[i];
            d += s.players[i].name + " " + (delta >= 0 ? "+" : "") + delta + " ";
            if (delta == 1) continue;
            if (delta == 0 || delta == -1) { losers++; if (delta == -1) Cover("event 20 point lost"); continue; }
            Conflict("odd score change: " + d);
        }
        if (losers != 1) Conflict("a game must have exactly one loser, but the points changed: " + d.Trim());
    }

    // Cards are never lost or duplicated.
    void CheckCards()
    {
        int held = 0;
        foreach (PlayerState p in s.players) { if (p.heldBlock != null) held++; if (p.extraOption != null) held++; }
        int blockTotal = s.blockDeck.DrawPileCount + s.blockDeck.DiscardPileCount + held;
        if (blockTotal != 30) Conflict("block cards don't add up to 30 (draw " + s.blockDeck.DrawPileCount + " + discard " + s.blockDeck.DiscardPileCount + " + in hand " + held + " = " + blockTotal + ")");
        int eventTotal = s.eventDeck.DrawPileCount + s.eventDeck.DiscardPileCount + (s.eventCard != null ? 1 : 0);
        if (eventTotal != 20) Conflict("event cards don't add up to 20 (" + eventTotal + ")");
        foreach (CardData style in blockStyles)
            if (RuleCheck.SupplyLeft(s, style) < 0) Conflict("more " + style.title + "s placed than exist");
        if (setAside.Count > 0) Cover("blocks set aside");
    }

    // What the players see matches the game.
    void CheckScreen(int who)
    {
        if (who < 0) return;
        string name = s.players[who].name;
        if (!PromptText().Contains(name)) Conflict("the prompt doesn't name " + name + ": '" + PromptText() + "'");
        if (turnFrame != null && (!turnFrame.activeSelf || turnFrame.GetComponentInChildren<Image>().color.r != PlayerColor(who).r))
            Conflict("the screen frame isn't in " + name + "'s colour");
        if (bannerKey.StartsWith(who + ":")) Cover("turn banner");
        bool evLegal = LegalMoves.IsLegal(s, new GameAction { type = ActionType.DrawEventCard, player = who });
        if (drawEventButton.interactable != evLegal) Conflict("Draw Event is " + (drawEventButton.interactable ? "enabled" : "disabled") + " but drawing is " + (evLegal ? "legal" : "not legal"));
        bool blLegal = LegalMoves.IsLegal(s, new GameAction { type = ActionType.DrawBlockCard, player = who });
        if (drawBlockButton.interactable != blLegal) Conflict("Draw Block is " + (drawBlockButton.interactable ? "enabled" : "disabled") + " but drawing is " + (blLegal ? "legal" : "not legal"));
        bool ready = NeedsReady() && readyConfirmed != ReadyKey();
        if (readyPanel != null && readyPanel.activeSelf != ready) Conflict("the Ready box is " + (readyPanel.activeSelf ? "showing" : "hidden") + " when it should be " + (ready ? "showing" : "hidden"));
    }

    string PromptText() { return prompt != null ? prompt.text : ""; }

    bool BannerHas(string text)
    {
        foreach (BannerMsg m in bannerQueue) if ((m.title + " " + m.sub).Contains(text)) return true;
        return false;
    }

    // Presses a button like a player would. An error is recorded as a conflict and the test carries on.
    bool Press(Button b)
    {
        if (b == null) { Conflict("a button the test needed is missing"); return false; }
        try { b.onClick.Invoke(); return true; }
        catch (System.Exception e)
        {
            string label = b.GetComponentInChildren<TMP_Text>() != null ? b.GetComponentInChildren<TMP_Text>().text.Split((char)10)[0] : b.name;
            Conflict("error when pressing '" + label + "': " + e.GetType().Name + ": " + e.Message + " at " + FirstGameFrame(e));
            return false;
        }
    }

    bool SafePlace(CardData style, Bot bot, System.Random rnd)
    {
        try { return TestPlace(style, bot, rnd); }
        catch (System.Exception e) { Conflict("error while placing a block: " + e.GetType().Name + ": " + e.Message + " at " + FirstGameFrame(e)); return false; }
    }

    void Safely(string what, System.Action a)
    {
        try { a(); }
        catch (System.Exception e) { Conflict("error while " + what + ": " + e.GetType().Name + ": " + e.Message + " at " + FirstGameFrame(e)); }
    }

    // The first line of the stack trace that is in the game's own code.
    static string FirstGameFrame(System.Exception e)
    {
        foreach (string line in (e.StackTrace ?? "").Split((char)10))
            if (line.Contains("Assets/Scripts") || line.Contains("Assets" + (char)92 + "Scripts")) return line.Trim();
        return "?";
    }

    // Clicks the action-bar button whose label starts with `label`.
    bool ClickAction(string label)
    {
        if (actionArea == null) return false;
        foreach (Button b in actionArea.GetComponentsInChildren<Button>())
        {
            TMP_Text tx = b.GetComponentInChildren<TMP_Text>();
            if (tx != null && tx.text.StartsWith(label) && b.interactable) return Press(b);
        }
        return false;
    }
}
