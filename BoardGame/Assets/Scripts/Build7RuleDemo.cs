using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Build 7 scene: replays the October 29 playtest step by step through the real TurnStateMachine,
// showing both paths into the rule checks:
//   1. the option list (LegalMoves.Get) never offers an illegal action, and
//   2. a direct attempt (TurnStateMachine.TryApply) is still refused, naming the rule id.
// Press Next step (or Space) to advance.
public class Build7RuleDemo : MonoBehaviour
{
    public ScenarioConfig scenario;        // supplies the decks and 3 players
    public BoardLayout towerBase;          // 4x4 layout used for each player's own tower base
    public CardData lightTremor;           // event card 2, the event face up in the replay

    TurnStateMachine machine;
    GameState s;
    readonly List<string> lines = new List<string>();
    readonly List<KeyValuePair<string, Action>> steps = new List<KeyValuePair<string, Action>>();
    int next;
    TMP_Text logText, header;

    void Start()
    {
        BuildUI();
        Restart();
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) Next();
#endif
    }

    // ---------------- The replay ----------------

    void Restart()
    {
        lines.Clear();
        next = 0;

        DeckManager blockDeck = NewDeck("BlockDeck");
        DeckManager eventDeck = NewDeck("EventDeck");
        blockDeck.Configure(scenario.blockDeck);
        eventDeck.Configure(scenario.eventDeck);

        machine = new TurnStateMachine(blockDeck, eventDeck, scenario);
        machine.Log += m => Add(m);
        s = machine.State;
        s.useRulesheet = true;     // every action now goes through RuleCheck
        s.ownBases = true;         // rulesheet section 2: each player builds on their own 4x4 tower base
        s.board = towerBase;
        s.activePlayer = 0;
        s.phase = TurnPhase.Event;

        steps.Clear();
        Step("Round 1 begins. Player 1 is the first player. Player 2 reaches for the event deck.",
            () => Try(Act(ActionType.DrawEventCard, 1)));
        Step("Event card 2 (Light Tremor) is flipped by Player 1: no effect this round. Draw phase.",
            () => { s.eventCard = lightTremor; s.eventDrawn = true; s.phase = TurnPhase.Draw; });
        Step("Player 1 draws a block card.", () => Try(Act(ActionType.DrawBlockCard, 0)));
        Step("Player 1 tries to draw a second block card.", () => Try(Act(ActionType.DrawBlockCard, 0)));
        Step("Players 2 and 3 draw. Build phase: Player 1 places first.", () =>
        {
            Try(Act(ActionType.DrawBlockCard, 1));
            Try(Act(ActionType.DrawBlockCard, 2));
            s.phase = TurnPhase.Place;
            s.buildTurn = 0;
        });
        Step("Option list for Player 1 (path 1: the legal-move function).", () => Options(0));
        Step("Player 2 tries to place before it is their turn.", () => Try(Place(1, 1, 6)));
        Step("Player 1 tries to place on Player 2's tower base.", () => Try(Place(0, 1, 6)));
        Step("Player 1 places on space 6 of their own tower base.", () => { Try(Place(0, 0, 6)); s.buildTurn = 1; });
        Step("STRANGER TEST #2: \"When does your tower first shake?\" The testers shook as soon as one tower had a block. Option list for Player 1 first:",
            () => Options(0));
        Step("...and the direct attempt: Player 1 tries to start the shake now.", () => Try(Act(ActionType.StartShake, 0)));
        Step("Players 2 and 3 place. Every standing player has placed: Shake phase.", () =>
        {
            Try(Place(1, 1, 1)); s.buildTurn = 2;
            Try(Place(2, 2, 16));
            s.buildTurn = -1;
            s.phase = TurnPhase.Shake;
        });
        Step("Player 2 tries to run the shake.", () => Try(Act(ActionType.StartShake, 1)));
        Step("Player 1, the first player, runs the shake (motor 5 seconds).", () => Try(Act(ActionType.StartShake, 0)));
        Step("STRANGER TEST #1: \"What if the towers fail? Is it a tie?\" Later in the game Player 3 is out, and Players 1 and 2 both collapse in the same shake with 4 blocks each.", () =>
        {
            foreach (PlayerState p in s.players) p.inGame = false;
            s.players[0].fellThisShake = true; s.players[0].blocksBeforeShake = 4;
            s.players[1].fellThisShake = true; s.players[1].blocksBeforeShake = 4;
            s.players[2].fellThisShake = false;
            s.phase = TurnPhase.EndCheck;
            machine.StepPhase();
        });
        Step("Replay finished. Press Restart to run it again.", () => { });

        Add("<b>Build 7 - rule checks replay of the October 29 playtest.</b> Press <b>Next step</b> or Space.");
        Refresh();
    }

    void Next()
    {
        if (next >= steps.Count) return;
        Add("<color=#FFD15A>" + (next + 1) + ". " + steps[next].Key + "</color>");
        steps[next].Value();
        next++;
        Refresh();
    }

    void Step(string text, Action action) { steps.Add(new KeyValuePair<string, Action>(text, action)); }

    void Try(GameAction a)
    {
        RuleResult r = machine.TryApply(a);
        if (r.legal)
        {
            Add("<color=#9BE59B>   Legal: " + s.players[a.player].name + " " + Describe(a) + "</color>");
        }
        else
        {
            RuleBook.Rule rule = RuleBook.Get(r.ruleId);
            Add("<color=#FF6B6B>   REFUSED " + r.ruleId + ": " + r.reason + "</color>");
            Add("<color=#BBBBBB>      " + r.ruleId + " on the rulesheet (section " + rule.section + "): " + rule.text + "</color>");
        }
    }

    void Options(int player)
    {
        List<GameAction> legal = LegalMoves.Get(s, s.players[player]);
        bool shakeOffered = false;
        foreach (GameAction g in legal) if (g.type == ActionType.StartShake) shakeOffered = true;
        Add("   Options for " + s.players[player].name + ": " + LegalMoves.Describe(s, legal));
        Add("   Start shake offered: " + (shakeOffered ? "yes" : "<b>no</b>"));
    }

    GameAction Act(ActionType t, int player) { return new GameAction { type = t, player = player }; }

    GameAction Place(int player, int baseOwner, int space)
    {
        SpaceData sd = towerBase.Get(space);
        return new GameAction
        {
            type = ActionType.PlaceBlock,
            player = player,
            card = s.players[player].heldBlock,
            baseOwner = baseOwner,
            space = space,
            spaceName = sd != null ? sd.displayName : "?"
        };
    }

    static string Describe(GameAction a)
    {
        switch (a.type)
        {
            case ActionType.DrawEventCard: return "flips the event card.";
            case ActionType.DrawBlockCard: return "draws a block card.";
            case ActionType.StartShake: return "starts the shake.";
            default: return "places " + (a.card != null ? a.card.title : "?") + " on space " + a.space + " (" + a.spaceName + ") of their own base.";
        }
    }

    DeckManager NewDeck(string deckName)
    {
        Transform old = transform.Find(deckName);
        if (old != null) Destroy(old.gameObject);
        GameObject go = new GameObject(deckName);
        go.transform.SetParent(transform, false);
        DeckManager d = go.AddComponent<DeckManager>();
        d.allowRefill = false;
        return d;
    }

    void Add(string line) { lines.Add(line); Debug.Log("[Build 7] " + line); }

    // ---------------- Minimal screen ----------------

    void Refresh()
    {
        int from = Mathf.Max(0, lines.Count - 22);
        logText.text = string.Join("\n", lines.GetRange(from, lines.Count - from).ToArray());
        header.text = "<b>QUAKER - Build 7: the rulesheet as legality checks</b>    step " + next + " of " + steps.Count;
    }

    void BuildUI()
    {
        GameObject root = new GameObject("Build7Canvas", typeof(RectTransform));
        root.transform.SetParent(transform, false);
        Canvas c = root.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler cs = root.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();

        Image bg = Panel(root.transform, new Color(0.12f, 0.13f, 0.16f));
        Anchor(bg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        header = Label(root.transform, 36);
        Anchor(header.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -80), new Vector2(-40, -20));

        Image logBox = Panel(root.transform, new Color(0.07f, 0.07f, 0.09f));
        Anchor(logBox.rectTransform, new Vector2(0, 0), new Vector2(0.64f, 1), new Vector2(40, 120), new Vector2(-10, -95));
        logText = Label(logBox.transform, 24);
        Anchor(logText.rectTransform, Vector2.zero, Vector2.one, new Vector2(20, 15), new Vector2(-20, -15));

        Image rulesBox = Panel(root.transform, new Color(0.95f, 0.95f, 0.93f));
        Anchor(rulesBox.rectTransform, new Vector2(0.64f, 0), new Vector2(1, 1), new Vector2(10, 120), new Vector2(-40, -95));
        TMP_Text rules = Label(rulesBox.transform, 23);
        rules.color = Color.black;
        rules.text = RuleBook.DescribeShort();
        rules.enableAutoSizing = true;
        rules.fontSizeMin = 16;
        rules.fontSizeMax = 23;
        Anchor(rules.rectTransform, Vector2.zero, Vector2.one, new Vector2(18, 12), new Vector2(-18, -12));

        Button nextButton = MakeButton(root.transform, "Next step (Space)");
        Anchor((RectTransform)nextButton.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 30), new Vector2(340, 90));
        nextButton.onClick.AddListener(Next);
        Button restart = MakeButton(root.transform, "Restart");
        Anchor((RectTransform)restart.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(360, 30), new Vector2(560, 90));
        restart.onClick.AddListener(Restart);
    }

    static Image Panel(Transform parent, Color col)
    {
        GameObject go = new GameObject("Panel", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = col;
        return img;
    }

    static TMP_Text Label(Transform parent, float size)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.TopLeft;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.overflowMode = TextOverflowModes.Truncate;
        t.raycastTarget = false;
        return t;
    }

    static Button MakeButton(Transform parent, string label)
    {
        Image img = Panel(parent, Color.white);
        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        TMP_Text t = Label(img.transform, 24);
        t.color = Color.black;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.text = label;
        Anchor(t.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return b;
    }

    static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = offMin;
        rt.offsetMax = offMax;
    }
}
