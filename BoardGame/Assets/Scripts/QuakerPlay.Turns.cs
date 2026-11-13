using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Whose turn is it? Every player picks a colour at setup (the swatch on their name row). In the game that
// colour marks their name in the turn order and the prompt, a thin frame around the screen while they act,
// and a short "Ben's turn" banner whenever the turn passes to them.
// Ready prompts: before a placement under event 14 (ten-count) or event 3 (blind), the player clicks Ready
// first, so the timer doesn't run and the tower doesn't vanish while the mouse is still being handed over.
public partial class QuakerPlay
{
    // 8 colours that read well on the dark UI and in print; the first 4 are the defaults for players 1-4.
    static readonly Color[] Palette =
    {
        new Color32(0xE0, 0x7A, 0x2E, 0xFF),   // orange
        new Color32(0x3B, 0x82, 0xF6, 0xFF),   // blue
        new Color32(0x22, 0xA3, 0x5A, 0xFF),   // green
        new Color32(0x9B, 0x59, 0xD0, 0xFF),   // purple
        new Color32(0xE0, 0x48, 0x48, 0xFF),   // red
        new Color32(0xE8, 0xC0, 0x2E, 0xFF),   // yellow
        new Color32(0x1F, 0xB5, 0xB0, 0xFF),   // teal
        new Color32(0xE8, 0x5A, 0x9E, 0xFF),   // pink
    };
    static readonly string[] PaletteNames = { "Orange", "Blue", "Green", "Purple", "Red", "Yellow", "Teal", "Pink" };

    [SerializeField, HideInInspector] List<Button> colorButtons = new List<Button>();
    [SerializeField, HideInInspector] GameObject turnFrame, turnBanner, readyPanel;
    [SerializeField, HideInInspector] TMP_Text bannerName, bannerAction, readyTitle, readyText;
    [SerializeField, HideInInspector] Button readyButton;

    readonly int[] setupColor = { 0, 1, 2, 3 };   // palette index per setup row
    int[] playerColor = { 0, 1, 2, 3 };           // palette index per player, fixed at match start
    int bannerWho = -1;
    string bannerKey = "";

    // Banners play one after another, BannerSeconds each, so an event reveal and a turn change never overwrite.
    struct BannerMsg { public string title, sub; public Color color; }
    readonly List<BannerMsg> bannerQueue = new List<BannerMsg>();
    CardData bannerEvent;          // the last event card announced
    string coffeeKey = "";         // the round Coffee Break's "nobody places" banner was shown for
    string shakeOnlyKey = "";      // the shake-only round whose banner was shown
    static readonly Color EventGold = new Color(1f, 0.82f, 0.35f);
    float bannerShownAt = -10f;
    const float BannerSeconds = 2f;
    string readyConfirmed = "";

    // ---------------- Building (Editor and Play) ----------------

    // Editor: adds the colour swatches, turn frame, banner and Ready box to Play.unity's saved UI.
    public void AddTurnUIToScene()
    {
        if (canvasRoot == null) return;
        foreach (string n in new[] { "TurnFrame", "TurnBanner", "ReadyPanel" })
        {
            Transform t = canvasRoot.transform.Find(n);
            if (t != null) DestroyImmediate(t.gameObject);
        }
        foreach (GameObject row in nameRows)
        {
            if (row == null) continue;
            Transform sw = row.transform.Find("Color");
            if (sw != null) DestroyImmediate(sw.gameObject);
        }
        turnFrame = turnBanner = readyPanel = null;
        colorButtons.Clear();
        EnsureTurnUI();
    }

    void EnsureTurnUI()
    {
        if (canvasRoot == null || menuScene) return;
        Transform root = canvasRoot.transform;

        // Colour swatch on every setup row, between the name and the arrows
        if (colorButtons.Count == 0)
        {
            for (int i = 0; i < nameRows.Count; i++)
            {
                if (nameRows[i] == null) continue;
                RectTransform field = nameFields[i] != null ? (RectTransform)nameFields[i].transform : null;
                if (field != null) B6Place(field, new Vector2(0.5f, 0.5f), new Vector2(-60, 0), new Vector2(380, 60));
                GameObject go = new GameObject("Color", typeof(RectTransform));
                go.transform.SetParent(nameRows[i].transform, false);
                B6Place((RectTransform)go.transform, new Vector2(0.5f, 0.5f), new Vector2(188, 0), new Vector2(56, 56));
                Image img = go.AddComponent<Image>();
                img.sprite = roundedSprite;
                img.type = roundedSprite != null ? Image.Type.Sliced : Image.Type.Simple;
                img.color = Palette[i % Palette.Length];
                Button b = go.AddComponent<Button>();
                b.targetGraphic = img;
                ColorBlock cb = b.colors;
                cb.normalColor = Color.white; cb.highlightedColor = new Color(1f, 1f, 1f, 0.85f); cb.pressedColor = new Color(0.8f, 0.8f, 0.8f);
                b.colors = cb;
                colorButtons.Add(b);
            }
        }

        // Thin frame around the screen in the acting player's colour
        if (turnFrame == null)
        {
            turnFrame = new GameObject("TurnFrame", typeof(RectTransform));
            turnFrame.transform.SetParent(root, false);
            B6Stretch((RectTransform)turnFrame.transform);
            foreach (var edge in new[] { (Vector2.zero, new Vector2(1, 0), 0), (new Vector2(0, 1), Vector2.one, 1), (Vector2.zero, new Vector2(0, 1), 2), (new Vector2(1, 0), Vector2.one, 3) })
            {
                GameObject bar = new GameObject("Edge", typeof(RectTransform));
                bar.transform.SetParent(turnFrame.transform, false);
                RectTransform rt = (RectTransform)bar.transform;
                rt.anchorMin = edge.Item1; rt.anchorMax = edge.Item2;
                const float w = 10f;
                rt.offsetMin = edge.Item3 == 1 ? new Vector2(0, -w) : edge.Item3 == 3 ? new Vector2(-w, 0) : Vector2.zero;
                rt.offsetMax = edge.Item3 == 0 ? new Vector2(0, w) : edge.Item3 == 2 ? new Vector2(w, 0) : Vector2.zero;
                Image im = bar.AddComponent<Image>();
                im.raycastTarget = false;
            }
            turnFrame.transform.SetAsFirstSibling();   // behind every panel
            turnFrame.SetActive(false);
        }

        // "Ben's turn" banner, upper middle
        if (turnBanner == null)
        {
            turnBanner = new GameObject("TurnBanner", typeof(RectTransform), typeof(CanvasGroup));
            turnBanner.transform.SetParent(root, false);
            B6Place((RectTransform)turnBanner.transform, new Vector2(0.5f, 0.66f), Vector2.zero, new Vector2(720, 140));
            Image bg = turnBanner.AddComponent<Image>();
            bg.sprite = roundedSprite;
            bg.type = roundedSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            bg.color = new Color(0.04f, 0.05f, 0.07f, 0.82f);
            bg.raycastTarget = false;
            turnBanner.GetComponent<CanvasGroup>().blocksRaycasts = false;
            bannerName = B6Text(turnBanner.transform, "Name", "", 58, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            B6Place(bannerName.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(700, 72));
            bannerAction = B6Text(turnBanner.transform, "Action", "", 26, FontStyles.Normal, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.9f));
            B6Place(bannerAction.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -38), new Vector2(700, 36));
            turnBanner.SetActive(false);
        }

        // Ready box for events 3 and 14
        if (readyPanel == null)
        {
            readyPanel = new GameObject("ReadyPanel", typeof(RectTransform));
            readyPanel.transform.SetParent(root, false);
            B6Stretch((RectTransform)readyPanel.transform);
            readyPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.45f);   // blocks clicks on the table
            GameObject box = B6Box(readyPanel.transform, "Box", new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(780, 400));
            readyTitle = B6Text(box.transform, "Title", "", 54, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            B6Place(readyTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(740, 70));
            readyText = B6Text(box.transform, "Text", "", 26, FontStyles.Normal, TextAlignmentOptions.Top, Color.white);
            B6Place(readyText.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -185), new Vector2(700, 140));
            readyButton = B6Button(box.transform, "ReadyButton", "Ready", new Vector2(0.5f, 0), new Vector2(0, 65), new Vector2(260, 74));
            readyButton.GetComponentInChildren<TMP_Text>().fontSize = 32;
            readyPanel.SetActive(false);
        }

        // Banner and Ready box sit under the Cards and Help panels, over the HUD
        Transform cards = root.Find("CardsPanel");
        if (cards != null)
        {
            turnBanner.transform.SetSiblingIndex(cards.GetSiblingIndex());
            readyPanel.transform.SetSiblingIndex(cards.GetSiblingIndex());
        }
    }

    // ---------------- Play ----------------

    void WireTurnUI()
    {
        for (int i = 0; i < 4; i++)
        {
            try { setupColor[i] = PlayerPrefs.GetInt("QuakerColor" + i, i); } catch { setupColor[i] = i; }
            setupColor[i] = Mathf.Clamp(setupColor[i], 0, Palette.Length - 1);
        }
        FixColorClashes(-1);
        for (int i = 0; i < colorButtons.Count; i++)
        {
            int k = i;
            if (colorButtons[i] != null) colorButtons[i].onClick.AddListener(() => NextColor(k));
        }
        if (readyButton != null) readyButton.onClick.AddListener(() => { readyConfirmed = ReadyKey(); readyPanel.SetActive(false); });
        ShowSetupColors();
    }

    // The next colour nobody else among the active players is using.
    void NextColor(int row)
    {
        for (int step = 1; step <= Palette.Length; step++)
        {
            int c = (setupColor[row] + step) % Palette.Length;
            bool taken = false;
            for (int j = 0; j < setupCount; j++) if (j != row && setupColor[j] == c) taken = true;
            if (!taken) { setupColor[row] = c; break; }
        }
        SaveColors();
        ShowSetupColors();
    }

    // Two active rows with the same colour (e.g. after changing the player count): move the later one on.
    void FixColorClashes(int keep)
    {
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < i; j++)
                if (setupColor[i] == setupColor[j])
                {
                    int move = i == keep ? j : i;
                    for (int c = 0; c < Palette.Length; c++)
                    {
                        bool used = false;
                        for (int k = 0; k < 4; k++) if (k != move && setupColor[k] == c) used = true;
                        if (!used) { setupColor[move] = c; break; }
                    }
                }
    }

    void SwapColors(int a, int b)
    {
        if (a < 0 || b < 0 || a >= 4 || b >= 4) return;
        int t = setupColor[a]; setupColor[a] = setupColor[b]; setupColor[b] = t;
        SaveColors();
        ShowSetupColors();
    }

    void SaveColors()
    {
        try { for (int i = 0; i < 4; i++) PlayerPrefs.SetInt("QuakerColor" + i, setupColor[i]); PlayerPrefs.Save(); } catch { }
    }

    void ShowSetupColors()
    {
        for (int i = 0; i < colorButtons.Count; i++)
            if (colorButtons[i] != null) ((Image)colorButtons[i].targetGraphic).color = Palette[setupColor[i]];
    }

    // Called by StartMatch: player i plays in the colour of setup row i.
    void FixPlayerColors(int count)
    {
        playerColor = new int[count];
        for (int i = 0; i < count; i++) playerColor[i] = setupColor[i];
        bannerWho = -1;
        bannerKey = "";
        bannerQueue.Clear();
        bannerEvent = null;
        coffeeKey = "";
        shakeOnlyKey = "";
        readyConfirmed = "";
    }

    Color PlayerColor(int player)
    {
        return player >= 0 && player < playerColor.Length ? Palette[playerColor[player]] : Color.white;
    }

    string ColorName(int player)
    {
        return player >= 0 && player < playerColor.Length ? PaletteNames[playerColor[player]] : "";
    }

    // "<color=#3B82F6>Ben</color>" for text.
    string Tinted(int player, string text)
    {
        return "<color=#" + ColorUtility.ToHtmlStringRGB(PlayerColor(player)) + ">" + text + "</color>";
    }

    // Every frame: frame colour, banner when the turn passes, and the Ready gate.
    // Returns true while a Ready prompt is waiting (no aiming, no ten-count).
    bool UpdateTurnUI()
    {
        bool inGame = machine != null && !machine.MatchOver && setupPanel != null && !setupPanel.activeSelf
            && (gameOverPanel == null || !gameOverPanel.activeSelf);
        int who = inGame ? Actor() : -1;

        if (turnFrame != null)
        {
            bool show = inGame && who >= 0 && !busy;
            if (turnFrame.activeSelf != show) turnFrame.SetActive(show);
            if (show)
            {
                Color c = PlayerColor(who); c.a = 0.9f;
                foreach (Image im in turnFrame.GetComponentsInChildren<Image>()) im.color = c;
            }
        }

        if (inGame)
        {
            // Event reveal: everyone sees what the card just flipped does.
            if (s.eventCard != null && s.eventCard != bannerEvent)
            {
                bannerEvent = s.eventCard;
                QueueBanner(s.eventCard.title, EventEffect(s.eventCard), EventGold);
            }
            // Coffee Break: the round jumps from drawing straight to the shake.
            if (RuleCheck.EventNumber(s) == 4 && (s.phase == TurnPhase.Shake || s.phase == TurnPhase.CollapseCheck))
            {
                string k = s.gameNumber + ":" + s.roundInGame;
                if (k != coffeeKey)
                {
                    coffeeKey = k;
                    QueueBanner("Coffee Break", (coffeeDrawn > 0 ? "Everyone drew and discarded a block card. " : "") + "Nobody places this round. On to the shake.", EventGold);
                    coffeeDrawn = 0;
                }
            }
            // R-19: a shake-only round has no event card, so say why nobody flips one.
            if (s.deckShort && s.eventCard == null && (s.phase == TurnPhase.Shake || s.phase == TurnPhase.CollapseCheck))
            {
                string k = s.gameNumber + ":" + s.roundInGame;
                if (k != shakeOnlyKey) { shakeOnlyKey = k; QueueBanner("Shake-only round", (s.fuseOut ? rules.displayName + ": the " + rules.roundLimit + " build rounds are over" : "The block deck is short") + ": no event card, no drawing, no placing. Each shake is stronger.", EventGold); }
            }
            // Turn banner: when the acting player changes, or the same player moves on to placing (after picking a
            // rival or running the aftershocks) or to the Structural Audit second draw.
            string key = who + ":" + TurnContext();
            if (!busy && who >= 0 && key != bannerKey)
            {
                bannerKey = key;
                bannerWho = who;
                QueueBanner(s.players[who].name + "\u2019s turn", BannerAction(who), PlayerColor(who));
            }
        }
        else
        {
            bannerWho = -1; bannerKey = ""; bannerEvent = null;
            bannerQueue.Clear();
        }

        // The buttons that need pressing next glow in the acting player's colour: Draw Event, Draw Block, and the
        // action bar (start the shake, redraw, use the other card, pick a rival).
        bool glow = inGame && !busy && who >= 0;
        Glow(drawEventButton, glow, who);
        Glow(drawBlockButton, glow, who);
        if (actionArea != null)
            foreach (Button b in actionArea.GetComponentsInChildren<Button>()) Glow(b, glow, who);

        ShowBanners(inGame);

        // Ready prompt for timed or blind placements
        bool waiting = inGame && !busy && NeedsReady() && readyConfirmed != ReadyKey();
        if (readyPanel != null)
        {
            if (readyPanel.activeSelf != waiting) readyPanel.SetActive(waiting);
            if (waiting && readyTitle != null)
            {
                int p = s.buildTurn;
                readyTitle.text = Tinted(p, s.players[p].name) + ", ready?";
                readyText.text = RuleCheck.TenCount(s)
                    ? "<b>Foreman Tightens the Schedule:</b> you have <b>" + B.tenCountSeconds + " seconds</b> to place " + YourBlock(s.players[p].heldBlock, "", "")
                      + ".\nThe timer starts when you press Ready."
                    : "<b>Rebar Delivery:</b> place " + YourBlock(s.players[p].heldBlock, "", "") + " <b>blind</b>.\nThe tower disappears when you press Ready.";
            }
        }
        return waiting;
    }

    string BannerAction(int who)
    {
        int ev = RuleCheck.EventNumber(s);
        switch (s.phase)
        {
            case TurnPhase.Opening: return "Place any block from the supply";
            case TurnPhase.Event: return "Draw the event card";
            case TurnPhase.Draw: return ev == 13 ? "Draw your block cards (you get 2 this round)" : "Draw your block card";
            case TurnPhase.Shake:
            {
                int n = RuleCheck.ShakeCount(s);
                string sh = "Start the shake";
                if (n > 1) sh += " \u2013 it runs " + (n == 2 ? "twice" : n + " times");
                if (RuleCheck.ShakeSpeed(s) > 1f) sh += (n > 1 ? " at" : " \u2013") + " double speed";
                if (s.deckShort) sh += " \u2013 stronger than last time";
                return sh;
            }
            case TurnPhase.Place:
            {
                if (RuleCheck.RivalPending(s)) return "Second Crew Arrives: pick a rival to swap their block card";
                if (RuleCheck.AftershocksPending(s)) return "Aftershock Cluster: run " + B.aftershockShakes + " shakes before anyone places";
                PlayerState p = s.players[who];
                CardData c = p.heldBlock;
                if (c == null) return "Structural Audit: draw a second block card";
                string place = "Place " + YourBlock(c, "", "");
                if (p.extraOption != null) place += " or " + YourBlock(p.extraOption, "", "").Replace("your ", "");
                string limit = PlacementLimit(ev);
                if (limit.Length > 0) place += " \u2013 " + limit;
                if (ev == 1 && !p.redrewThisRound) place += " (or swap it once with Redraw)";
                return place;
            }
        }
        return "";
    }

    // Where a block may not go this round (cards 6, 7, 11, 12), for the placing player's banner.
    static string PlacementLimit(int ev)
    {
        switch (ev)
        {
            case 6: return "on top of another block";
            case 7: return "covering at least one outer space";
            case 11: return "not on the 4 middle spaces";
            case 12: return "not on the 4 corner spaces";
        }
        return "";
    }

    // What makes the same player's turn change meaning (so it gets its own banner).
    string TurnContext()
    {
        if (s.phase == TurnPhase.Place)
        {
            if (RuleCheck.RivalPending(s)) return "rival";
            if (RuleCheck.AftershocksPending(s)) return "aftershocks";
            return SecondDrawPending() ? "audit" : "place";
        }
        return s.phase == TurnPhase.Shake ? "shake" : "";
    }

    // The middle line of an event card: what it does.
    static string EventEffect(CardData c)
    {
        string[] lines = c.bodyText.Replace("\r", "").Split('\n');
        return lines.Length >= 3 ? lines[1] : c.bodyText;
    }

    void QueueBanner(string title, string sub, Color color)
    {
        if (bannerQueue.Count == 0) bannerShownAt = Time.time;
        if (bannerQueue.Count >= 4) bannerQueue.RemoveAt(1);   // never fall far behind the game
        bannerQueue.Add(new BannerMsg { title = title, sub = sub, color = color });
        GameSounds.Play(GameSound.TurnBanner, 0.6f);
    }

    // Shows the front of the queue for BannerSeconds, fading in and out, then moves on to the next.
    void ShowBanners(bool inGame)
    {
        if (turnBanner == null) return;
        if (bannerQueue.Count > 0 && Time.time - bannerShownAt >= BannerSeconds)
        {
            bannerQueue.RemoveAt(0);
            bannerShownAt = Time.time;
        }
        bool show = inGame && bannerQueue.Count > 0;
        if (turnBanner.activeSelf != show) turnBanner.SetActive(show);
        if (!show) return;
        BannerMsg m = bannerQueue[0];
        bool changed = (bannerName != null && bannerName.text != m.title) || (bannerAction != null && bannerAction.text != m.sub);
        if (bannerName != null && bannerName.text != m.title) bannerName.text = m.title;
        if (bannerName != null) bannerName.color = m.color;
        if (bannerAction != null && bannerAction.text != m.sub) bannerAction.text = m.sub;
        if (changed) FitBanner();
        float t = Time.time - bannerShownAt;
        turnBanner.GetComponent<CanvasGroup>().alpha = t < 0.15f ? t / 0.15f : t > BannerSeconds - 0.4f ? Mathf.Max(0f, (BannerSeconds - t) / 0.4f) : 1f;
    }

    // Sizes the banner to its text so nothing spills out: the title shrinks to fit one line (down to BannerTitleMin),
    // and the line under it wraps onto up to three lines, shrinking a little if it still needs more room.
    // The box then grows or shrinks around both.
    const float BannerWidth = 860f, BannerPad = 20f, BannerTitleHeight = 72f, BannerTitleMin = 30f;
    const float BannerSubSize = 26f, BannerSubMin = 18f, BannerSubMaxHeight = 110f;

    void FitBanner()
    {
        if (turnBanner == null || bannerName == null || bannerAction == null) return;
        float inner = BannerWidth - 2f * BannerPad;

        bannerName.textWrappingMode = TextWrappingModes.NoWrap;
        bannerName.enableAutoSizing = true;
        bannerName.fontSizeMax = 58f;
        bannerName.fontSizeMin = BannerTitleMin;
        bannerName.overflowMode = TextOverflowModes.Ellipsis;   // only if even the smallest size is too wide

        bannerAction.textWrappingMode = TextWrappingModes.Normal;
        bannerAction.enableAutoSizing = false;
        bannerAction.fontSize = BannerSubSize;
        bannerAction.overflowMode = TextOverflowModes.Overflow;
        float subHeight = 0f;
        if (!string.IsNullOrEmpty(bannerAction.text))
        {
            bannerAction.gameObject.SetActive(true);
            subHeight = bannerAction.GetPreferredValues(bannerAction.text, inner, 0f).y;
            if (subHeight > BannerSubMaxHeight)
            {
                // Too long for three lines: let it shrink into the space it has.
                bannerAction.enableAutoSizing = true;
                bannerAction.fontSizeMax = BannerSubSize;
                bannerAction.fontSizeMin = BannerSubMin;
                bannerAction.overflowMode = TextOverflowModes.Ellipsis;
                subHeight = BannerSubMaxHeight;
            }
        }

        float height = BannerPad + BannerTitleHeight + (subHeight > 0f ? 6f + subHeight : 0f) + BannerPad;
        RectTransform box = (RectTransform)turnBanner.transform;
        box.sizeDelta = new Vector2(BannerWidth, height);
        // Auto-size can land a few pixels wide, so the title gets a little less width than the line under it.
        B6Place(bannerName.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -(BannerPad + BannerTitleHeight * 0.5f)), new Vector2(inner - 20f, BannerTitleHeight));
        B6Place(bannerAction.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -(BannerPad + BannerTitleHeight + 6f + subHeight * 0.5f)), new Vector2(inner, Mathf.Max(subHeight, 1f)));
        bannerAction.gameObject.SetActive(subHeight > 0f);
    }

    // Called after a move is made, for banners about other players: Second Crew Arrives swaps the rival's card.
    void OnActionDone(GameAction a)
    {
        // Every draw gets its own banner. The last player's draw moves the round straight to placing, and the card
        // panel then shows the first builder's card, so without this the last drawer never sees what they pulled.
        if (a.type == ActionType.DrawBlockCard && a.player >= 0 && a.player < s.players.Count)
        {
            PlayerState p = s.players[a.player];
            if (p.heldBlock != null)
                QueueBanner(p.name + " drew " + ABlock(p.heldBlock) + (p.extraOption != null ? " and " + ABlock(p.extraOption) : ""),
                    s.phase == TurnPhase.Draw ? "Their block card for this round"
                    : p.placementsThisRound > 0 ? "Structural Audit: their second block"
                    : "Everyone has drawn. On to placing.",
                    PlayerColor(a.player));
        }
        if (a.type == ActionType.PickRival && a.target >= 0 && a.target < s.players.Count)
        {
            CardData now = s.players[a.target].heldBlock;
            QueueBanner(s.players[a.target].name + "\u2019s card was swapped",
                "Second Crew Arrives \u2013 " + (now != null ? "now " + (now.title.StartsWith("The ") ? now.title : "a " + now.title) : "the block deck was empty"),
                PlayerColor(a.target));
        }
    }

    // Structural Audit (event 17): the last player has placed once and now has to draw a second block card.
    bool SecondDrawPending()
    {
        if (s == null || s.phase != TurnPhase.Place || s.buildTurn < 0) return false;
        if (RuleCheck.RivalPending(s) || RuleCheck.AftershocksPending(s)) return false;
        PlayerState p = s.players[s.buildTurn];
        return p.heldBlock == null && p.placementsThisRound > 0;
    }

    // A pulsing tint on a button while it is the thing to press next (it is enabled), white otherwise.
    void Glow(Button b, bool active, int who)
    {
        if (b == null) return;
        ColorBlock cb = b.colors;
        Color normal = new Color(0.96f, 0.96f, 0.96f);
        if (active && b.interactable)
        {
            float k = 0.35f + 0.35f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f));
            normal = Color.Lerp(normal, PlayerColor(who), k);
        }
        if (cb.normalColor == normal) return;
        cb.normalColor = normal;
        cb.selectedColor = normal;
        b.colors = cb;
    }

    // ---------------- R-26: a block that fits nowhere ----------------

    string deadCheckKey = "";

    // Once per turn (and again whenever the tower or the card changes): if the builder's block has no spot on
    // the real tower that the rules accept, they discard it and draw again (R-26), or skip if they can't.
    // Returns true when it changed the card or skipped the turn.
    bool CheckDeadCard()
    {
        if (machine == null || machine.MatchOver || busy || shaking || s.phase != TurnPhase.Place || s.buildTurn < 0) return false;
        if (RuleCheck.RivalPending(s) || RuleCheck.AftershocksPending(s)) return false;
        PlayerState p = s.players[s.buildTurn];
        if (p.heldBlock == null) return false;
        string key = s.gameNumber + ":" + s.roundInGame + ":" + s.buildTurn + ":" + p.placementsThisRound + ":" + placed.Count + ":" + p.heldBlock.id
            + ":" + (p.extraOption != null ? p.extraOption.id : 0);
        if (key == deadCheckKey) return false;
        deadCheckKey = key;
        if (HasPhysicalSpot(p.heldBlock, p.index)) return false;
        if (p.extraOption != null)
        {
            // Permit Renewed: the other card counts too. The rules only accept the card in hand, so check it there.
            CardData held = p.heldBlock;
            p.heldBlock = p.extraOption; p.extraOption = held;
            bool otherFits = HasPhysicalSpot(p.heldBlock, p.index);
            p.extraOption = p.heldBlock; p.heldBlock = held;
            if (otherFits) return false;
        }

        CardData dead = p.heldBlock;
        if (machine.RedrawDeadCard(p, "no space on the tower fits it"))
        {
            QueueBanner(dead.title + " fits nowhere", p.name + " discards it and draws " + (p.heldBlock != null ? ABlock(p.heldBlock) : "nothing"), PlayerColor(p.index));
            revealPlayer = p.index; revealUntil = Time.time + RevealSeconds;
        }
        else
        {
            QueueBanner(dead.title + " fits nowhere", p.name + " can't draw another card, so they skip", PlayerColor(p.index));
            machine.SkipPlacement(p, "no space on the tower fits the " + dead.title + " and no card is left to redraw.");
        }
        deadCheckKey = "";
        AfterMove();
        return true;
    }

    // Is there any spot on the tower, in any of the 24 turns, where this block can rest and the rules accept it?
    bool HasPhysicalSpot(CardData style, int who)
    {
        GameObject probe = SpawnBlock(style, true);
        GameObject oldGhost = ghost;
        ghost = probe;   // RestingLevels measures the ghost's cubes
        bool found = false;
        try
        {
            foreach (Quaternion turn in TestTurns())
            {
                probe.transform.rotation = turn;
                Bounds gb = RenderBounds(probe);
                int wx = Mathf.Max(1, Mathf.RoundToInt(gb.size.x / cell)), wz = Mathf.Max(1, Mathf.RoundToInt(gb.size.z / cell));
                if (wx > 4 || wz > 4) continue;
                List<Bounds> cubes = new List<Bounds>();
                foreach (Renderer r in probe.GetComponentsInChildren<Renderer>())
                {
                    Bounds b = r.bounds;
                    b.center = new Vector3(b.center.x - gb.center.x, b.center.y - gb.min.y, b.center.z - gb.center.z);
                    cubes.Add(b);
                }
                for (int sx = 0; sx + wx <= 4 && !found; sx++)
                    for (int sz = 0; sz + wz <= 4 && !found; sz++)
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
                            GameAction a = new GameAction
                            {
                                type = ActionType.PlaceBlock, player = who, card = style, space = space,
                                angle = Mathf.RoundToInt(turn.eulerAngles.y), physical = true, onBlock = lv.onBlock,
                                covers = new List<int>(covers).ToArray(), offBase = off > 0
                            };
                            if (LegalMoves.Validate(s, a).legal) { found = true; break; }
                        }
                    }
                if (found) break;
            }
        }
        finally
        {
            ghost = oldGhost;
            Destroy(probe);
        }
        return found;
    }

    // "a T-Block", "an L-Block", but "The Cube" (not "a The Cube").
    static string ABlock(CardData c)
    {
        string t = c.title;
        if (t.StartsWith("The ")) return t;
        bool letterName = t.Length > 1 && t[1] == '-';                     // read as a letter: "L-Block" is "el-block"
        bool an = letterName ? "AEFHILMNORSX".IndexOf(char.ToUpperInvariant(t[0])) >= 0 : "AEIOU".IndexOf(char.ToUpperInvariant(t[0])) >= 0;
        return (an ? "an " : "a ") + t;
    }

    // "your T-Block", but "The Cube" (not "your The Cube").
    static string YourBlock(CardData c, string open, string close)
    {
        if (c == null) return "your block";
        return c.title.StartsWith("The ") ? open + c.title + close : "your " + open + c.title + close;
    }

    // A placement that needs the player to confirm they have the mouse first.
    bool NeedsReady()
    {
        if (s == null || s.phase != TurnPhase.Place || s.buildTurn < 0) return false;
        if (RuleCheck.RivalPending(s) || RuleCheck.AftershocksPending(s)) return false;
        if (s.players[s.buildTurn].heldBlock == null) return false;
        return RuleCheck.TenCount(s) || RuleCheck.Blind(s);
    }

    string ReadyKey()
    {
        return s == null ? "" : s.gameNumber + ":" + s.roundInGame + ":" + s.buildTurn + ":" + s.players[s.buildTurn].placementsThisRound;
    }
}
