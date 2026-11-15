using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Build 9: the setup procedure from the finished box, as code. StartMatch runs the same numbered steps, in the
// same order, as "Setting up a game" in the rulebook. One method per step, named after it.
// Docs/Build9_SetupProcedure.md has the same list, so the two can be read side by side.
//
//   0. Choose the scenario            (digital only, before setup: Standard is the rulebook, or a Build 5 scenario)
//   1. Tower base: flat in the middle of the table
//   2. Supply: sort the 30 blocks into 6 piles by style
//   3. Block deck: shuffle, face down
//   4. Event deck: shuffle, face down
//   5. Scores: everyone starts at 0 and keeps count
//   6. Player aid: the last page of the rulebook, open for everyone
//   7. First player: the tallest player
//   8. Opening placement               (the first action of the game)
public partial class QuakerPlay
{
    [Header("Build 9: scenarios to choose from at setup (Prefabs/Scenarios, from Build 5)")]
    public List<ScenarioConfig> scenarios = new List<ScenarioConfig>();

    ScenarioConfig chosenScenario;    // null = Standard (Rulesheet v2 as printed)
    readonly List<Button> scenarioButtons = new List<Button>();
    TMP_Text scenarioInfo;
    string spaceColorsKey = "";

    const int SetupSteps = 8;
    static readonly Color GoldSpace = new Color(1f, 0.78f, 0.2f);
    static readonly Color ClosedSpace = new Color(0.12f, 0.12f, 0.14f);

    void StartMatch(int playerCount)
    {
        Step0_ChooseTheScenario(playerCount);
        Step1_PutTheTowerBaseInTheMiddle();
        Step2_SortTheBlocksIntoTheSupply();
        DeckManager blockDeck = Step3_ShuffleTheBlockDeck();
        DeckManager eventDeck = Step4_ShuffleTheEventDeck();
        Step5_EveryoneStartsAtZeroPoints();
        Step6_LeaveThePlayerAidOpen();
        Step7_TheTallestPlayerIsTheFirstPlayer(playerCount, blockDeck, eventDeck);
        Step8_OpeningPlacement();
    }

    void SetupStep(int n, string what)
    {
        Add("Setup step " + n + " of " + SetupSteps + ": " + what);
    }

    // 0. Before setup, digital only: choose the scenario. Standard is the rulebook as printed; a scenario changes only
    //    what it lists. Not a rulebook step, so it is not counted in "Setup step N of 8".
    void Step0_ChooseTheScenario(int playerCount)
    {
        log.Clear();
        notice = "";
        ScenarioConfig sc = chosenScenario;

        rules = ScriptableObject.CreateInstance<ScenarioConfig>();
        rules.displayName = sc != null ? sc.displayName : "Standard";
        rules.experienceGoal = sc != null ? sc.experienceGoal : "";
        rules.playerCount = playerCount;
        rules.board = towerBase;
        rules.refillWhenEmpty = false;                 // section 7: the block discard pile is never reshuffled
        rules.pointsPerGameWin = B.pointsPerGameWin;   // every non-loser scores this
        rules.pointsToWinMatch = B.pointsToWinMatch;   // first to this many points
        if (sc != null)
        {
            // Only the parts of a Build 5 scenario that mean something on one shared tower (see sharedTowerRule).
            rules.sharedTowerRule = sc.sharedTowerRule;
            rules.blockedSpaceIds = new List<int>(sc.blockedSpaceIds);            // The Sliver
            foreach (FirstPlacement fp in sc.firstPlacementSpaces)               // Four Corners
                if (fp.playerIndex < playerCount) rules.firstPlacementSpaces.Add(new FirstPlacement { playerIndex = fp.playerIndex, spaceId = fp.spaceId });
            rules.roundLimit = sc.roundLimit;                                     // Fuse Lit
            rules.blocksPerStyle = sc.blocksPerStyle;                             // Loose Change
            rules.collapseShare = sc.collapseShare;                               // Thin Margins
        }
        Add("Before setup, scenario: " + rules.displayName + (sc != null && sc.sharedTowerRule.Length > 0 ? ". " + sc.sharedTowerRule : " (the rulebook as printed)."));
    }

    // 1. Tower base: put it flat in the middle of the table, grid side up. Clear anything left from the last match.
    //    The build stands it on a shake board so it can be shaken; at the table the first player shakes it by hand.
    void Step1_PutTheTowerBaseInTheMiddle()
    {
        lastDrawer = -1;   // the last drawer of the previous match may not exist in this one
        revealPlayer = -1;
        shakeOnlyGame = -1; shakeOnlyRounds = 0;   // a new match's game 1 starts its shake-only rounds from 1
        StopDemo();
        SetHud(true);
        ClearBlocks();     // the shake board goes back to its place, empty
        spaceColorsKey = "";
        int closed = rules.blockedSpaceIds.Count;
        RefreshSpaceColors();
        SetupStep(1, "Tower base: flat in the middle of the table, 16 spaces, empty." + (closed > 0 ? " " + closed + " spaces are closed (grey) in " + rules.displayName + "." : ""));
    }

    // 2. Supply: sort the blocks into piles by style: 6 styles, 5 of each (or the scenario's count).
    void Step2_SortTheBlocksIntoTheSupply()
    {
        int total = 0;
        foreach (CardData c in blockStyles) if (c != null) total += rules.blocksPerStyle > 0 ? rules.blocksPerStyle : c.copies;
        SetupStep(2, "Supply: " + total + " blocks sorted into " + blockStyles.Count + " piles by style.");
    }

    // 3. Block deck: shuffle the block cards and put them face down beside the supply.
    DeckManager Step3_ShuffleTheBlockDeck()
    {
        foreach (CardData c in blockStyles)
            if (c != null) rules.blockDeck.Add(new DeckEntry { card = c, copies = rules.blocksPerStyle > 0 ? rules.blocksPerStyle : c.copies });   // 5 per style, on each card
        DeckManager blockDeck = NewDeck("BlockDeck", false);
        SeedGame(1);                                  // before the first shuffle (QuakerPlay.Seed.cs)
        blockDeck.Configure(rules.blockDeck);
        SetupStep(3, "Block deck: " + blockDeck.TotalAvailable + " block cards, face down. Nobody is dealt a card; everyone draws in the Draw phase.");
        return blockDeck;
    }

    // 4. Event deck: shuffle the event cards and put them face down beside the block deck.
    DeckManager Step4_ShuffleTheEventDeck()
    {
        foreach (CardData c in eventCards) if (c != null) rules.eventDeck.Add(new DeckEntry { card = c, copies = B.eventCardCopies });
        DeckManager eventDeck = NewDeck("EventDeck", true);   // section 7: an empty event deck is reshuffled
        eventDeck.Configure(rules.eventDeck);
        SetupStep(4, "Event deck: " + eventDeck.TotalAvailable + " event cards, face down.");
        return eventDeck;
    }

    // 5. Scores: there are no score tokens; each player keeps count of their own points, starting at 0.
    //    The build keeps the count for them (the Turn order panel).
    void Step5_EveryoneStartsAtZeroPoints()
    {
        SetupStep(5, "Scores: everyone starts at 0 and keeps count. First to " + B.pointsToWinMatch + " points wins the match.");
    }

    // 6. Player aid: the last page of the rulebook, left open where everyone can see it.
    //    The build has the rules in Help (H) and tells each player their step at the top of the screen.
    void Step6_LeaveThePlayerAidOpen()
    {
        SetupStep(6, "Player aid: the rules are in Help (H); the prompt at the top says whose step it is.");
    }

    // 7. First player: sit around the table; in the first game the tallest player is the first player.
    //    At the table this is judged by eye; here the names are entered tallest first on the setup screen.
    void Step7_TheTallestPlayerIsTheFirstPlayer(int playerCount, DeckManager blockDeck, DeckManager eventDeck)
    {
        machine = new TurnStateMachine(blockDeck, eventDeck, rules, playerCount);
        machine.Log += Add;
        machine.BeforeNewGameShuffle += SeedGame;
        s = machine.State;
        s.useRulesheet = true;
        s.ownBases = false;
        s.board = towerBase;
        s.turnOrder.Clear();
        for (int i = 0; i < playerCount; i++)
        {
            s.turnOrder.Add(i);
            string n = PlayerName(i);
            for (int j = 0; j < i; j++) if (s.players[j].name == n) n += " " + (i + 1);   // keep names apart
            s.players[i].name = n;
            PlayerPrefs.SetString("QuakerName" + i, i < nameFields.Count ? nameFields[i].text : "");
        }
        PlayerPrefs.Save();
        FixPlayerColors(playerCount);
        SetupStep(7, "First player, seated tallest first: " + string.Join(", ", s.players.ConvertAll(p => p.name)) + ". " + s.players[0].name + " is the first player.");
    }

    // 8. The first action: starting with the first player, everyone places one block of their choice on the base.
    void Step8_OpeningPlacement()
    {
        machine.BeginOpening();
        openingStyle = FirstStyleInSupply();
        setupPanel.SetActive(false);
        SetupStep(8, s.players[s.buildTurn >= 0 ? s.buildTurn : 0].name + " starts the opening placement: each player places any 1 block, no card needed. Setup is done.");
        Add("New match: " + rules.displayName + ", " + s.players.Count + " players. First to " + B.pointsToWinMatch + " points wins.");
        Refresh();
    }

    // ---------------- Tower base colours: closed spaces (The Sliver) and the opening space (Four Corners) ----------------

    void RefreshSpaceColors()
    {
        if (towerBaseModel == null) return;
        bool inMatch = machine != null && rules != null && setupPanel != null && !setupPanel.activeSelf;
        int gold = inMatch && s.phase == TurnPhase.Opening && s.buildTurn >= 0 ? RuleCheck.OpeningSpace(s, s.buildTurn) : 0;
        string key = (inMatch ? rules.displayName : "-") + ":" + gold;
        if (key == spaceColorsKey) return;
        spaceColorsKey = key;
        foreach (BoardTile t in towerBaseModel.GetComponentsInChildren<BoardTile>(true))
        {
            bool closed = inMatch && !rules.IsOpen(t.spaceId);
            if (closed) t.SetBlocked(true, ClosedSpace);
            else if (t.spaceId == gold) t.SetBlocked(true, GoldSpace);
            else t.SetBlocked(false, Color.white);
        }
    }

    // ---------------- Setup screen: the scenario picker ----------------

    // Added at Play under the player rows if the saved setup screen doesn't have it yet.
    void EnsureScenarioPicker()
    {
        if (setupPanel == null || menuScene) return;
        Transform old = setupPanel.transform.Find("ScenarioPicker");
        if (old != null) Destroy(old.gameObject);
        scenarioButtons.Clear();

        GameObject row = new GameObject("ScenarioPicker", typeof(RectTransform));
        row.transform.SetParent(setupPanel.transform, false);
        B6Place((RectTransform)row.transform, new Vector2(0.5f, 0.1f), new Vector2(0, 150), new Vector2(1500, 140));

        List<ScenarioConfig> options = new List<ScenarioConfig> { null };
        foreach (ScenarioConfig sc in scenarios) if (sc != null) options.Add(sc);
        float w = 205f, gap = 12f;
        float x0 = -(options.Count - 1) * (w + gap) / 2f;
        for (int i = 0; i < options.Count; i++)
        {
            ScenarioConfig sc = options[i];
            Button b = B6Button(row.transform, "Scenario " + i, sc == null ? "Standard" : sc.displayName,
                new Vector2(0.5f, 1f), new Vector2(x0 + i * (w + gap), -28), new Vector2(w, 52));
            TMP_Text label = b.GetComponentInChildren<TMP_Text>();
            if (label != null) { label.fontSize = 22; label.enableAutoSizing = true; label.fontSizeMin = 16; label.fontSizeMax = 22; }
            b.onClick.AddListener(() => ChooseScenario(sc));
            scenarioButtons.Add(b);
        }
        scenarioInfo = B6Text(row.transform, "About", "", 21, FontStyles.Normal, TextAlignmentOptions.Top, new Color(0.9f, 0.9f, 0.9f));
        B6Place(scenarioInfo.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 38), new Vector2(1400, 76));

        ScenarioConfig keep = chosenScenario;
        if (keep == null)
        {
            string saved = "";
            try { saved = PlayerPrefs.GetString("QuakerScenario", ""); } catch { }
            foreach (ScenarioConfig sc in scenarios) if (sc != null && sc.displayName == saved) keep = sc;
        }
        ChooseScenario(keep);
    }

    void ChooseScenario(ScenarioConfig sc)
    {
        chosenScenario = sc;
        try { PlayerPrefs.SetString("QuakerScenario", sc != null ? sc.displayName : ""); } catch { }
        List<ScenarioConfig> options = new List<ScenarioConfig> { null };
        foreach (ScenarioConfig c in scenarios) if (c != null) options.Add(c);
        for (int i = 0; i < scenarioButtons.Count && i < options.Count; i++)
        {
            ColorBlock cb = scenarioButtons[i].colors;
            cb.normalColor = options[i] == sc ? new Color(1f, 0.82f, 0.35f) : new Color(0.96f, 0.96f, 0.96f);
            cb.selectedColor = cb.normalColor;
            scenarioButtons[i].colors = cb;
        }
        if (scenarioInfo != null)
            scenarioInfo.text = sc == null
                ? "<b>Scenario: Standard</b>  The rulesheet as printed."
                : "<b>Scenario: " + sc.displayName + "</b>" + (sc.experienceGoal.Length > 0 ? " (" + sc.experienceGoal.ToLower() + ")" : "") + "  " + sc.sharedTowerRule;
    }
}
