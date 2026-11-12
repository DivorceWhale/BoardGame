using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The start menu lives in its own scene, Menu.unity (QUAKER > Play > Create Menu Scene), which is a copy of
// Play.unity with Menu Scene ticked. Play loads Play.unity; the Back button on Play.unity's setup screen loads
// Menu.unity again.
// The start menu: a see-through panel on the left (title, Play, How to Play, Cards, Quit) over a live demo.
// Behind it, computer builders drop blocks onto the real tower base one at a time with the game's own
// physics and placement rules; the base shakes every few blocks, and when the tower collapses or gets
// tall it is cleared and built again. The camera slowly circles the tower.
public partial class QuakerPlay
{
    [Header("Start menu")]
    [Tooltip("Ticked in Menu.unity: open on the start menu. Unticked in Play.unity: open on player setup.")]
    public bool menuScene;
    [Tooltip("Scene that Play loads (must be in Build Settings).")]
    public string gameSceneName = "Play";
    [Tooltip("Scene that setup's Back button loads (must be in Build Settings).")]
    public string menuSceneName = "Menu";
    [Tooltip("Degrees per second the camera circles the demo tower.")]
    public float menuOrbitSpeed = 7f;
    [Tooltip("Blocks the demo builds before it clears the tower and starts again.")]
    public int menuDemoBlocks = 16;

    [SerializeField, HideInInspector] GameObject startMenu;
    [SerializeField, HideInInspector] Button menuPlay, menuHowTo, menuCards, menuQuit, setupBack;

    bool demoRunning;
    Coroutine demoRoutine;

    // The in-game HUD, hidden on the start menu and the setup screen.
    static readonly string[] HudNames =
    {
        "TopBar", "PlayersPanel", "Supply", "Actions", "EventDeckPanel", "CurrentEventLabel", "CurrentEventCard",
        "DrawEventButton", "BlockDeckPanel", "CardsButton", "HelpButton"
    };

    // Editor: turns a copy of Play.unity into the menu scene. Deletes everything only a match needs (the HUD,
    // card panels, supply and action bars, setup, game result and match over screens) and forgets them.
    // Keeps the start menu, How to Play (Help), Cards, the card zoom and their C / H keys.
    public void StripForMenu()
    {
        if (canvasRoot == null) return;
        List<string> remove = new List<string>(HudNames) { "Setup", "GameResult", "MatchOver" };
        foreach (string n in remove)
        {
            Transform t = canvasRoot.transform.Find(n);
            if (t != null) DestroyImmediate(t.gameObject);
        }
        header = prompt = playersText = overText = null;
        eventSlot = blockSlot = null;
        blockCurrent = blockDrawPile = blockDiscardPile = eventLabel = null;
        drawEventButton = drawBlockButton = null;
        styleArea = actionArea = null;
        setupPanel = overPanel = gameOverPanel = null;
        gameOverText = null;
        startButton = nextButton = againButton = setupBack = null;
        nameFields.Clear(); nameRows.Clear(); countButtons.Clear(); downButtons.Clear(); upButtons.Clear();
        menuScene = true;
    }

    // Editor: remakes the Help menu's tabs from Rules_v2.txt (one tab per "## " heading) without touching
    // anything else, for when categories are added or removed. Returns the number of tabs.
    public int RebuildRulesTabs()
    {
        Transform menu = canvasRoot != null ? canvasRoot.transform.Find("HelpPanel/RulesSheet/RulesMenu") : null;
        if (menu == null) return 0;
        for (int i = menu.childCount - 1; i >= 0; i--)
            if (menu.GetChild(i).name.StartsWith("Tab ")) DestroyImmediate(menu.GetChild(i).gameObject);
        rulesTabs.Clear();
        List<KeyValuePair<string, string>> sections = RuleSections();
        float top = -100f, step = Mathf.Min(68f, 800f / Mathf.Max(1, sections.Count));
        for (int i = 0; i < sections.Count; i++)
        {
            Button tab = B6Button(menu, "Tab " + sections[i].Key, sections[i].Key, new Vector2(0.5f, 1), new Vector2(0, top - i * step), new Vector2(300, step - 10));
            tab.GetComponentInChildren<TMP_Text>().fontSize = 22;
            rulesTabs.Add(tab);
        }
        if (sections.Count > 0 && rulesHeading != null && rulesBody != null)
        {
            rulesHeading.text = sections[0].Key;
            rulesBody.text = sections[0].Value;
        }
        return sections.Count;
    }

    // ---------------- Building the menu (Editor and Play) ----------------

    // Editor: adds the start menu to the saved UI without rebuilding anything else.
    public void AddStartMenuToScene()
    {
        if (canvasRoot == null) return;
        Transform old = canvasRoot.transform.Find("StartMenu");
        if (old != null) DestroyImmediate(old.gameObject);
        if (setupPanel != null)
        {
            Transform back = setupPanel.transform.Find("BackToMenu");
            if (back != null) DestroyImmediate(back.gameObject);
        }
        BuildStartMenu(canvasRoot.transform);
    }

    void BuildStartMenu(Transform root)
    {
        GameObject menu = new GameObject("StartMenu", typeof(RectTransform));
        menu.transform.SetParent(root, false);
        B6Stretch((RectTransform)menu.transform);
        startMenu = menu;

        // A soft dark column on the left; the rest of the screen stays clear so the tower shows.
        GameObject column = new GameObject("Column", typeof(RectTransform));
        column.transform.SetParent(menu.transform, false);
        RectTransform col = (RectTransform)column.transform;
        col.anchorMin = new Vector2(0, 0); col.anchorMax = new Vector2(0, 1);
        col.pivot = new Vector2(0, 0.5f);
        col.offsetMin = new Vector2(0, 0); col.offsetMax = new Vector2(640, 0);
        column.AddComponent<Image>().color = new Color(0.03f, 0.04f, 0.06f, 0.62f);
        // Fade the column's right edge into the scene
        GameObject fade = new GameObject("Fade", typeof(RectTransform));
        fade.transform.SetParent(menu.transform, false);
        RectTransform fr = (RectTransform)fade.transform;
        fr.anchorMin = new Vector2(0, 0); fr.anchorMax = new Vector2(0, 1);
        fr.pivot = new Vector2(0, 0.5f);
        fr.offsetMin = new Vector2(640, 0); fr.offsetMax = new Vector2(760, 0);
        Image fi = fade.AddComponent<Image>();
        fi.color = new Color(0.03f, 0.04f, 0.06f, 0.62f);
        fi.sprite = FadeSprite();

        Transform c = column.transform;
        TMP_Text title = B6Text(c, "Title", "QUAKER", 104, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.88f, 0.42f, 0.13f));
        B6PlaceLeft(title.rectTransform, new Vector2(70, 300), new Vector2(560, 140));
        title.characterSpacing = 2;
        title.textWrappingMode = TextWrappingModes.NoWrap;
        TMP_Text sub = B6Text(c, "Subtitle", "One Shared Tower", 40, FontStyles.Bold, TextAlignmentOptions.Left, Color.white);
        B6PlaceLeft(sub.rectTransform, new Vector2(74, 210), new Vector2(540, 56));
        TMP_Text tag = B6Text(c, "Tagline", "Build it together. Survive the shake.\nDon't place the last block before it falls.", 22,
            FontStyles.Italic, TextAlignmentOptions.TopLeft, new Color(1f, 1f, 1f, 0.75f));
        B6PlaceLeft(tag.rectTransform, new Vector2(76, 140), new Vector2(520, 70));

        menuPlay = MenuButton(c, "PlayButton", "Play", 30, 0);
        menuHowTo = MenuButton(c, "HowToPlayButton", "How to Play", 26, -95);
        menuCards = MenuButton(c, "CardsMenuButton", "Cards", 26, -180);
        menuQuit = MenuButton(c, "QuitButton", "Quit", 26, -265);
        ColorBlock pc = menuPlay.colors;
        pc.normalColor = new Color(1f, 0.82f, 0.35f);
        pc.selectedColor = pc.normalColor;
        menuPlay.colors = pc;

        TMP_Text credit = B6Text(c, "Credit", "2–4 players  ·  Team C", 18, FontStyles.Normal, TextAlignmentOptions.Left, new Color(1f, 1f, 1f, 0.55f));
        B6PlaceLeft(credit.rectTransform, new Vector2(76, -470), new Vector2(520, 30));

        // Sit under the Cards and Help panels, so How to Play and Cards open on top of the menu.
        Transform cards = root.Find("CardsPanel");
        if (cards != null) menu.transform.SetSiblingIndex(cards.GetSiblingIndex());

        // A way back from the setup screen
        if (setupPanel != null)
        {
            setupBack = B6Button(setupPanel.transform, "BackToMenu", "Back", new Vector2(0, 1), new Vector2(110, -60), new Vector2(160, 56));
        }
        menu.SetActive(false);
    }

    Button MenuButton(Transform parent, string name, string label, float size, float y)
    {
        Button b = B6Button(parent, name, label, new Vector2(0, 0.5f), Vector2.zero, new Vector2(340, 74));
        B6PlaceLeft((RectTransform)b.transform, new Vector2(76, y), new Vector2(340, 74));
        b.GetComponentInChildren<TMP_Text>().fontSize = size;
        return b;
    }

    // Left-anchored in the column, positioned by its left edge.
    static void B6PlaceLeft(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    // A small left-to-right fade, used to soften the edge of the menu column.
    static Sprite FadeSprite()
    {
        Texture2D t = new Texture2D(64, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "MenuFade" };
        for (int x = 0; x < 64; x++) t.SetPixel(x, 0, new Color(1, 1, 1, 1f - x / 63f));
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, 64, 1), new Vector2(0.5f, 0.5f));
    }

    // ---------------- Play ----------------

    void WireStartMenu()
    {
        if (menuPlay != null) menuPlay.onClick.AddListener(() => { if (menuScene) LoadScene(gameSceneName); else ShowSetup(); });
        if (menuHowTo != null) menuHowTo.onClick.AddListener(() => TogglePanel("HelpToggle"));
        if (menuScene) AddInGameRulesNote();
        if (menuCards != null) menuCards.onClick.AddListener(() => TogglePanel("CardsToggle"));
        if (menuQuit != null) menuQuit.onClick.AddListener(QuitGame);
        if (setupBack != null) setupBack.onClick.AddListener(() => { if (menuScene) ShowStartMenu(); else LoadScene(menuSceneName); });
        // The faded column edge is a runtime texture, so make a fresh one each Play.
        if (startMenu != null)
        {
            Transform fade = startMenu.transform.Find("Fade");
            if (fade != null) fade.GetComponent<Image>().sprite = FadeSprite();
        }
    }

    void LoadScene(string sceneName)
    {
        StopDemo();
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    void TogglePanel(string toggleName)
    {
        Transform t = canvasRoot != null ? canvasRoot.transform.Find(toggleName) : null;
        HelpPanel h = t != null ? t.GetComponent<HelpPanel>() : null;
        if (h != null) h.Toggle();
    }

    static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void ShowStartMenu()
    {
        if (setupPanel != null) setupPanel.SetActive(false);
        if (overPanel != null) overPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        SetHud(false);
        if (startMenu != null) startMenu.SetActive(true);
        pitch = 32f;
        distance = 46f;
        StartDemo();
    }

    void SetHud(bool on)
    {
        if (canvasRoot == null) return;
        foreach (string n in HudNames)
        {
            Transform t = canvasRoot.transform.Find(n);
            if (t == null) continue;
            if (n == "CurrentEventCard" && on) continue;   // the card panel shows it once an event is drawn
            // The supply bar is only for the opening placement, and Refresh shows it then. The action bar (start the
            // shake, redraw, use the other card, pick a rival) must always be on during a match; Refresh fills it.
            if (n == "Supply") { if (!on) t.gameObject.SetActive(false); continue; }
            t.gameObject.SetActive(on);
        }
        if (on) { pitch = 50f; distance = 38f; }
    }

    void StartDemo()
    {
        if (demoRunning) return;
        demoRunning = true;
        demoRoutine = StartCoroutine(DemoLoop());
    }

    void StopDemo()
    {
        if (!demoRunning) return;
        demoRunning = false;
        if (demoRoutine != null) StopCoroutine(demoRoutine);
        demoRoutine = null;
        shaking = false;
        shakeSpeed = 1f;
        shakeExtra = 0f;
        ClearBlocks();
    }

    // The demo has no use for fallen blocks: remove them instead of lining them up on the table.
    void ClearSetAside()
    {
        foreach (GameObject g in setAside) if (g != null) Destroy(g);
        setAside.Clear();
    }

    IEnumerator DemoLoop()
    {
        System.Random rnd = new System.Random();
        int[] fell = new int[3];
        Bot[] builders = { Bot.Showcase };
        while (demoRunning)
        {
            ClearBlocks();
            yield return new WaitForSeconds(0.6f);
            int count = 0;
            bool collapsed = false;
            while (demoRunning && count < menuDemoBlocks && !collapsed)
            {
                CardData style = blockStyles[rnd.Next(blockStyles.Count)];
                yield return BotPlace(style, builders[count % builders.Length], rnd, fell, 9f);
                ClearSetAside();
                count++;
                collapsed = IsCollapse(fell[1], fell[2]);
                if (collapsed) break;
                yield return new WaitForSeconds(0.5f);
                if (count % 4 == 0)
                {
                    yield return TestShake(1, 1f, fell);   // a normal shake, like the end of a round
                    ClearSetAside();
                    collapsed = IsCollapse(fell[1], fell[2]);
                    yield return new WaitForSeconds(0.6f);
                }
            }
            yield return new WaitForSeconds(collapsed ? 2.5f : 3.5f);
        }
    }
}
