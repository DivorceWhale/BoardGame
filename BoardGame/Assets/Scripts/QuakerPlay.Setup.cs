using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Build 9: the setup procedure from the finished box, as code. StartMatch runs the same numbered steps, in the
// same order, that a player follows to get from a closed box to the first turn. One method per physical step,
// named after it. Docs/Build9_SetupProcedure.md has the same list, so the two can be read side by side.
//
//   1. Choose the scenario            (Standard, or one of the five Build 5 scenarios)
//   2. Set the tower base on the shake board
//   3. Sort the blocks into the supply
//   4. Shuffle the block deck
//   5. Shuffle the event deck
//   6. Seat the players, tallest first
//   7. Set out the point markers
//   8. The first player makes the opening placement   (the first action of the game)
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
        Step1_ChooseTheScenario(playerCount);
        Step2_SetTheTowerBaseOnTheShakeBoard();
        Step3_SortTheBlocksIntoTheSupply();
        DeckManager blockDeck = Step4_ShuffleTheBlockDeck();
        DeckManager eventDeck = Step5_ShuffleTheEventDeck();
        Step6_SeatThePlayersTallestFirst(playerCount, blockDeck, eventDeck);
        Step7_SetOutThePointMarkers();
        Step8_FirstPlayerMakesTheOpeningPlacement();
    }

    void SetupStep(int n, string what)
    {
        Add("Setup step " + n + " of " + SetupSteps + ": " + what);
    }

    // 1. Choose the scenario. Standard is the rulesheet as printed; a scenario changes only what it lists.
    void Step1_ChooseTheScenario(int playerCount)
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
        SetupStep(1, "Choose the scenario: " + rules.displayName + (sc != null && sc.sharedTowerRule.Length > 0 ? ". " + sc.sharedTowerRule : " (the rulesheet as printed)."));
    }

    // 2. Set the tower base on the shake board, in the middle of the table. Clear anything left from the last match.
    void Step2_SetTheTowerBaseOnTheShakeBoard()
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
        SetupStep(2, "Set the tower base (16 spaces) on the shake board." + (closed > 0 ? " " + closed + " spaces are closed (grey) in " + rules.displayName + "." : ""));
    }

    // 3. Sort the blocks by style into the supply beside the board: 6 styles, 5 of each (or the scenario's count).
    void Step3_SortTheBlocksIntoTheSupply()
    {
        int total = 0;
        foreach (CardData c in blockStyles) if (c != null) total += rules.blocksPerStyle > 0 ? rules.blocksPerStyle : c.copies;
        SetupStep(3, "Sort the blocks into the supply: " + blockStyles.Count + " styles, " + total + " blocks.");
    }

    // 4. Shuffle the block cards into a face-down block deck.
    DeckManager Step4_ShuffleTheBlockDeck()
    {
        foreach (CardData c in blockStyles)
            if (c != null) rules.blockDeck.Add(new DeckEntry { card = c, copies = rules.blocksPerStyle > 0 ? rules.blocksPerStyle : c.copies });   // 5 per style, on each card
        DeckManager blockDeck = NewDeck("BlockDeck", false);
        SeedGame(1);                                  // before the first shuffle (QuakerPlay.Seed.cs)
        blockDeck.Configure(rules.blockDeck);
        SetupStep(4, "Shuffle the block deck: " + blockDeck.TotalAvailable + " block cards, face down. Nobody is dealt a card; everyone draws in the Draw phase.");
        return blockDeck;
    }

    // 5. Shuffle the event cards into a face-down event deck.
    DeckManager Step5_ShuffleTheEventDeck()
    {
        foreach (CardData c in eventCards) if (c != null) rules.eventDeck.Add(new DeckEntry { card = c, copies = B.eventCardCopies });
        DeckManager eventDeck = NewDeck("EventDeck", true);   // section 7: an empty event deck is reshuffled
        eventDeck.Configure(rules.eventDeck);
        SetupStep(5, "Shuffle the event deck: " + eventDeck.TotalAvailable + " event cards, face down.");
        return eventDeck;
    }

    // 6. The players line up tallest to shortest and sit clockwise in that order. The tallest is the first player.
    //    At the table this is judged by eye; here the names are entered in that order on the setup screen.
    void Step6_SeatThePlayersTallestFirst(int playerCount, DeckManager blockDeck, DeckManager eventDeck)
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
        SetupStep(6, "Seat the players, tallest first: " + string.Join(", ", s.players.ConvertAll(p => p.name)) + ". " + s.players[0].name + " is the first player.");
    }

    // 7. Put the point markers in a pile beside the board. Everyone starts with none.
    void Step7_SetOutThePointMarkers()
    {
        SetupStep(7, "Set out the point markers: everyone starts with 0. First to " + B.pointsToWinMatch + " points wins the match.");
    }

    // 8. The first action: starting with the first player, everyone places one block of their choice on the base.
    void Step8_FirstPlayerMakesTheOpeningPlacement()
    {
        machine.BeginOpening();
        openingStyle = FirstStyleInSupply();
        setupPanel.SetActive(false);
        SetupStep(8, s.players[s.buildTurn >= 0 ? s.buildTurn : 0].name + " makes the opening placement. Setup is done.");
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
