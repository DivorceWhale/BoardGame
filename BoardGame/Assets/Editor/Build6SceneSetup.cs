using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Adds the Build 6 UI to the open scene: all twenty event cards (C), the player aid (H),
// and a Draw Event button that shows the drawn event through the CardUI prefab.
// Run from: QUAKER > Build 6 > Set Up Build-6 Scene. Running it again rebuilds the UI.
public static class Build6SceneSetup
{
    const string RootName = "Build6UI";
    const string CardPrefabPath = "Assets/Prefabs/Cards/CardUI.prefab";
    const string PlayerAidPath = "Assets/Art/PlayerAid.txt";
    const string EventFolder = "Assets/Prefabs/Cards/Event/";

    [MenuItem("QUAKER/Build 6/Set Up Build-6 Scene")]
    public static void SetUp()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.name.Contains("Build-6"))
        {
            if (!EditorUtility.DisplayDialog("Build 6", "The open scene is \"" + scene.name +
                "\", not Build-6. Add the Build 6 UI here anyway?", "Yes", "Cancel")) return;
        }

        CardView cardPrefab = AssetDatabase.LoadAssetAtPath<CardView>(CardPrefabPath);
        if (cardPrefab == null)
        {
            Debug.LogError("Build 6: run QUAKER > Build 6 > Create Card UI Prefab first.");
            return;
        }

        GameObject old = GameObject.Find(RootName);
        if (old != null) Object.DestroyImmediate(old);

        EnsureEventSystem();

        // Overlay canvas, 1920 x 1080
        GameObject root = new GameObject(RootName, typeof(RectTransform));
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();

        // Top-right buttons
        Button cardsButton = MakeButton(root.transform, "CardsButton", "Cards (C)", new Vector2(1, 1), new Vector2(-110, -50));
        Button helpButton = MakeButton(root.transform, "HelpButton", "Help (H)", new Vector2(1, 1), new Vector2(-110, -120));

        // Current event: one CardUI instance fed by the event deck
        TurnDriver driver = Object.FindFirstObjectByType<TurnDriver>();
        DeckManager eventDeck = driver != null ? driver.eventDeck : null;

        MakeBox(root.transform, "EventDeckPanel", new Vector2(0, 0.5f), new Vector2(150, 10), new Vector2(290, 480));

        TMP_Text slotLabel = MakeText(root.transform, "CurrentEventLabel", "Current Event", 26, FontStyles.Bold,
            TextAlignmentOptions.Center, Color.white);
        Place(slotLabel.rectTransform, new Vector2(0, 0.5f), new Vector2(150, 200), new Vector2(260, 40));

        GameObject slotGo = (GameObject)PrefabUtility.InstantiatePrefab(cardPrefab.gameObject, root.transform);
        CardView slot = slotGo.GetComponent<CardView>();
        slot.name = "CurrentEventCard";
        RectTransform slotRect = (RectTransform)slot.transform;
        Place(slotRect, new Vector2(0, 0.5f), new Vector2(150, 20), slotRect.sizeDelta);
        slotRect.localScale = Vector3.one * 0.6f;
        slot.gameObject.SetActive(false);

        Button drawEventButton = MakeButton(root.transform, "DrawEventButton", "Draw Event", new Vector2(0, 0.5f), new Vector2(150, -160));
        if (eventDeck != null)
        {
            Undo.RecordObject(eventDeck, "Build 6 event display");
            eventDeck.uiDisplay = slot;
            eventDeck.discardDelay = Mathf.Max(eventDeck.discardDelay, 5f);
            // Fallback deck when no scenario configures it: all twenty event cards
            eventDeck.cards = new List<CardData>();
            for (int n = 1; n <= 20; n++)
            {
                CardData c = AssetDatabase.LoadAssetAtPath<CardData>(EventFolder + "EC" + n + ".asset");
                if (c != null) eventDeck.cards.Add(c);
            }
            eventDeck.expectedDeckSize = 20;
            EditorUtility.SetDirty(eventDeck);
            UnityEventTools.AddPersistentListener(drawEventButton.onClick, eventDeck.DrawCard);
        }
        else
        {
            Debug.LogWarning("Build 6: no TurnDriver with an Event Deck found, so Draw Event is not wired.");
        }

        // Block deck panel: replaces the old world-space Draw Card UI with the same style
        DeckManager blockDeck = driver != null ? driver.blockDeck : null;
        GameObject blockPanel = MakeBox(root.transform, "BlockDeckPanel", new Vector2(1, 0.5f), new Vector2(-160, 10), new Vector2(290, 600));
        Transform bp = blockPanel.transform;

        TMP_Text blockHeader = MakeText(bp, "Header", "Current Block", 26, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        Place(blockHeader.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 265), new Vector2(260, 40));

        GameObject blockSlotGo = (GameObject)PrefabUtility.InstantiatePrefab(cardPrefab.gameObject, bp);
        blockSlotGo.name = "CurrentBlockCard";
        CardView blockSlot = blockSlotGo.GetComponent<CardView>();
        RectTransform blockSlotRect = (RectTransform)blockSlotGo.transform;
        Place(blockSlotRect, new Vector2(0.5f, 0.5f), new Vector2(0, 85), blockSlotRect.sizeDelta);
        blockSlotRect.localScale = Vector3.one * 0.6f;
        blockSlotGo.SetActive(false);

        TMP_Text blockCurrent = MakeText(bp, "CurrentBlock", "Drawn Card: None", 22, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.82f, 0.35f));
        Place(blockCurrent.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -95), new Vector2(260, 36));
        TMP_Text blockDraw = MakeText(bp, "DrawPile", "Draw Pile: 0", 20, FontStyles.Normal, TextAlignmentOptions.Center, Color.white);
        Place(blockDraw.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -135), new Vector2(260, 28));
        TMP_Text blockDiscard = MakeText(bp, "DiscardPile", "Discard Pile: 0", 20, FontStyles.Normal, TextAlignmentOptions.Center, Color.white);
        Place(blockDiscard.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -168), new Vector2(260, 28));
        Button drawBlockButton = MakeButton(bp, "DrawBlockButton", "Draw Block", new Vector2(0.5f, 0.5f), new Vector2(0, -245), new Vector2(200, 60));

        if (blockDeck != null)
        {
            Undo.RecordObject(blockDeck, "Build 6 block deck UI");
            blockDeck.drawnCardText = blockCurrent;
            blockDeck.drawPileText = blockDraw;
            blockDeck.discardPileText = blockDiscard;
            blockDeck.uiDisplay = blockSlot;
            blockDeck.discardDelay = Mathf.Max(blockDeck.discardDelay, 5f);
            EditorUtility.SetDirty(blockDeck);
            UnityEventTools.AddPersistentListener(drawBlockButton.onClick, blockDeck.DrawCard);
        }
        else
        {
            Debug.LogWarning("Build 6: no TurnDriver with a Block Deck found, so Draw Block is not wired.");
        }

        // Hide the old world-space Draw Card canvas (its texts are now in the panel above)
        GameObject oldDraw = GameObject.Find("DrawButton");
        if (oldDraw != null)
        {
            Canvas oldCanvas = oldDraw.GetComponentInParent<Canvas>();
            if (oldCanvas != null && oldCanvas.gameObject != root)
            {
                Undo.RecordObject(oldCanvas.gameObject, "Hide old draw UI");
                oldCanvas.gameObject.SetActive(false);
            }
        }

        // Cards panel: all twenty event cards through the one prefab, 4 across, scrolling
        GameObject cardsPanel = MakePanel(root.transform, "CardsPanel");

        GameObject scrollGo = new GameObject("CardScroll", typeof(RectTransform));
        scrollGo.transform.SetParent(cardsPanel.transform, false);
        Stretch((RectTransform)scrollGo.transform);
        ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 60;

        GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform));
        viewportGo.transform.SetParent(scrollGo.transform, false);
        RectTransform viewport = (RectTransform)viewportGo.transform;
        Stretch(viewport);
        viewportGo.AddComponent<Image>().color = new Color(0, 0, 0, 0);   // catches the mouse wheel between cards
        viewportGo.AddComponent<RectMask2D>();

        RectTransform grid = new GameObject("CardGrid", typeof(RectTransform)).GetComponent<RectTransform>();
        grid.SetParent(viewport, false);
        grid.anchorMin = new Vector2(0, 1);
        grid.anchorMax = new Vector2(1, 1);
        grid.pivot = new Vector2(0.5f, 1);
        grid.anchoredPosition = Vector2.zero;
        grid.sizeDelta = Vector2.zero;
        GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = ((RectTransform)cardPrefab.transform).sizeDelta;   // same size as the prefab
        layout.spacing = new Vector2(24, 24);
        layout.padding = new RectOffset(0, 0, 40, 40);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 4;
        ContentSizeFitter fitter = grid.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport;
        scroll.content = grid;

        CardGallery gallery = cardsPanel.AddComponent<CardGallery>();
        gallery.cardPrefab = cardPrefab;
        gallery.grid = grid;
        gallery.cards = new List<CardData>();
        for (int n = 1; n <= 20; n++)
        {
            CardData c = AssetDatabase.LoadAssetAtPath<CardData>(EventFolder + "EC" + n + ".asset");
            if (c != null) gallery.cards.Add(c); else Debug.LogWarning("Build 6: missing EC" + n + ".asset");
        }
        foreach (string b in new[] { "The Cube", "Long Block", "L-Block", "BL-Block", "T-Block", "Z-Block" })
        {
            CardData c = AssetDatabase.LoadAssetAtPath<CardData>("Assets/Prefabs/Cards/Blocks/" + b + ".asset");
            if (c != null) gallery.cards.Add(c); else Debug.LogWarning("Build 6: missing block card " + b);
        }
        Button cardsClose = MakeButton(cardsPanel.transform, "CloseButton", "X", new Vector2(1, 1), new Vector2(-60, -50), new Vector2(70, 60));

        // Help panel: the player aid, verbatim
        GameObject helpPanel = MakePanel(root.transform, "HelpPanel");
        GameObject sheet = new GameObject("AidSheet", typeof(RectTransform));
        sheet.transform.SetParent(helpPanel.transform, false);
        Place((RectTransform)sheet.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1150, 960));
        sheet.AddComponent<Image>().color = Color.white;
        TMP_Text aidText = MakeText(sheet.transform, "AidText", "", 26, FontStyles.Normal, TextAlignmentOptions.TopLeft, Color.black);
        Stretch(aidText.rectTransform);
        aidText.rectTransform.offsetMin = new Vector2(40, 40);
        aidText.rectTransform.offsetMax = new Vector2(-40, -40);
        Button helpClose = MakeButton(sheet.transform, "CloseButton", "X", new Vector2(1, 1), new Vector2(-50, -45), new Vector2(70, 60));

        // Card zoom: click any card to see it large in the middle of the screen
        GameObject zoomGo = new GameObject("CardZoom", typeof(RectTransform));
        zoomGo.transform.SetParent(root.transform, false);
        Stretch((RectTransform)zoomGo.transform);
        CardZoom zoom = zoomGo.AddComponent<CardZoom>();
        GameObject zoomOverlay = MakePanel(zoomGo.transform, "ZoomOverlay");
        Button zoomClose = zoomOverlay.AddComponent<Button>();
        zoomClose.targetGraphic = zoomOverlay.GetComponent<Image>();
        zoomClose.transition = Selectable.Transition.None;
        GameObject bigGo = (GameObject)PrefabUtility.InstantiatePrefab(cardPrefab.gameObject, zoomOverlay.transform);
        bigGo.name = "ZoomedCard";
        RectTransform bigRect = (RectTransform)bigGo.transform;
        Place(bigRect, new Vector2(0.5f, 0.5f), new Vector2(0, 25), bigRect.sizeDelta);
        bigRect.localScale = Vector3.one * 1.8f;
        TMP_Text zoomHint = MakeText(zoomOverlay.transform, "Hint", "Click anywhere or press Esc to close", 22,
            FontStyles.Italic, TextAlignmentOptions.Center, new Color(1, 1, 1, 0.8f));
        Place(zoomHint.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(800, 40));
        zoom.overlay = zoomOverlay;
        zoom.bigCard = bigGo.GetComponent<CardView>();
        zoom.closeArea = zoomClose;
        zoomOverlay.SetActive(false);

        // Toggles (kept on always-active objects so the keys work while a panel is closed)
        HelpPanel cardsToggle = new GameObject("CardsToggle").AddComponent<HelpPanel>();
        cardsToggle.transform.SetParent(root.transform, false);
        cardsToggle.panel = cardsPanel;
        cardsToggle.openButton = cardsButton;
        cardsToggle.closeButton = cardsClose;
        cardsToggle.toggleKey = Key.C;

        HelpPanel helpToggle = new GameObject("HelpToggle").AddComponent<HelpPanel>();
        helpToggle.transform.SetParent(root.transform, false);
        helpToggle.panel = helpPanel;
        helpToggle.aidText = aidText;
        helpToggle.playerAid = AssetDatabase.LoadAssetAtPath<TextAsset>(PlayerAidPath);
        helpToggle.openButton = helpButton;
        helpToggle.closeButton = helpClose;
        helpToggle.toggleKey = Key.H;

        cardsPanel.SetActive(false);
        helpPanel.SetActive(false);

        AddSceneToBuild(scene.path);
        Undo.RegisterCreatedObjectUndo(root, "Build 6 UI");
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = root;
        Debug.Log("Build 6: UI added to " + scene.name + ". Save the scene (Ctrl+S).");
    }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    static void AddSceneToBuild(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in scenes) if (s.path == path) return;
        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    static Sprite Rounded()
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

    static GameObject MakeBox(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Place((RectTransform)go.transform, anchor, pos, size);
        Image img = go.AddComponent<Image>();
        img.sprite = Rounded();
        img.type = Image.Type.Sliced;
        img.color = new Color(0.10f, 0.11f, 0.14f, 0.88f);
        return go;
    }

    static void Height(GameObject go, float h)
    {
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = h;
        le.minHeight = h;
    }

    static GameObject MakePanel(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Stretch((RectTransform)go.transform);
        go.AddComponent<Image>().color = new Color(0, 0, 0, 0.8f);
        return go;
    }

    static Button MakeButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, Vector2? size = null)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Place((RectTransform)go.transform, anchor, pos, size ?? new Vector2(180, 56));
        Image img = go.AddComponent<Image>();
        img.sprite = Rounded();
        img.type = Image.Type.Sliced;
        img.color = Color.white;
        Button b = go.AddComponent<Button>();
        b.targetGraphic = img;
        ColorBlock cb = b.colors;
        cb.normalColor = new Color(0.96f, 0.96f, 0.96f);
        cb.highlightedColor = new Color(1f, 0.85f, 0.45f);
        cb.pressedColor = new Color(0.85f, 0.65f, 0.25f);
        b.colors = cb;
        TMP_Text t = MakeText(go.transform, "Label", label, 24, FontStyles.Bold, TextAlignmentOptions.Center, Color.black);
        Stretch(t.rectTransform);
        return b;
    }

    static TMP_Text MakeText(Transform parent, string name, string text, float size, FontStyles style,
        TextAlignmentOptions align, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = align;
        t.color = color;
        t.enableAutoSizing = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;
        return t;
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
