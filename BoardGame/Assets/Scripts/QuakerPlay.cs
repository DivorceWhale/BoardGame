using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Play scene: Quaker Rulesheet v2 (one shared tower) as a 3D physics game, hotseat for 2-4 players.
// The Tower Base model sits on a shake board. Players aim their block with the mouse, rotate it and drop it;
// the shake board really shakes, and the tower has fallen when no block is left on the tower base.
// Every action still goes through TurnStateMachine.Play -> RuleCheck, so only legal moves happen.
public partial class QuakerPlay : MonoBehaviour
{
    [Header("Components (set by QUAKER > Play > Create Play Scene)")]
    public GameObject towerBasePrefab;                          // Prefabs/Tower Base.prefab (16 BoardTile tiles)
    public BoardLayout towerBase;                               // the 16 spaces and their zones
    public List<CardData> blockStyles = new List<CardData>();   // the 6 block styles; 5 cards and 5 blocks each
    public List<GameObject> blockPrefabs = new List<GameObject>(); // same order as blockStyles
    public List<CardData> eventCards = new List<CardData>();    // event cards 1-20

    [Header("Build 6 card UI")]
    public CardView cardPrefab;      // Prefabs/Cards/CardUI.prefab
    public TextAsset rulesText;      // Art/Rules_v2.txt: one category per "## Heading"
    public Sprite roundedSprite;     // the built-in UISprite, like the Build 6 buttons

    [Header("Balance (Build 8): every shared balance number is on this asset")]
    [Tooltip("Assets/Resources/QuakerBalance.asset. Edit values there, not in code.")]
    public BalanceConfig balance;
    static BalanceConfig B { get { return BalanceConfig.Current; } }
    float shakeSeconds { get { return B.shakeSeconds; } }
    float shakeStrength { get { return B.shakeStrength; } }
    float shakeHz { get { return B.shakeHz; } }
    float shakeRamp { get { return B.shakeRamp; } }
    float shakeOnlyStep { get { return B.shakeOnlyStep; } }
    float shakeMaxStrength { get { return B.shakeMaxStrength; } }
    float gravityScale { get { return B.gravityScale; } }
    float blockMass { get { return B.blockMass; } }
    float settleTimeout { get { return B.settleTimeout; } }
    float collapseShare { get { return rules != null && rules.collapseShare > 0f ? rules.collapseShare : B.collapseShare; } }   // Thin Margins tightens it
    int collapseMinBlocks { get { return B.collapseMinBlocks; } }

    [Header("Scene objects (QUAKER > Play > Rebuild Scene Objects; built at Play if empty)")]
    public Rigidbody shakeBoard;          // kinematic platform; the tower base is its child
    public GameObject towerBaseModel;     // the Tower Base instance on the shake board
    public BoxCollider baseSlab;          // solid collider under the 16 tiles
    public Collider table;                // fallen blocks land here and are set aside on it
    public PhysicsMaterial grip;          // friction for the base, the table and the blocks
    public Material tableMaterial;

    // Game
    TurnStateMachine machine;
    GameState s;
    ScenarioConfig rules;
    readonly List<string> log = new List<string>();
    CardData openingStyle;
    string notice = "";
    bool busy;                 // a block is settling, or the board is shaking
    bool shaking;
    float shakeEndsAt;
    float shakeStartedAt;
    float shakeExtra;              // added strength for this shake (shake-only rounds)
    int shakeOnlyGame = -1, shakeOnlyRounds;   // shake-only rounds so far in that game
    string lastCollapse = "";
    float shakeSpeed = 1f;
    int shakeNumber, shakeCount;

    // Event 14 ten-count
    string tenCountTurn = "";
    float tenCountEndsAt = -1f;
    bool towerHidden;

    // World
    float tableTop;            // y of the table surface
    Vector3 boardHome;
    readonly Dictionary<int, Vector3> tileCentres = new Dictionary<int, Vector3>();
    float cell = 4f;           // distance between tile centres
    float baseTop;             // y of the top of the tiles
    Vector3 baseCentre;
    Vector3 gridMin, gridMax;  // outer corners of the 16 tiles
    readonly List<Rigidbody> placed = new List<Rigidbody>();
    Vector3 originalGravity;

    // Aiming
    GameObject ghost;
    CardData ghostStyle;
    Quaternion ghostTurn = Quaternion.identity;
    bool levelLower, levelHigher;   // Q / E this frame
    int levelFromTop;               // 0 = on top of the tower; 1+ = tucked under an overhang
    string levelAimKey = "";
    GameAction aimed;
    bool aimValid;
    RuleResult aimRule;

    // Camera
    float yaw = 35f, pitch = 50f, distance = 38f;

    // UI
    // UI: built into Play.unity by QUAKER > Play > Rebuild Scene Objects (or at Play if missing).
    // The references are saved with the scene; the buttons are wired up in WireUI at Play.
    [SerializeField, HideInInspector] GameObject canvasRoot;
    [SerializeField, HideInInspector] TMP_Text header, prompt, playersText, overText;

    // Build 6 card panels
    [SerializeField, HideInInspector] CardView eventSlot, blockSlot;
    CardData shownEvent, shownBlock;
    [SerializeField, HideInInspector] TMP_Text blockCurrent, blockDrawPile, blockDiscardPile, eventLabel;
    [SerializeField, HideInInspector] Button drawEventButton, drawBlockButton;
    int lastDrawer = -1;
    // The block panel shows a card the moment it is drawn, for this long, before it follows the next player.
    const float RevealSeconds = 3f;
    int coffeeDrawn;   // block cards drawn and discarded for the players on this Coffee Break
    int revealPlayer = -1;
    float revealUntil;
    [SerializeField, HideInInspector] RectTransform styleArea, actionArea;
    [SerializeField, HideInInspector] GameObject setupPanel, overPanel;
    [SerializeField, HideInInspector] Button startButton, nextButton, againButton;
    [SerializeField, HideInInspector] List<Button> upButtons = new List<Button>();
    [SerializeField, HideInInspector] List<Button> rulesTabs = new List<Button>();
    [SerializeField, HideInInspector] TMP_Text rulesHeading, rulesBody;
    [SerializeField, HideInInspector] RectTransform rulesContent;
    System.Func<GameObject, Transform, GameObject> uiSpawn;   // the Editor keeps card prefab links

    // Setup: player names in turn order
    int setupCount = 2;
    [SerializeField, HideInInspector] List<TMP_InputField> nameFields = new List<TMP_InputField>();
    [SerializeField, HideInInspector] List<GameObject> nameRows = new List<GameObject>();
    [SerializeField, HideInInspector] List<Button> countButtons = new List<Button>();
    [SerializeField, HideInInspector] List<Button> downButtons = new List<Button>();

    // Fallen blocks set aside beside the table (section 7), and the result of the last game
    readonly List<GameObject> setAside = new List<GameObject>();
    [SerializeField, HideInInspector] GameObject gameOverPanel;
    [SerializeField, HideInInspector] TMP_Text gameOverText;

    void Start()
    {
        if (balance != null) BalanceConfig.Current = balance;
        originalGravity = Physics.gravity;
        Physics.gravity = originalGravity * gravityScale;

        // Blocks, base and table are all on the Default layer. If Project Settings > Physics has
        // Default unticked against itself, every block falls through everything.
        if (Physics.GetIgnoreLayerCollision(0, 0))
        {
            Debug.LogWarning("[Quaker] Project Settings > Physics > Layer Collision Matrix has Default vs Default unticked, "
                + "so blocks would fall through the tower base. Turning it back on for this session; tick it in Project Settings to fix it for good.");
            Physics.IgnoreLayerCollision(0, 0, false);
        }
        BuildWorld();
        if (canvasRoot == null) BuildUI();
        if (menuScene && startMenu == null) BuildStartMenu(canvasRoot.transform);
        if (!menuScene) EnsureTurnUI();   // colour swatches, turn frame, banner, Ready box (QuakerPlay.Turns.cs)   // a menu scene saved without its menu
        if (menuScene) WireRules();   // the menu scene only has the menu, How to Play and Cards
        else { WireUI(); WireTurnUI(); }
        WireStartMenu();
        if (menuScene) ShowStartMenu();   // Menu.unity: the start menu over the self-building tower
        else ShowSetup();                 // Play.unity: straight to player setup
    }

    void OnDestroy()
    {
        Physics.gravity = originalGravity;
    }

    // ---------------- World ----------------

    void BuildWorld()
    {
        if (shakeBoard == null || towerBaseModel == null || baseSlab == null) CreateWorldObjects(null);
        ReadWorld();
    }

    // Builds the shake board, the Tower Base on it, its colliders and the table, as children of this object.
    // Runs in the Editor (QUAKER > Play > Rebuild Scene Objects) so they are saved in Play.unity and can be
    // moved and edited there; Play only builds them when the scene has none.
    // instantiate: how to create the Tower Base (the Editor passes PrefabUtility.InstantiatePrefab).
    public void CreateWorldObjects(System.Func<GameObject, Transform, GameObject> instantiate)
    {
        RemoveWorldObjects();
        if (grip == null) grip = new PhysicsMaterial("Grip") { staticFriction = 0.7f, dynamicFriction = 0.6f, bounciness = 0f };

        GameObject board = new GameObject("Shake Board");
        board.transform.SetParent(transform, false);
        shakeBoard = board.AddComponent<Rigidbody>();
        shakeBoard.isKinematic = true;
        shakeBoard.interpolation = RigidbodyInterpolation.Interpolate;

        GameObject tb;
        if (towerBasePrefab == null) tb = new GameObject("Tower Base (missing)");
        else if (instantiate != null) tb = instantiate(towerBasePrefab, board.transform);
        else tb = Instantiate(towerBasePrefab, board.transform);
        tb.name = "Tower Base";
        towerBaseModel = tb;
        baseSlab = SolidifyBase(tb, board.transform);

        Bounds all = TileBounds(tb);
        GameObject t = GameObject.CreatePrimitive(PrimitiveType.Cube);
        t.name = "Table";
        t.transform.SetParent(transform, false);
        t.transform.position = new Vector3(all.center.x, all.max.y - 6.5f, all.center.z);
        t.transform.localScale = new Vector3(120f, 1f, 120f);
        table = t.GetComponent<Collider>();
        table.sharedMaterial = grip;
        Renderer tr = t.GetComponent<Renderer>();
        if (tableMaterial != null) tr.sharedMaterial = tableMaterial;
        else if (Application.isPlaying) SetColour(tr, new Color(0.35f, 0.27f, 0.2f));
    }

    public void RemoveWorldObjects()
    {
        foreach (string n in new[] { "Shake Board", "Table" })
        {
            Transform old = transform.Find(n);
            while (old != null)
            {
                old.SetParent(null);
                if (Application.isPlaying) Destroy(old.gameObject); else DestroyImmediate(old.gameObject);
                old = transform.Find(n);
            }
        }
        shakeBoard = null;
        towerBaseModel = null;
        baseSlab = null;
        table = null;
    }

    // Measures the scene objects: where the 16 spaces are, the top of the base, the table surface.
    void ReadWorld()
    {
        tileCentres.Clear();
        foreach (BoardTile tile in towerBaseModel.GetComponentsInChildren<BoardTile>())
        {
            Renderer r = tile.GetComponentInChildren<Renderer>();
            Bounds b = r != null ? r.bounds : new Bounds(tile.transform.position, Vector3.one);
            tileCentres[tile.spaceId] = new Vector3(b.center.x, b.max.y, b.center.z);
        }
        if (tileCentres.Count == 0) Debug.LogError("Play scene: the Tower Base has no BoardTile tiles.");
        Bounds all = TileBounds(towerBaseModel);
        baseTop = all.max.y;
        baseCentre = new Vector3(all.center.x, baseTop, all.center.z);
        gridMin = all.min;
        gridMax = all.max;
        if (tileCentres.ContainsKey(1) && tileCentres.ContainsKey(2)) cell = Vector3.Distance(tileCentres[1], tileCentres[2]);
        boardHome = shakeBoard.transform.position;
        tableTop = table != null ? table.bounds.max.y : baseTop - 6f;
    }

    static Bounds TileBounds(GameObject towerBase)
    {
        Bounds all = new Bounds(towerBase.transform.position, Vector3.zero);
        bool first = true;
        foreach (BoardTile tile in towerBase.GetComponentsInChildren<BoardTile>(true))
            foreach (Renderer r in tile.GetComponentsInChildren<Renderer>())
            {
                if (first) { all = r.bounds; first = false; } else all.Encapsulate(r.bounds);
            }
        return all;
    }

    // The Tower Base model's tiles are thin mesh colliders with a negative Y scale, which flips their faces,
    // so falling blocks pass straight through them. Switch every model collider off and use solid boxes:
    // the 16 tiles become one slab (top = top of the tiles, 2 units thick); the edge pieces get their own boxes.
    BoxCollider SolidifyBase(GameObject model, Transform board)
    {
        foreach (Collider old in model.GetComponentsInChildren<Collider>(true)) old.enabled = false;

        HashSet<Renderer> tileRenderers = new HashSet<Renderer>();
        foreach (BoardTile tile in model.GetComponentsInChildren<BoardTile>(true))
            foreach (Renderer r in tile.GetComponentsInChildren<Renderer>()) tileRenderers.Add(r);
        Bounds tiles = TileBounds(model);

        const float slabThickness = 2f;
        GameObject slab = new GameObject("Tower Base Slab (collider)");
        slab.transform.SetParent(board, false);
        slab.transform.position = new Vector3(tiles.center.x, tiles.max.y - slabThickness * 0.5f, tiles.center.z);
        BoxCollider slabBox = slab.AddComponent<BoxCollider>();
        slabBox.size = new Vector3(tiles.size.x, slabThickness, tiles.size.z);
        slabBox.sharedMaterial = grip;

        foreach (Renderer r in model.GetComponentsInChildren<Renderer>())
        {
            if (tileRenderers.Contains(r)) continue;
            Bounds b = r.bounds;
            float thickness = Mathf.Max(b.size.y, 1f);
            GameObject edge = new GameObject("Edge collider: " + r.name);
            edge.transform.SetParent(board, false);
            edge.transform.position = new Vector3(b.center.x, b.max.y - thickness * 0.5f, b.center.z);
            BoxCollider box = edge.AddComponent<BoxCollider>();
            box.size = new Vector3(b.size.x, thickness, b.size.z);
            box.sharedMaterial = grip;
        }
        return slabBox;
    }

    // Wraps a block prefab so its pivot is the centre of the block, and returns the wrapper.
    GameObject SpawnBlock(CardData style, bool forGhost)
    {
        int k = blockStyles.IndexOf(style);
        GameObject prefab = k >= 0 && k < blockPrefabs.Count ? blockPrefabs[k] : null;
        GameObject model = prefab != null ? Instantiate(prefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);

        Bounds b = RenderBounds(model);
        GameObject wrap = new GameObject((forGhost ? "Aim: " : "") + (style != null ? style.title : "Block"));
        if (!forGhost) wrap.AddComponent<BlockSound>();   // Build 9: knocks when it hits something
        wrap.transform.position = b.center;
        model.transform.SetParent(wrap.transform, true);

        // Don't trust the model's own colliders: replace them with one box per cube, sized from its mesh.
        foreach (Collider old in model.GetComponentsInChildren<Collider>(true)) DestroyImmediate(old);
        foreach (MeshFilter mf in model.GetComponentsInChildren<MeshFilter>())
        {
            if (forGhost) { mf.gameObject.layer = 2; continue; }   // Ignore Raycast, no colliders
            if (mf.sharedMesh == null) continue;
            BoxCollider bc = mf.gameObject.AddComponent<BoxCollider>();
            bc.center = mf.sharedMesh.bounds.center;
            bc.size = mf.sharedMesh.bounds.size;
        }
        wrap.layer = forGhost ? 2 : 0;
        return wrap;
    }

    static Bounds RenderBounds(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    // The demo block being lowered by hand. It has no physics yet, so it isn't in placed.
    GameObject lowering;

    void ClearBlocks()
    {
        if (lowering != null) Destroy(lowering);
        lowering = null;
        GameSounds.StopLoop();   // a shake cut short (new match, back to the menu) mustn't keep rumbling
        foreach (Rigidbody rb in placed) if (rb != null) Destroy(rb.gameObject);
        placed.Clear();
        foreach (GameObject g in setAside) if (g != null) Destroy(g);
        setAside.Clear();
        ResetTowerRecord();
        shakeBoard.transform.position = boardHome;
    }

    void FixedUpdate()
    {
        if (shakeBoard == null) return;
        if (shaking)
        {
            // The board moves side to side and front to back at slightly different rates, so it wanders in
            // a figure-of-eight. Each axis peaks at shakeStrength x gravity. Event 15 (double speed) runs the shake
            // twice as fast, which shakes twice as hard.
            float t = Time.time - shakeStartedAt;
            float wx = 2f * Mathf.PI * shakeHz * shakeSpeed, wz = wx * 0.8f;
            float peak = StrengthNow() * shakeSpeed * Physics.gravity.magnitude;   // target peak acceleration
            float ramp = shakeRamp > 0f ? Mathf.Clamp01(Mathf.Min(t, shakeEndsAt - Time.time) / shakeRamp) : 1f;
            ramp = ramp * ramp * (3f - 2f * ramp);                                  // smooth start and stop
            Vector3 offset = new Vector3(Mathf.Sin(t * wx) * peak / (wx * wx), 0f, Mathf.Sin(t * wz) * peak / (wz * wz)) * ramp;
            shakeBoard.MovePosition(boardHome + offset);
            foreach (Rigidbody rb in placed) if (rb != null) rb.WakeUp();
        }
        else if (shakeBoard.position != boardHome)
        {
            shakeBoard.MovePosition(boardHome);
        }
    }

    // A block is still on the tower when it has not dropped to the table (rulesheet: fallen = off the base).
    bool OnTower(Rigidbody rb)
    {
        return rb != null && RenderBounds(rb.gameObject).min.y > baseTop - 2f;
    }

    bool AnyMoving()
    {
        foreach (Rigidbody rb in placed)
            if (rb != null && (rb.linearVelocity.sqrMagnitude > 0.02f || rb.angularVelocity.sqrMagnitude > 0.02f)) return true;
        return false;
    }

    // Where every block on the tower is now (the underside of each), to compare with after it settles.
    // except: the block that was just dropped, which isn't part of the tower yet.
    Dictionary<Rigidbody, float> TowerSnapshot(Rigidbody except)
    {
        Dictionary<Rigidbody, float> d = new Dictionary<Rigidbody, float>();
        foreach (Rigidbody rb in placed)
            if (rb != null && rb != except) d[rb] = RenderBounds(rb.gameObject).min.y;
        return d;
    }

    // Blocks from the snapshot that came down: dropped half a block or more, or are off the base.
    int CountCameDown(Dictionary<Rigidbody, float> before)
    {
        int n = 0;
        foreach (KeyValuePair<Rigidbody, float> kv in before)
        {
            if (kv.Key == null || !OnTower(kv.Key)) { n++; continue; }
            if (RenderBounds(kv.Key.gameObject).min.y < kv.Value - cell * 0.5f) n++;
        }
        return n;
    }

    bool IsCollapse(int cameDown, int total)
    {
        return total > 0 && cameDown >= collapseMinBlocks && cameDown >= collapseShare * total - 0.001f;
    }

    // Wait for the blocks to settle, set aside anything that fell off the base, then let the rules decide:
    // the tower has fallen if it collapsed (see collapseShare) or nothing is left on the base.
    // before: the tower before the shake; null = before this placement (everything but the newest block).
    IEnumerator SettleThenCheck(bool roundCheck, Dictionary<Rigidbody, float> before = null)
    {
        busy = true;
        Refresh();
        if (before == null) before = TowerSnapshot(placed.Count > 0 ? placed[placed.Count - 1] : null);
        float t0 = Time.time;
        yield return new WaitForSeconds(0.6f);
        while (Time.time - t0 < settleTimeout && AnyMoving()) yield return null;

        int blocksBefore = placed.Count;
        int fallen = 0;
        for (int i = placed.Count - 1; i >= 0; i--)
        {
            if (OnTower(placed[i])) continue;
            fallen++;
            SetAside(placed[i]);
            placed.RemoveAt(i);
        }
        RecordTower(blocksBefore);
        int cameDown;
        bool collapsed = TowerCollapsed(out cameDown);   // half of this game's tallest tower (QuakerPlay.Collapse.cs)

        if (s.tower.Count > 0 && (collapsed || placed.Count == 0))
        {
            lastCollapse = collapsed ? cameDown + " of the " + towerPeak + " blocks the tower reached have come down."
                                     : "No block is left on the tower base.";
            Add("  The tower has collapsed: " + lastCollapse);
            GameSounds.Play(GameSound.Collapse);
            int game = s.gameNumber, loser = s.lastPlacer;
            int[] pointsBefore = new int[s.players.Count];
            for (int i = 0; i < pointsBefore.Length; i++) pointsBefore[i] = s.players[i].matchPoints;
            bool event20 = RuleCheck.EventNumber(s) == 20;
            machine.TowerFell();
            // Leave the fallen tower in view until the players move on to the next game.
            if (!machine.MatchOver) ShowGameOver(game, loser, pointsBefore, event20);
        }
        else
        {
            if (fallen > 0) GameSounds.Play(GameSound.BlockFall);
            if (fallen > 0) Add("  " + fallen + " block" + (fallen == 1 ? "" : "s") + " fell off but the tower is still standing. They are set aside (section 7).");
            if (cameDown > 0 && !collapsed) Add("  " + cameDown + " of the " + towerPeak + " blocks the tower reached have come down so far; not a collapse yet.");
            if (roundCheck) machine.TowerStands();
        }
        busy = false;
        AfterMove();
    }

    // Section 7: a block that falls while the tower still stands is set aside until the game ends.
    // It stays out of play (and out of the supply), lined up on the table beside the shake board.
    void SetAside(Rigidbody rb)
    {
        if (rb == null) return;
        setAsideThisGame++;
        GameObject g = rb.gameObject;
        Destroy(rb);
        foreach (Collider col in g.GetComponentsInChildren<Collider>()) col.enabled = false;
        foreach (Renderer r in g.GetComponentsInChildren<Renderer>()) r.enabled = true;
        g.transform.rotation = Quaternion.identity;
        int n = setAside.Count;
        Vector3 spot = new Vector3(gridMax.x + 9f + (n / 4) * 11f, 0f, gridMin.z + 3f + (n % 4) * 9f);
        Bounds b = RenderBounds(g);
        g.transform.position += new Vector3(spot.x - b.center.x, tableTop - b.min.y, spot.z - b.center.z);
        g.name = "Set aside: " + g.name;
        setAside.Add(g);
    }

    void ShowGameOver(int game, int loser, int[] pointsBefore, bool event20)
    {
        List<string> lines = new List<string> { "<size=130%><b>The tower has collapsed!</b></size>", "<size=80%>" + lastCollapse + "</size>", "" };
        lines.Add(loser >= 0 ? "<b>" + s.players[loser].name + "</b> placed the most recent block and loses game " + game + "."
                             : "Nobody had placed a block.");
        List<string> scorers = new List<string>();
        for (int i = 0; i < s.players.Count; i++) if (i != loser) scorers.Add(s.players[i].name);
        if (scorers.Count > 0)
        {
            string who = scorers.Count == 1 ? scorers[0]
                : string.Join(", ", scorers.GetRange(0, scorers.Count - 1).ToArray()) + " and " + scorers[scorers.Count - 1];
            lines.Add(who + (scorers.Count == 1 ? " takes" : " each take") + " " + B.pointsPerGameWin + (B.pointsPerGameWin == 1 ? " point" : " points") + ".");
        }
        if (event20 && loser >= 0 && s.players[loser].matchPoints < pointsBefore[loser])
            lines.Add(s.players[loser].name + " also loses " + B.event20PointLoss + (B.event20PointLoss == 1 ? " point" : " points") + " (Inspector's Final Walk-Through).");
        lines.Add("");
        foreach (int i in s.turnOrder)
            lines.Add(s.players[i].name + ": <color=#ffd94d>" + s.players[i].matchPoints + " / " + rules.pointsToWinMatch + "</color> points");
        gameOverText.text = string.Join("\n", lines.ToArray());
        gameOverPanel.SetActive(true);
        GameSounds.Play(GameSound.GameOver);
        Refresh();
    }

    void NextGame()
    {
        gameOverPanel.SetActive(false);
        ClearBlocks();
        notice = "";
        Refresh();
    }

    float StrengthNow()
    {
        return shakeExtra > 0f ? Mathf.Min(shakeStrength + shakeExtra, Mathf.Max(shakeMaxStrength, shakeStrength)) : shakeStrength;
    }

    // Which shake-only round this shake is (1 = the first of this game), or 0 if blocks are still being placed.
    int ShakeOnlyNumber()
    {
        if (s == null || !s.deckShort) return 0;
        return (shakeOnlyGame == s.gameNumber ? shakeOnlyRounds : 0) + 1;
    }

    float ShakeOnlyStrength(int n)
    {
        return n <= 0 ? shakeStrength : Mathf.Min(shakeStrength + shakeOnlyStep * n, Mathf.Max(shakeMaxStrength, shakeStrength));
    }

    IEnumerator Shake(GameAction a)
    {
        bool aftershocks = s.phase == TurnPhase.Place;          // event 16: before any new placement
        int count = aftershocks ? B.aftershockShakes : RuleCheck.ShakeCount(s);  // R-23: events 5, 10, 19 shake more than once
        shakeSpeed = RuleCheck.ShakeSpeed(s);                    // R-23: event 15 runs at double speed
        int shakeOnly = aftershocks ? 0 : ShakeOnlyNumber();     // deck short: each shake-only round shakes harder
        RuleResult r = machine.Play(a);
        if (!r.legal) { notice = r.ToString(); Refresh(); yield break; }
        if (shakeOnly > 0)
        {
            shakeOnlyGame = s.gameNumber;
            shakeOnlyRounds = shakeOnly;
            shakeExtra = ShakeOnlyStrength(shakeOnly) - shakeStrength;
            Add("  Shake-only round " + shakeOnly + ": the board shakes harder (strength " + StrengthNow().ToString("0.00") + ").");
        }
        if (count > 1 || shakeSpeed > 1f)
            Add("  R-23: " + (count > 1 ? "the board shakes " + count + " times" : "the board shakes") + (shakeSpeed > 1f ? " at double speed" : "") + ".");

        busy = true;
        Dictionary<Rigidbody, float> before = TowerSnapshot(null);   // the tower as it stood before the shake started
        for (shakeNumber = 1; shakeNumber <= count; shakeNumber++)
        {
            shaking = true;
            GameSounds.StartLoop(GameSound.ShakeLoop);
            shakeCount = count;
            shakeEndsAt = Time.time + shakeSeconds;
            shakeStartedAt = Time.time;
            Refresh();
            while (Time.time < shakeEndsAt) yield return null;
            shaking = false;
            GameSounds.StopLoop();
            if (shakeNumber < count) yield return new WaitForSeconds(0.8f);   // back to back, with a breath between
        }
        shakeSpeed = 1f;
        shakeExtra = 0f;
        yield return SettleThenCheck(!aftershocks, before);
    }

    // ---------------- Match setup (rulesheet section 3) ----------------

    void ShowSetup()
    {
        if (startMenu != null) startMenu.SetActive(false);
        SetHud(false);
        StartDemo();   // keep the tower building behind the setup screen
        Image setupBg = setupPanel.GetComponent<Image>();
        if (setupBg != null) setupBg.color = new Color(0.04f, 0.05f, 0.07f, 0.78f);
        setupPanel.SetActive(true);
        overPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        SetPlayerCount(setupCount);
        RefreshSpaceColors();   // back to the plain tower base behind the setup screen
    }

    void SetPlayerCount(int n)
    {
        setupCount = n;
        for (int i = 0; i < nameRows.Count; i++) nameRows[i].SetActive(i < n);
        for (int i = 0; i < downButtons.Count; i++) downButtons[i].interactable = i < n - 1;
        for (int i = 0; i < countButtons.Count; i++)
        {
            ColorBlock cb = countButtons[i].colors;
            cb.normalColor = i + 2 == n ? new Color(1f, 0.82f, 0.35f) : new Color(0.96f, 0.96f, 0.96f);
            cb.selectedColor = cb.normalColor;
            countButtons[i].colors = cb;
        }
    }

    // Swap two names, so players can change who goes first.
    void SwapNames(int a, int b)
    {
        if (a < 0 || b < 0 || a >= nameFields.Count || b >= nameFields.Count) return;
        string t = nameFields[a].text;
        nameFields[a].text = nameFields[b].text;
        nameFields[b].text = t;
        SwapColors(a, b);   // the colour moves with the name
    }

    string PlayerName(int i)
    {
        string n = i < nameFields.Count ? nameFields[i].text.Trim() : "";
        if (n.Length == 0) n = "Player " + (i + 1);
        if (n.Length > 16) n = n.Substring(0, 16);
        return n;
    }

    // StartMatch: the numbered setup procedure, in QuakerPlay.Setup.cs.

    DeckManager NewDeck(string deckName, bool refill)
    {
        Transform old = transform.Find(deckName);
        if (old != null) Destroy(old.gameObject);
        GameObject go = new GameObject(deckName);
        go.transform.SetParent(transform, false);
        DeckManager d = go.AddComponent<DeckManager>();
        d.allowRefill = refill;
        return d;
    }

    // ---------------- Per-frame input ----------------

    void Update()
    {
        UpdateCamera();
        bool waitingForReady = UpdateTurnUI();   // events 3 and 14: the player clicks Ready first
        if (revealPlayer >= 0 && Time.time >= revealUntil) { revealPlayer = -1; Refresh(); }   // the drawn card has been seen
        if (machine == null || machine.MatchOver || setupPanel.activeSelf || gameOverPanel.activeSelf)
        {
            SetGhost(null);
            return;
        }
        if (!gameTestRunning && CheckDeadCard()) return;   // R-26: the builder's block fits nowhere (the game tests call it themselves)
        if (shaking)
            prompt.text = "<b>Shaking" + (shakeCount > 1 ? " (" + shakeNumber + " of " + shakeCount + ")" : "") + (shakeSpeed > 1f ? " at double speed" : "")
                + (shakeExtra > 0f ? " harder (shake-only round " + shakeOnlyRounds + ")" : "")
                + "...</b> " + Mathf.CeilToInt(shakeEndsAt - Time.time) + "   Nobody touches the tower or the tower base.";
        else if (!busy && s.phase == TurnPhase.CollapseCheck) StartCoroutine(SettleThenCheck(true));   // e.g. event 18: no shake

        if (waitingForReady) SetGhost(null);   // no aiming and no ten-count until Ready
        else
        {
            UpdateTenCount();
            UpdateAim();
        }
        SetTowerHidden(ghost != null && RuleCheck.Blind(s));   // R-25: event 3, place blind
    }

    // R-24: with event 14 face up, each placement must happen within a ten-count.
    void UpdateTenCount()
    {
        bool placing = !busy && s.phase == TurnPhase.Place && RuleCheck.TenCount(s) && Actor() == s.buildTurn && s.buildTurn >= 0
            && s.players[s.buildTurn].heldBlock != null;
        if (!placing) { tenCountEndsAt = -1f; tenCountTurn = ""; return; }

        string turn = s.roundInGame + ":" + s.buildTurn + ":" + s.players[s.buildTurn].placementsThisRound;
        if (turn != tenCountTurn)
        {
            tenCountTurn = turn;
            tenCountEndsAt = Time.time + B.tenCountSeconds;
        }
        if (Time.time >= tenCountEndsAt)
        {
            tenCountEndsAt = -1f;
            machine.MissedTenCount();
            AfterMove();
        }
    }

    // R-25: hide every block already on the tower (and the aiming preview) while a blind placement is aimed.
    void SetTowerHidden(bool hide)
    {
        if (hide == towerHidden) return;
        towerHidden = hide;
        foreach (Rigidbody rb in placed)
            if (rb != null) foreach (Renderer r in rb.GetComponentsInChildren<Renderer>()) r.enabled = !hide;
    }

    CardData StyleToPlace(int who)
    {
        if (busy || who < 0) return null;
        if (s.phase == TurnPhase.Opening) return openingStyle;
        if (RuleCheck.RivalPending(s) || RuleCheck.AftershocksPending(s)) return null;
        if (s.phase == TurnPhase.Place) return s.players[who].heldBlock;
        return null;
    }

    void UpdateAim()
    {
        int who = Actor();
        CardData style = StyleToPlace(who);
        SetGhost(style);
        if (ghost == null) return;

        bool rotLeft = false, rotRight = false, tipUp = false, tipDown = false, click = false;
        levelLower = levelHigher = false;
        Vector2 mouse = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            Keyboard k = Keyboard.current;
            rotLeft = k.aKey.wasPressedThisFrame || k.leftArrowKey.wasPressedThisFrame;     // A: -90 degrees
            rotRight = k.dKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame;   // D: +90 degrees
            tipUp = k.wKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame;         // W: tip up
            tipDown = k.sKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame;     // S: tip down
            levelLower = k.qKey.wasPressedThisFrame;                                        // Q: under an overhang
            levelHigher = k.eKey.wasPressedThisFrame;                                       // E: back up on top
        }
        if (Mouse.current != null)
        {
            mouse = Mouse.current.position.ReadValue();
            click = Mouse.current.leftButton.wasPressedThisFrame;
        }
#endif
        // Every press is 90 degrees. A/D spin the block flat on the table; W/S tip it up or down, as seen from
        // the camera, whichever side of the tower you are looking from. Spinning and tipping together reach
        // every way a block can face.
        Vector3 fwd = SnapToAxis(Camera.main != null ? Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up) : Vector3.forward);
        Vector3 right = Vector3.Cross(Vector3.up, fwd);
        if (rotLeft) ghostTurn = Quaternion.AngleAxis(-90, Vector3.up) * ghostTurn;
        if (rotRight) ghostTurn = Quaternion.AngleAxis(90, Vector3.up) * ghostTurn;
        if (tipUp) ghostTurn = Quaternion.AngleAxis(-90, right) * ghostTurn;
        if (tipDown) ghostTurn = Quaternion.AngleAxis(90, right) * ghostTurn;
        if (rotLeft || rotRight || tipUp || tipDown) ghostTurn = SnapRotation(ghostTurn);
        ghost.transform.rotation = ghostTurn;

        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        Camera cam = Camera.main;
        RaycastHit hit;
        if (cam == null || overUI || !Physics.Raycast(cam.ScreenPointToRay(mouse), out hit, 500f))
        {
            ghost.SetActive(false);
            return;
        }
        ghost.SetActive(true);

        // Snap the footprint to the 4x4 grid, then drop the block straight down onto whatever is below it.
        Bounds gb = RenderBounds(ghost);
        int pointed = NearestTile(hit.point, cell * 0.75f);
        Vector3 c = hit.point;
        if (pointed > 0)
        {
            Vector3 t = tileCentres[pointed];
            int wx = Mathf.Max(1, Mathf.RoundToInt(gb.size.x / cell));
            int wz = Mathf.Max(1, Mathf.RoundToInt(gb.size.z / cell));
            c.x = t.x + (wx % 2 == 0 ? cell * 0.5f * Mathf.Sign(hit.point.x - t.x + 0.0001f) : 0f);
            c.z = t.z + (wz % 2 == 0 ? cell * 0.5f * Mathf.Sign(hit.point.z - t.z + 0.0001f) : 0f);
            // Keep the whole footprint on the 4x4 grid: near an edge, slide it inward a whole cell.
            if (wx <= 4) c.x = Mathf.Clamp(c.x, gridMin.x + wx * cell * 0.5f, gridMax.x - wx * cell * 0.5f);
            if (wz <= 4) c.z = Mathf.Clamp(c.z, gridMin.z + wz * cell * 0.5f, gridMax.z - wz * cell * 0.5f);
        }
        // Every height where the block can rest here without passing through anything: on the base, on top of
        // the tower, or tucked under an overhang (like the gap under a Z-Block). Q goes lower, E goes higher.
        Vector3 pivotOffset = ghost.transform.position - gb.center;
        List<Level> levels = RestingLevels(gb, c);
        string aimKey = Mathf.RoundToInt(c.x * 10f) + ":" + Mathf.RoundToInt(c.z * 10f) + ":" + ghostTurn.GetHashCode();
        if (aimKey != levelAimKey) { levelAimKey = aimKey; levelFromTop = 0; }   // new spot or turn: start on top
        if (levelLower) levelFromTop++;
        if (levelHigher) levelFromTop--;
        levelFromTop = Mathf.Clamp(levelFromTop, 0, levels.Count - 1);
        Level chosen = levels[levels.Count - 1 - levelFromTop];
        bool onBlock = chosen.onBlock;
        float bottom = chosen.bottom;
        ghost.transform.position = new Vector3(c.x, bottom + gb.extents.y + 0.15f, c.z) + pivotOffset;

        // Which spaces are under the block: one per 4-unit cube of the model.
        HashSet<int> covers = new HashSet<int>();
        int cubesOff = 0;   // cubes of this block that are not over any of the 16 spaces
        foreach (Renderer r in ghost.GetComponentsInChildren<Renderer>())
        {
            int id = NearestTile(r.bounds.center, cell * 0.6f);
            if (id > 0) covers.Add(id);
            else cubesOff++;
        }
        if (pointed <= 0 && covers.Count > 0) foreach (int id in covers) { pointed = id; break; }

        SpaceData sd = pointed > 0 ? towerBase.Get(pointed) : null;
        aimed = new GameAction
        {
            type = ActionType.PlaceBlock,
            player = who,
            card = style,
            space = pointed,
            spaceName = sd != null ? sd.displayName : "off the base",
            angle = Mathf.RoundToInt(ghostTurn.eulerAngles.y),
            physical = true,
            onBlock = onBlock,
            covers = new List<int>(covers).ToArray(),
            offBase = cubesOff > 0
        };
        aimRule = LegalMoves.Validate(s, aimed);
        aimValid = aimRule.legal;
        Tint(ghost, aimValid ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.45f, 0.45f));
        bool blind = RuleCheck.Blind(s);
        foreach (Renderer gr in ghost.GetComponentsInChildren<Renderer>()) gr.enabled = !blind;
        string timer = tenCountEndsAt > 0f ? "<color=#ffb060><b>Ten-count: " + Mathf.CeilToInt(tenCountEndsAt - Time.time) + "</b></color>   " : "";
        string levelNote = levels.Count > 1
            ? "   <color=#9fd0ff>" + (levelFromTop == 0 ? "On top" : "Underneath") + " (height " + (levels.Count - levelFromTop) + " of " + levels.Count
              + ": " + (levelFromTop < levels.Count - 1 ? "Q to go under" : "") + (levelFromTop > 0 && levelFromTop < levels.Count - 1 ? ", " : "")
              + (levelFromTop > 0 ? "E to go higher" : "") + ")</color>"
            : "";
        prompt.text = PromptFor(who) + "\n" + timer + (aimValid
            ? "<color=#a0ffa0>Space " + pointed + " (" + aimed.spaceName + ")" + (onBlock ? ", on a block" : "") + ". Click to place.</color>"
            : "<color=#ff8080>" + aimRule + "</color>") + levelNote;

        if (click && !overUI) Place();
    }

    void Place()
    {
        int gameBefore = s.gameNumber;
        RuleResult r = machine.Play(aimed);
        if (!r.legal) { notice = r.ToString(); Refresh(); return; }

        GameObject block = SpawnBlock(aimed.card, false);
        block.transform.rotation = ghost.transform.rotation;
        block.transform.position = ghost.transform.position;
        Rigidbody rb = block.AddComponent<Rigidbody>();
        rb.mass = blockMass;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        PhysicsMaterial grip = new PhysicsMaterial("Block") { staticFriction = 0.7f, dynamicFriction = 0.6f, bounciness = 0f };
        Collider[] cols = block.GetComponentsInChildren<Collider>();
        foreach (Collider col in cols) col.sharedMaterial = grip;
        placed.Add(rb);

        // Debug line for the Console: if a block ever passes through the base, these numbers show why.
        Bounds blockBounds = cols.Length > 0 ? cols[0].bounds : RenderBounds(block);
        foreach (Collider col in cols) blockBounds.Encapsulate(col.bounds);
        Debug.Log("[Quaker debug] Dropped " + block.name + ": " + cols.Length + " box colliders, collider bottom y="
            + blockBounds.min.y.ToString("0.00") + ", size " + blockBounds.size.ToString("0.0")
            + ". Base slab top y=" + (baseSlab != null ? baseSlab.bounds.max.y.ToString("0.00") : "none")
            + ", slab size " + (baseSlab != null ? baseSlab.bounds.size.ToString("0.0") : "-") + ".");
        GameSounds.Play(GameSound.BlockPlace);

        notice = "";
        SetGhost(null);
        if (s.gameNumber == gameBefore) StartCoroutine(SettleThenCheck(false));
    }

    struct Level
    {
        public float bottom;     // y of the underside of the block
        public bool onBlock;     // resting on a block rather than on the tower base
    }

    // Heights (lowest first) where the aimed block can rest with its footprint centred at c: something must be
    // directly under one of its cubes (the base or a block), and none of its cubes may overlap a block already
    // there. Players slide a block in from the side, so a gap under an overhang counts. Always has at least
    // one entry: on top of everything.
    List<Level> RestingLevels(Bounds gb, Vector3 c)
    {
        // The aimed block's cubes, as boxes relative to the bottom-centre of its footprint.
        List<Bounds> cubes = new List<Bounds>();
        foreach (Renderer r in ghost.GetComponentsInChildren<Renderer>())
        {
            Bounds b = r.bounds;
            b.center = new Vector3(b.center.x - gb.center.x, b.center.y - gb.min.y, b.center.z - gb.center.z);
            cubes.Add(b);
        }

        // Block colliders under the footprint, and the tops they offer as support.
        List<Collider> nearby = new List<Collider>();
        foreach (Rigidbody rb in placed)
        {
            if (rb == null) continue;
            foreach (Collider col in rb.GetComponentsInChildren<Collider>())
            {
                Bounds cb = col.bounds;
                foreach (Bounds cube in cubes)
                {
                    float cx = c.x + cube.center.x, cz = c.z + cube.center.z;
                    if (Mathf.Abs(cx - cb.center.x) < cube.extents.x + cb.extents.x - 0.3f
                        && Mathf.Abs(cz - cb.center.z) < cube.extents.z + cb.extents.z - 0.3f) { nearby.Add(col); break; }
                }
            }
        }

        // A candidate height puts the underside of one of the cubes exactly on the top of a block below it.
        List<float> tops = new List<float> { baseTop };
        foreach (Collider col in nearby)
        {
            Bounds cb = col.bounds;
            foreach (Bounds cube in cubes)
            {
                if (Mathf.Abs(c.x + cube.center.x - cb.center.x) >= cube.extents.x + cb.extents.x - 0.3f
                    || Mathf.Abs(c.z + cube.center.z - cb.center.z) >= cube.extents.z + cb.extents.z - 0.3f) continue;
                float h = cb.max.y - cube.min.y;
                if (h >= baseTop - 0.01f) tops.Add(h);
            }
        }
        tops.Sort();

        List<Level> levels = new List<Level>();
        float highest = baseTop;
        foreach (float h in tops)
        {
            highest = Mathf.Max(highest, h);
            if (levels.Count > 0 && Mathf.Abs(levels[levels.Count - 1].bottom - h) < 0.5f) continue;
            if (!Fits(cubes, c, h, nearby)) continue;
            levels.Add(new Level { bottom = h, onBlock = h > baseTop + 0.5f || SupportedByBlock(cubes, c, h, nearby) });
        }
        if (levels.Count == 0) levels.Add(new Level { bottom = highest, onBlock = highest > baseTop + 0.5f });
        return levels;
    }

    // True when no cube of the aimed block, resting with its underside at h, overlaps a block collider.
    static bool Fits(List<Bounds> cubes, Vector3 c, float h, List<Collider> nearby)
    {
        foreach (Bounds cube in cubes)
        {
            Bounds at = new Bounds(new Vector3(c.x + cube.center.x, h + cube.center.y, c.z + cube.center.z), cube.size - Vector3.one * 0.3f);
            foreach (Collider col in nearby)
                if (col.bounds.Intersects(at)) return false;
        }
        return true;
    }

    // True when one of the aimed block's lowest cubes sits on a block (not the base) at height h.
    static bool SupportedByBlock(List<Bounds> cubes, Vector3 c, float h, List<Collider> nearby)
    {
        foreach (Bounds cube in cubes)
        {
            float cubeBottom = h + cube.min.y;
            foreach (Collider col in nearby)
            {
                Bounds cb = col.bounds;
                if (Mathf.Abs(cb.max.y - cubeBottom) > 0.5f) continue;
                if (Mathf.Abs(c.x + cube.center.x - cb.center.x) < cube.extents.x + cb.extents.x - 0.3f
                    && Mathf.Abs(c.z + cube.center.z - cb.center.z) < cube.extents.z + cb.extents.z - 0.3f) return true;
            }
        }
        return false;
    }

    // The nearest of the four horizontal grid directions.
    static Vector3 SnapToAxis(Vector3 v)
    {
        if (v.sqrMagnitude < 1e-6f) return Vector3.forward;
        return Mathf.Abs(v.x) > Mathf.Abs(v.z) ? new Vector3(Mathf.Sign(v.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(v.z));
    }

    // Keeps the block exactly square to the grid after many turns (no floating-point drift).
    static Quaternion SnapRotation(Quaternion q)
    {
        Vector3 f = q * Vector3.forward, u = q * Vector3.up;
        f = new Vector3(Mathf.Round(f.x), Mathf.Round(f.y), Mathf.Round(f.z));
        u = new Vector3(Mathf.Round(u.x), Mathf.Round(u.y), Mathf.Round(u.z));
        return Quaternion.LookRotation(f, u);
    }

    int NearestTile(Vector3 p, float within)
    {
        int best = -1;
        float bestD = within * within;
        foreach (KeyValuePair<int, Vector3> kv in tileCentres)
        {
            float dx = p.x - kv.Value.x, dz = p.z - kv.Value.z;
            float d = dx * dx + dz * dz;
            if (d < bestD) { bestD = d; best = kv.Key; }
        }
        return best;
    }

    void SetGhost(CardData style)
    {
        if (style == ghostStyle && (ghost != null || style == null)) return;
        if (ghost != null) Destroy(ghost);
        ghost = null;
        ghostStyle = style;
        if (style == null) return;
        ghost = SpawnBlock(style, true);
        ghost.transform.rotation = ghostTurn;
    }

    static void Tint(GameObject go, Color c)
    {
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>()) SetColour(r, c);
    }

    static void SetColour(Renderer r, Color c)
    {
        if (r == null) return;
        foreach (Material m in r.materials)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            else if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }
    }

    void UpdateCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
#if ENABLE_INPUT_SYSTEM
        Mouse m = Mouse.current;
        if (m != null)
        {
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (m.rightButton.isPressed)
            {
                Vector2 d = m.delta.ReadValue();
                yaw += d.x * 0.25f;
                pitch = Mathf.Clamp(pitch - d.y * 0.25f, 10f, 85f);
            }
            else if (demoRunning) yaw += menuOrbitSpeed * Time.deltaTime;   // slow turn around the demo tower
            float scroll = m.scroll.ReadValue().y;
            if (!overUI && Mathf.Abs(scroll) > 0.01f) distance = Mathf.Clamp(distance - Mathf.Sign(scroll) * 2f, 12f, 90f);
        }
#endif
        PoseCamera(cam);
    }

    // Editor: measure the scene objects and put the camera where Play starts, so the Game view matches.
    public void PreviewInEditor()
    {
        if (shakeBoard == null || towerBaseModel == null) return;
        ReadWorld();
        if (Camera.main != null) PoseCamera(Camera.main);
    }

    float cameraLift;   // start menu: the camera rises with the demo tower

    void PoseCamera(Camera cam)
    {
        float lift = demoRunning ? Mathf.Clamp(TowerHeight() * 0.45f, 0f, 14f) : 0f;
        cameraLift = Mathf.Lerp(cameraLift, lift, Application.isPlaying ? Mathf.Clamp01(Time.deltaTime * 1.5f) : 1f);
        Vector3 target = baseCentre + Vector3.up * (4f + cameraLift);
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        cam.transform.position = target + rot * new Vector3(0f, 0f, -distance);
        cam.transform.LookAt(target);
    }

    // ---------------- Turn input ----------------

    int Actor()
    {
        switch (s.phase)
        {
            case TurnPhase.Place:
                if (RuleCheck.RivalPending(s) || RuleCheck.AftershocksPending(s)) return s.activePlayer;   // events 9, 16
                return s.buildTurn;
            case TurnPhase.Opening: return s.buildTurn;
            case TurnPhase.Event:
            case TurnPhase.Shake: return s.activePlayer;
            case TurnPhase.Draw:
                foreach (int i in s.turnOrder) if (!s.players[i].drewThisRound) return i;
                return -1;
        }
        return -1;
    }

    void Do(GameAction a)
    {
        if (a.type == ActionType.StartShake) { StartCoroutine(Shake(a)); return; }
        bool drawingBlock = a.type == ActionType.DrawBlockCard;
        RuleResult r = machine.Play(a);
        if (r.legal && drawingBlock) { lastDrawer = a.player; revealPlayer = a.player; revealUntil = Time.time + RevealSeconds; GameSounds.Play(GameSound.CardDraw); }
        if (r.legal && a.type == ActionType.DrawEventCard) GameSounds.Play(GameSound.EventFlip);
        if (r.legal) OnActionDone(a);   // banners about other players (QuakerPlay.Turns.cs)
        if (r.legal && a.type == ActionType.DrawEventCard) CoffeeBreakDraws();
        notice = r.legal ? "" : r.ToString();
        AfterMove();
    }

    // R-13, card 4 (Coffee Break): everyone draws, then discards, and nobody places. The cards still leave the
    // block deck, but there is nothing for the players to decide, so the build draws for them in turn order.
    // The cards go to the discard pile at the end of the round with everyone else's.
    void CoffeeBreakDraws()
    {
        if (RuleCheck.EventNumber(s) != 4 || s.phase != TurnPhase.Draw || s.deckShort) return;
        int drawn = 0;
        foreach (int i in new List<int>(s.turnOrder))
        {
            if (s.phase != TurnPhase.Draw) break;
            if (machine.Play(new GameAction { type = ActionType.DrawBlockCard, player = i }).legal) drawn++;
        }
        coffeeDrawn = drawn;
    }

    void AfterMove()
    {
        if (s.phase == TurnPhase.Opening && (openingStyle == null || RuleCheck.SupplyLeft(s, openingStyle) <= 0))
            openingStyle = FirstStyleInSupply();
        Refresh();
    }

    CardData FirstStyleInSupply()
    {
        foreach (CardData c in blockStyles) if (c != null && RuleCheck.SupplyLeft(s, c) > 0) return c;
        return null;
    }

    // ---------------- HUD ----------------

    void Refresh()
    {
        if (machine == null) return;
        int who = Actor();

        bool fuse = rules != null && rules.roundLimit > 0;   // Fuse Lit: the round count is announced out loud
        header.text = "<b>QUAKER</b>   Game " + s.gameNumber + "   Round " + s.roundInGame + (fuse && s.roundInGame <= rules.roundLimit ? " of " + rules.roundLimit : "")
            + "   <b>" + PhaseTitle() + "</b>"
            + (s.phase != TurnPhase.Opening && s.fuseOut ? "   <color=#ffb060><size=75%>Fuse burnt out: shake rounds</size></color>"
             : s.phase != TurnPhase.Opening && DeckShortNow() ? "   <color=#ffb060><size=75%>Block deck short: shake rounds</size></color>" : "");
        prompt.text = PromptFor(who) + (notice.Length > 0 ? "\n<color=#ff8080>" + notice + "</color>" : "");

        RefreshCardPanels(who);
        RefreshSpaceColors();
        playersText.text = PlayersSummary(who);


        RefreshStyles();
        RefreshActions(who);

        if (machine.MatchOver && !overPanel.activeSelf) GameSounds.Play(GameSound.MatchWin);
        overPanel.SetActive(machine.MatchOver);
        if (machine.MatchOver) overText.text = MatchOverText();
    }

    // Build 6 panels: the face-up event card on the left, the current block card on the right.
    void RefreshCardPanels(int who)
    {
        if (eventSlot == null) return;

        if (s.eventCard != shownEvent)
        {
            shownEvent = s.eventCard;
            eventSlot.gameObject.SetActive(shownEvent != null);
            if (shownEvent != null) eventSlot.Show(shownEvent);
        }
        eventLabel.text = shownEvent != null ? "Current Event"
            : s.deckShort && s.phase != TurnPhase.Opening ? "Current Event\n<size=75%>none: shake-only round</size>"
            : "Current Event\n<size=75%>none yet</size>";
        drawEventButton.interactable = !busy && who >= 0 && s.phase == TurnPhase.Event
            && LegalMoves.IsLegal(s, new GameAction { type = ActionType.DrawEventCard, player = who });

        // Block panel: right after a draw, the card that player just drew (for RevealSeconds, even if the draw moved
        // the round on to placing). Otherwise it follows the acting player: their card while they place, and empty
        // while the next player still has to draw, so nobody mistakes the last player's card for their own.
        bool revealing = revealPlayer >= 0 && revealPlayer < s.players.Count && Time.time < revealUntil
            && s.players[revealPlayer].heldBlock != null;
        int owner = revealing ? revealPlayer : s.phase == TurnPhase.Place ? who : -1;
        if (owner >= s.players.Count) owner = -1;
        CardData card = owner >= 0 ? s.players[owner].heldBlock : null;
        if (card != shownBlock)
        {
            shownBlock = card;
            blockSlot.gameObject.SetActive(card != null);
            if (card != null) blockSlot.Show(card);
        }
        blockCurrent.text = card != null ? Tinted(owner, s.players[owner].name) + (revealing ? " drew: " : ": ") + card.title
            : s.phase == TurnPhase.Draw && who >= 0 && !s.deckShort ? Tinted(who, s.players[who].name) + "\n<size=85%>draw your block card</size>"
            : SecondDrawPending() ? Tinted(s.buildTurn, "Structural Audit:") + "\n<size=85%>draw a second block</size>"
            : s.phase != TurnPhase.Opening && DeckShortNow() ? "Deck short: no draws" : "Drawn Card: None";
        blockDrawPile.text = "Draw Pile: " + s.blockDeck.DrawPileCount;
        blockDiscardPile.text = "Discard Pile: " + s.blockDeck.DiscardPileCount;

        bool canDraw = !busy && who >= 0 && LegalMoves.IsLegal(s, new GameAction { type = ActionType.DrawBlockCard, player = who });
        drawBlockButton.interactable = canDraw;
        drawBlockButton.GetComponentInChildren<TMP_Text>().text = canDraw ? "Draw Block\n<size=70%>" + s.players[who].name + "</size>" : "Draw Block";
    }

    // Section 7: with fewer block cards than players, rounds are only Event, Shake and Check.
    bool DeckShortNow()
    {
        return s.deckShort || s.blockDeck.TotalAvailable < s.players.Count;
    }

    string PhaseTitle()
    {
        switch (s.phase)
        {
            case TurnPhase.Opening: return "Opening placement";
            case TurnPhase.Event: return "Event phase";
            case TurnPhase.Draw: return "Draw phase";
            case TurnPhase.Place: return "Build phase";
            case TurnPhase.Shake: return "Shake phase";
            case TurnPhase.CollapseCheck: return "Check phase";
        }
        return s.phase.ToString();
    }

    string PromptFor(int who)
    {
        if (busy && !shaking) return "Waiting for the tower to settle...";
        string name = who >= 0 ? "<b>" + Tinted(who, s.players[who].name) + "</b>" : "";
        const string controls = "  <size=75%>(A/D turn, W/S tip up/down, Q/E under or on top, click to drop; right-drag to orbit, scroll to zoom)</size>";
        switch (s.phase)
        {
            case TurnPhase.Opening:
                int must = RuleCheck.OpeningSpace(s, who);
                return name + ": choose any block style from the supply and drop it on the tower base."
                    + (must > 0 ? " <color=#ffd060>" + rules.displayName + ": it must cover your gold space (space " + must + ").</color>" : "") + controls;
            case TurnPhase.Event:
                return name + ": flip the top event card and read it aloud." + (DeckShortNow()
                    ? "\n<color=#ffb060>The block deck is down to " + s.blockDeck.TotalAvailable + " card" + (s.blockDeck.TotalAvailable == 1 ? "" : "s")
                      + ", fewer than the players: nobody draws or places now. Event, shake and check until the tower falls.</color>" : "");
            case TurnPhase.Draw:
                return name + ": draw your block card for this round.";
            case TurnPhase.Place:
                if (RuleCheck.RivalPending(s)) return name + ": Second Crew Arrives. Pick a rival; they redraw their block card.";
                if (RuleCheck.AftershocksPending(s)) return name + ": Aftershock Cluster. Run " + B.aftershockShakes + " shakes before anyone places.";
                PlayerState p = s.players[who];
                if (p.heldBlock == null) return name + ": <b>Structural Audit</b> - you place a second block. Click <b>Draw Block</b> for another block card.";
                string extra = "";
                int ev = RuleCheck.EventNumber(s);
                if (ev == 1 && !p.redrewThisRound) extra = " You may redraw it once (Site Inspector).";
                if (p.extraOption != null) extra = " Or use your extra option, <b>" + p.extraOption.title + "</b> (Permit Renewed).";
                if (ev == 3) extra += " <b>Blind:</b> you can't see the tower or your block.";
                return name + ": place " + YourBlock(p.heldBlock, "<b>", "</b>") + " on the tower." + extra + controls;
            case TurnPhase.Shake:
                return name + ": start the shake. The board shakes for " + shakeSeconds + " seconds." + (s.deckShort
                    ? "\n<color=#ffb060>" + (s.fuseOut ? rules.displayName + ": the " + rules.roundLimit + " build rounds are over" : "Block deck short") + ": shake-only round " + ShakeOnlyNumber() + ", so the board shakes harder ("
                      + Mathf.RoundToInt(ShakeOnlyStrength(ShakeOnlyNumber()) / shakeStrength * 100f) + "% of a normal shake). If the tower collapses, "
                      + (s.lastPlacer >= 0 ? s.players[s.lastPlacer].name : "the last placer") + " placed the most recent block and loses.</color>" : "");
            case TurnPhase.CollapseCheck:
                return "Checking the tower...";
        }
        return "";
    }

    string PlayersSummary(int who)
    {
        List<string> lines = new List<string> { "<b>Turn order</b>", "" };
        foreach (int i in s.turnOrder)
        {
            PlayerState p = s.players[i];
            string status;
            if (s.phase == TurnPhase.Opening) status = p.placedOpening ? "opening block placed" : "waiting";
            else if (p.skippedThisRound) status = "skipped";
            else if (p.placementsThisRound > 0) status = "placed";
            else if (p.heldBlock != null) status = "card: " + p.heldBlock.title + (p.extraOption != null ? " or " + p.extraOption.title : "");
            else status = p.drewThisRound ? "no card" : "-";
            string tag = i == s.turnOrder[0] ? " <size=75%>(first player)</size>" : "";
            lines.Add((i == who ? "<b>> " : "   ") + Tinted(i, p.name) + (i == who ? "</b>" : "") + tag);
            lines.Add("      <color=#ffd94d>" + p.matchPoints + " / " + rules.pointsToWinMatch + "</color> points   <size=80%>" + status + "</size>");
        }
        lines.Add("");
        lines.Add("<size=80%>Blocks on the tower: " + placed.Count + (setAside.Count > 0 ? "   Set aside: " + setAside.Count : "") + "   Block deck: " + s.blockDeck.TotalAvailable + "   Event deck: " + s.eventDeck.TotalAvailable + "</size>");
        if (s.lastPlacer >= 0) lines.Add("<size=80%>Most recent block: " + s.players[s.lastPlacer].name + "</size>");
        return string.Join("\n", lines.ToArray());
    }

    void RefreshStyles()
    {
        Clear(styleArea);
        bool opening = s.phase == TurnPhase.Opening && !busy;
        styleArea.gameObject.SetActive(s.phase == TurnPhase.Opening);
        float w = 1f / Mathf.Max(1, blockStyles.Count);
        for (int k = 0; k < blockStyles.Count; k++)
        {
            CardData c = blockStyles[k];
            if (c == null) continue;
            int left = RuleCheck.SupplyLeft(s, c);
            Button b = MakeButton(styleArea, c.title + "\n<size=75%>" + left + " in supply</size>", 20);
            Anchor((RectTransform)b.transform, new Vector2(k * w, 0), new Vector2((k + 1) * w, 1), new Vector2(4, 0), new Vector2(-4, 0));
            ((Image)b.targetGraphic).color = left <= 0 ? new Color(0.35f, 0.35f, 0.38f)
                : c == openingStyle ? new Color(1f, 0.85f, 0.3f)
                : new Color(0.85f, 0.87f, 0.9f);
            b.interactable = opening && left > 0;
            CardData chosen = c;
            b.onClick.AddListener(() => { openingStyle = chosen; notice = ""; Refresh(); });
        }
    }

    void RefreshActions(int who)
    {
        Clear(actionArea);
        if (busy || who < 0) return;
        List<KeyValuePair<string, System.Action>> buttons = new List<KeyValuePair<string, System.Action>>();
        foreach (GameAction a in LegalMoves.Get(s, s.players[who]))
        {
            GameAction act = a;
            // Drawing event and block cards happens in the Build 6 card panels (Draw Event / Draw Block).
            if (a.type == ActionType.StartShake)
            {
                int n = s.phase == TurnPhase.Place ? B.aftershockShakes : RuleCheck.ShakeCount(s);
                string label = s.phase == TurnPhase.Place ? "Run the aftershocks (" + n + " shakes)"
                    : n > 1 ? "Start the shake (" + n + " x " + shakeSeconds + "s)" : "Start the shake (" + shakeSeconds + "s)";
                buttons.Add(Btn(label, () => Do(act)));
            }
            if (a.type == ActionType.RedrawBlock) buttons.Add(Btn("Redraw my block card", () => Do(act)));
            if (a.type == ActionType.SwapOption)
                buttons.Add(Btn("Use " + (s.players[who].extraOption != null ? s.players[who].extraOption.title : "?") + " instead", () => Do(act)));
            if (a.type == ActionType.PickRival) buttons.Add(Btn("Rival: " + s.players[a.target].name, () => Do(act)));
        }
        float w = 1f / Mathf.Max(3, buttons.Count);
        for (int k = 0; k < buttons.Count; k++)
        {
            Button b = MakeButton(actionArea, buttons[k].Key, buttons.Count > 3 ? 20 : 24);
            Anchor((RectTransform)b.transform, new Vector2(k * w, 0), new Vector2((k + 1) * w, 1), new Vector2(5, 0), new Vector2(-5, 0));
            System.Action click = buttons[k].Value;
            b.onClick.AddListener(() => click());
        }
    }

    static KeyValuePair<string, System.Action> Btn(string label, System.Action a) { return new KeyValuePair<string, System.Action>(label, a); }

    string MatchOverText()
    {
        List<string> names = new List<string>();
        foreach (PlayerState p in s.players) if (p.matchPoints >= rules.pointsToWinMatch) names.Add(p.name);
        string who = names.Count > 1 ? string.Join(" and ", names.ToArray()) + " share the match win!" : (names.Count == 1 ? names[0] + " wins the match!" : "Match over.");
        List<string> lines = new List<string> { "<size=140%><b>" + who + "</b></size>", "" };
        foreach (int i in s.turnOrder) lines.Add(s.players[i].name + ": " + s.players[i].matchPoints + " points");
        lines.Add("");
        lines.Add("Games played: " + s.gameNumber);
        return string.Join("\n", lines.ToArray());
    }

    void Add(string line)
    {
        log.Add(line);
        Debug.Log("[Quaker] " + line);
    }

    // ---------------- UI construction ----------------

    // Editor: builds the whole UI as saved objects under this one (instantiate keeps the CardUI prefab links).
    public void CreateUIObjects(System.Func<GameObject, Transform, GameObject> instantiate)
    {
        RemoveUIObjects();
        uiSpawn = instantiate;
        try { BuildUI(); } finally { uiSpawn = null; }
    }

    public void RemoveUIObjects()
    {
        Transform old = transform.Find("QuakerCanvas");
        while (old != null)
        {
            old.SetParent(null);
            if (Application.isPlaying) Destroy(old.gameObject); else DestroyImmediate(old.gameObject);
            old = transform.Find("QuakerCanvas");
        }
        canvasRoot = null;
    }

    CardView NewCard(Transform parent)
    {
        if (uiSpawn != null) return uiSpawn(cardPrefab.gameObject, parent).GetComponent<CardView>();
        return Instantiate(cardPrefab, parent);
    }

    // Play: hooks every button up to the game. Runs on the saved UI and on a freshly built one alike.
    void WireUI()
    {
        for (int i = 0; i < countButtons.Count; i++)
        {
            int n = i + 2;
            countButtons[i].onClick.AddListener(() => SetPlayerCount(n));
        }
        for (int i = 0; i < nameFields.Count; i++)
        {
            string saved = "";
            try { saved = PlayerPrefs.GetString("QuakerName" + i, ""); } catch { }
            nameFields[i].text = saved;
        }
        for (int i = 0; i < upButtons.Count; i++)
        {
            int k = i;
            upButtons[i].onClick.AddListener(() => SwapNames(k, k - 1));
        }
        for (int i = 0; i < downButtons.Count; i++)
        {
            int k = i;
            downButtons[i].onClick.AddListener(() => SwapNames(k, k + 1));
        }
        EnsureScenarioPicker();   // Build 9 (QuakerPlay.Setup.cs)
        if (startButton != null) startButton.onClick.AddListener(() => StartMatch(setupCount));
        if (nextButton != null) nextButton.onClick.AddListener(NextGame);
        if (againButton != null) againButton.onClick.AddListener(ShowSetup);
        Button mainMenu = MatchOverMenuButton();
        if (mainMenu != null) mainMenu.onClick.AddListener(() => LoadScene(menuSceneName));
        if (drawEventButton != null) drawEventButton.onClick.AddListener(() =>
        {
            int who = Actor();
            if (machine != null && who >= 0) Do(new GameAction { type = ActionType.DrawEventCard, player = who });
        });
        if (drawBlockButton != null) drawBlockButton.onClick.AddListener(() =>
        {
            int who = Actor();
            if (machine != null && who >= 0) Do(new GameAction { type = ActionType.DrawBlockCard, player = who });
        });
        WireRules();
    }

    List<KeyValuePair<string, string>> RuleSections()
    {
        return rulesText != null
            ? ParseRules(rulesText.text)
            : new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("Rules", "The rules file (Art/Rules_v2.txt) is not assigned.") };
    }

    // The Help tabs. The text always comes from Rules_v2.txt, so editing the file needs no rebuild
    // unless you add or remove a "## " category (then run Rebuild Scene Objects for the new tabs).
    void WireRules()
    {
        if (rulesHeading == null || rulesBody == null) return;
        List<KeyValuePair<string, string>> sections = RuleSections();
        if (sections.Count != rulesTabs.Count)
            Debug.LogWarning("[Quaker] Rules_v2.txt has " + sections.Count + " categories but the Help menu has " + rulesTabs.Count
                + " tabs. Run QUAKER > Play > Rebuild Scene Objects.");
        System.Action<int> select = i =>
        {
            rulesHeading.text = sections[i].Key;
            rulesBody.text = sections[i].Value;
            if (rulesContent != null) rulesContent.anchoredPosition = Vector2.zero;   // back to the top
            for (int k = 0; k < rulesTabs.Count; k++)
            {
                ColorBlock cb = rulesTabs[k].colors;
                cb.normalColor = k == i ? new Color(1f, 0.82f, 0.35f) : new Color(0.96f, 0.96f, 0.96f);
                cb.selectedColor = cb.normalColor;
                rulesTabs[k].colors = cb;
            }
        };
        for (int i = 0; i < rulesTabs.Count && i < sections.Count; i++)
        {
            int index = i;
            rulesTabs[i].GetComponentInChildren<TMP_Text>().text = sections[i].Key;
            rulesTabs[i].onClick.AddListener(() => select(index));
        }
        if (sections.Count > 0) select(0);
    }

    void BuildUI()
    {
        GameObject root = new GameObject("QuakerCanvas", typeof(RectTransform));
        root.transform.SetParent(transform, false);
        canvasRoot = root;
        Canvas c = root.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler cs = root.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();
        Transform t = root.transform;
        Color glass = new Color(0.06f, 0.07f, 0.09f, 0.82f);

        Image top = Panel(t, glass);
        top.name = "TopBar";
        Anchor(top.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -150), Vector2.zero);
        header = Label(top.transform, 34);
        header.name = "Header";
        header.text = "<b>QUAKER</b>";
        Anchor(header.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -62), new Vector2(-240, -10));
        prompt = Label(top.transform, 24);
        prompt.name = "Prompt";
        prompt.color = new Color(1f, 0.92f, 0.6f);
        Anchor(prompt.rectTransform, Vector2.zero, new Vector2(1, 1), new Vector2(30, 6), new Vector2(-240, -64));

        // Bottom left: players and points
        Image pl = Panel(t, glass);
        pl.name = "PlayersPanel";
        Anchor(pl.rectTransform, Vector2.zero, Vector2.zero, new Vector2(20, 20), new Vector2(460, 300));
        playersText = Label(pl.transform, 18);
        playersText.name = "Players";
        playersText.text = "<b>Turn order</b>";
        Anchor(playersText.rectTransform, Vector2.zero, Vector2.one, new Vector2(14, 8), new Vector2(-14, -8));

        // No on-screen log: every action and rule check still goes to the Console (see Add).

        // Bottom: supply (opening placement) and actions
        styleArea = Rect(t, "Supply");
        Anchor(styleArea, new Vector2(0, 0), new Vector2(1, 0), new Vector2(480, 25), new Vector2(-20, 95));
        actionArea = Rect(t, "Actions");
        Anchor(actionArea, new Vector2(0, 0), new Vector2(1, 0), new Vector2(480, 25), new Vector2(-20, 95));

        BuildCardUI(t);

        // Setup overlay
        Image setup = Panel(t, new Color(0.05f, 0.05f, 0.07f, 0.95f));
        setup.name = "Setup";
        Anchor(setup.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        setupPanel = setup.gameObject;
        TMP_Text st = Label(setup.transform, 34);
        st.name = "Title";
        st.alignment = TextAlignmentOptions.Center;
        st.text = "<size=160%><b>QUAKER</b></size>\nOne shared tower\n\n"
            + "Everyone builds on the one tower base. When it falls, whoever placed last loses that game.\n"
            + "Enter your names clockwise around the table, starting with the tallest player, who goes first in round 1. After every round the first player passes clockwise.";
        Anchor(st.rectTransform, new Vector2(0.1f, 0.62f), new Vector2(0.9f, 0.95f), Vector2.zero, Vector2.zero);

        // How many players
        countButtons.Clear();
        for (int n = 2; n <= 4; n++)
        {
            Button b = B6Button(setup.transform, n + " players", n + " players", new Vector2(0.5f, 0.6f), new Vector2((n - 3) * 230f, 0), new Vector2(210, 64));
            countButtons.Add(b);
        }

        // One row per player: turn position, name field, move up / move down
        nameFields.Clear();
        nameRows.Clear();
        downButtons.Clear();
        upButtons.Clear();
        for (int i = 0; i < 4; i++)
        {
            GameObject row = new GameObject("Player row " + (i + 1), typeof(RectTransform));
            row.transform.SetParent(setup.transform, false);
            B6Place((RectTransform)row.transform, new Vector2(0.5f, 0.6f), new Vector2(0, -95 - i * 78), new Vector2(720, 66));
            TMP_Text pos = B6Text(row.transform, "Position", (i + 1) + ".", 30, FontStyles.Bold, TextAlignmentOptions.Right, Color.white);
            B6Place(pos.rectTransform, new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(70, 60));

            GameObject fieldGo = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            fieldGo.name = "Name " + (i + 1);
            fieldGo.transform.SetParent(row.transform, false);
            B6Place((RectTransform)fieldGo.transform, new Vector2(0.5f, 0.5f), new Vector2(-20, 0), new Vector2(460, 60));
            TMP_InputField field = fieldGo.GetComponent<TMP_InputField>();
            field.characterLimit = 16;
            field.textComponent.fontSize = 28;
            field.textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            TMP_Text ph = field.placeholder as TMP_Text;
            if (ph != null) { ph.text = "Player " + (i + 1); ph.fontSize = 28; ph.alignment = TextAlignmentOptions.MidlineLeft; }
            nameFields.Add(field);

            Button up = B6Button(row.transform, "Up", "▲", new Vector2(1, 0.5f), new Vector2(-95, 0), new Vector2(60, 56));
            up.interactable = i > 0;
            upButtons.Add(up);
            Button down = B6Button(row.transform, "Down", "▼", new Vector2(1, 0.5f), new Vector2(-30, 0), new Vector2(60, 56));
            downButtons.Add(down);
            nameRows.Add(row);
        }

        Button start = B6Button(setup.transform, "StartMatch", "Start match", new Vector2(0.5f, 0.1f), Vector2.zero, new Vector2(300, 80));
        start.GetComponentInChildren<TMP_Text>().fontSize = 32;
        startButton = start;

        // Game result: a box over the fallen tower, so everyone can see what happened before the next game
        GameObject result = new GameObject("GameResult", typeof(RectTransform));
        result.transform.SetParent(t, false);
        B6Stretch((RectTransform)result.transform);
        result.AddComponent<Image>().color = new Color(0, 0, 0, 0.25f);   // blocks clicks on the table and buttons
        gameOverPanel = result;
        GameObject box = B6Box(result.transform, "Box", new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(760, 480));
        gameOverText = B6Text(box.transform, "Text", "", 28, FontStyles.Normal, TextAlignmentOptions.Top, Color.white);
        gameOverText.richText = true;
        B6Place(gameOverText.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -175), new Vector2(700, 320));
        Button next = B6Button(box.transform, "NextGame", "Next game", new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(260, 70));
        nextButton = next;
        gameOverPanel.SetActive(false);

        // Match-over overlay
        Image over = Panel(t, new Color(0.05f, 0.05f, 0.07f, 0.92f));
        over.name = "MatchOver";
        Anchor(over.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        overPanel = over.gameObject;
        overText = Label(over.transform, 36);
        overText.name = "MatchOverText";
        overText.alignment = TextAlignmentOptions.Center;
        Anchor(overText.rectTransform, new Vector2(0.1f, 0.4f), new Vector2(0.9f, 0.85f), Vector2.zero, Vector2.zero);
        Button again = MakeButton(over.transform, "New match", 32);
        Anchor((RectTransform)again.transform, new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.28f), new Vector2(-150, -45), new Vector2(150, 45));
        againButton = again;

        setupPanel.SetActive(false);
        overPanel.SetActive(false);
        BuildStartMenu(t);
    }

    // ---------------- Build 6 card UI (same layout as QUAKER > Build 6 > Set Up Build-6 Scene) ----------------

    void BuildCardUI(Transform root)
    {
        if (cardPrefab == null)
        {
            Debug.LogWarning("[Quaker] No CardUI prefab assigned; run QUAKER > Play > Create Play Scene again.");
            return;
        }
        Vector2 cardSize = ((RectTransform)cardPrefab.transform).sizeDelta;

        // Top-right buttons
        Button cardsButton = B6Button(root, "CardsButton", "Cards (C)", new Vector2(1, 1), new Vector2(-110, -50), new Vector2(180, 56));
        Button helpButton = B6Button(root, "HelpButton", "Help (H)", new Vector2(1, 1), new Vector2(-110, -120), new Vector2(180, 56));

        // Current event (left)
        B6Box(root, "EventDeckPanel", new Vector2(0, 0.5f), new Vector2(160, 10), new Vector2(290, 480));
        eventLabel = B6Text(root, "CurrentEventLabel", "Current Event", 26, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        B6Place(eventLabel.rectTransform, new Vector2(0, 0.5f), new Vector2(160, 200), new Vector2(260, 60));
        eventSlot = NewCard(root);
        eventSlot.name = "CurrentEventCard";
        B6Place((RectTransform)eventSlot.transform, new Vector2(0, 0.5f), new Vector2(160, 20), cardSize);
        eventSlot.transform.localScale = Vector3.one * 0.6f;
        eventSlot.gameObject.SetActive(false);
        drawEventButton = B6Button(root, "DrawEventButton", "Draw Event", new Vector2(0, 0.5f), new Vector2(160, -160), new Vector2(180, 56));

        // Current block (right)
        GameObject blockPanel = B6Box(root, "BlockDeckPanel", new Vector2(1, 0.5f), new Vector2(-160, 40), new Vector2(290, 600));
        Transform bp = blockPanel.transform;
        TMP_Text blockHeader = B6Text(bp, "Header", "Current Block", 26, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        B6Place(blockHeader.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 265), new Vector2(260, 40));
        blockSlot = NewCard(bp);
        blockSlot.name = "CurrentBlockCard";
        B6Place((RectTransform)blockSlot.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 85), cardSize);
        blockSlot.transform.localScale = Vector3.one * 0.6f;
        blockSlot.gameObject.SetActive(false);
        blockCurrent = B6Text(bp, "CurrentBlock", "Drawn Card: None", 22, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.82f, 0.35f));
        B6Place(blockCurrent.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -95), new Vector2(270, 36));
        blockDrawPile = B6Text(bp, "DrawPile", "Draw Pile: 0", 20, FontStyles.Normal, TextAlignmentOptions.Center, Color.white);
        B6Place(blockDrawPile.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -135), new Vector2(260, 28));
        blockDiscardPile = B6Text(bp, "DiscardPile", "Discard Pile: 0", 20, FontStyles.Normal, TextAlignmentOptions.Center, Color.white);
        B6Place(blockDiscardPile.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -168), new Vector2(260, 28));
        drawBlockButton = B6Button(bp, "DrawBlockButton", "Draw Block", new Vector2(0.5f, 0.5f), new Vector2(0, -245), new Vector2(200, 60));

        // Cards panel: all twenty event cards and the six block cards, 4 across, scrolling
        GameObject cardsPanel = B6Panel(root, "CardsPanel");
        GameObject scrollGo = new GameObject("CardScroll", typeof(RectTransform));
        scrollGo.transform.SetParent(cardsPanel.transform, false);
        B6Stretch((RectTransform)scrollGo.transform);
        ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 60;
        GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform));
        viewportGo.transform.SetParent(scrollGo.transform, false);
        RectTransform viewport = (RectTransform)viewportGo.transform;
        B6Stretch(viewport);
        viewportGo.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        viewportGo.AddComponent<RectMask2D>();
        RectTransform grid = new GameObject("CardGrid", typeof(RectTransform)).GetComponent<RectTransform>();
        grid.SetParent(viewport, false);
        grid.anchorMin = new Vector2(0, 1);
        grid.anchorMax = new Vector2(1, 1);
        grid.pivot = new Vector2(0.5f, 1);
        grid.anchoredPosition = Vector2.zero;
        grid.sizeDelta = Vector2.zero;
        GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = cardSize;
        layout.spacing = new Vector2(24, 24);
        layout.padding = new RectOffset(0, 0, 40, 40);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 4;
        grid.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport;
        scroll.content = grid;
        CardGallery gallery = cardsPanel.AddComponent<CardGallery>();
        gallery.cardPrefab = cardPrefab;
        gallery.grid = grid;
        gallery.cards = new List<CardData>(eventCards);
        gallery.cards.AddRange(blockStyles);
        Button cardsClose = B6Button(cardsPanel.transform, "CloseButton", "X", new Vector2(1, 1), new Vector2(-60, -50), new Vector2(70, 60));

        // Help panel: the rules, with a menu of categories down the left
        GameObject helpPanel = B6Panel(root, "HelpPanel");
        Button helpClose = BuildRulesBook(helpPanel.transform);

        // Card zoom: click any card to see it large
        GameObject zoomGo = new GameObject("CardZoom", typeof(RectTransform));
        zoomGo.transform.SetParent(root, false);
        B6Stretch((RectTransform)zoomGo.transform);
        GameObject zoomOverlay = B6Panel(zoomGo.transform, "ZoomOverlay");
        Button zoomClose = zoomOverlay.AddComponent<Button>();
        zoomClose.targetGraphic = zoomOverlay.GetComponent<Image>();
        zoomClose.transition = Selectable.Transition.None;
        CardView bigCard = NewCard(zoomOverlay.transform);
        bigCard.name = "ZoomedCard";
        B6Place((RectTransform)bigCard.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 25), cardSize);
        bigCard.transform.localScale = Vector3.one * 1.8f;
        TMP_Text zoomHint = B6Text(zoomOverlay.transform, "Hint", "Click anywhere or press Esc to close", 22,
            FontStyles.Italic, TextAlignmentOptions.Center, new Color(1, 1, 1, 0.8f));
        B6Place(zoomHint.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(800, 40));
        zoomOverlay.SetActive(false);
        CardZoom zoom = zoomGo.AddComponent<CardZoom>();   // Awake runs here, before the fields are set
        zoom.overlay = zoomOverlay;
        zoom.bigCard = bigCard;
        zoom.closeArea = zoomClose;
        // A saved CardZoom wires its close area in Awake; one added at Play has already run Awake without it.
        if (Application.isPlaying) zoomClose.onClick.AddListener(zoom.Close);

        // Toggles (always-active objects, so C and H work while a panel is closed)
        HelpPanel cardsToggle = new GameObject("CardsToggle").AddComponent<HelpPanel>();
        cardsToggle.transform.SetParent(root, false);
        cardsToggle.panel = cardsPanel;
        cardsToggle.openButton = cardsButton;
        cardsToggle.closeButton = cardsClose;
#if ENABLE_INPUT_SYSTEM
        cardsToggle.toggleKey = Key.C;
#endif
        HelpPanel helpToggle = new GameObject("HelpToggle").AddComponent<HelpPanel>();
        helpToggle.transform.SetParent(root, false);
        helpToggle.panel = helpPanel;
        helpToggle.openButton = helpButton;
        helpToggle.closeButton = helpClose;
#if ENABLE_INPUT_SYSTEM
        helpToggle.toggleKey = Key.H;
#endif
        // CardGallery renders in Start, the first time the panel is active. In the Editor keep the panel
        // hidden so it doesn't cover the scene; at Play it renders now and HelpPanel.Start hides it.
        cardsPanel.SetActive(Application.isPlaying);
        helpPanel.SetActive(false);
    }

    // Match over: "Main menu" beside "New match", back to the start menu. Scenes saved before this button
    // existed get it added at Play, so the saved UI doesn't need rebuilding.
    Button MatchOverMenuButton()
    {
        if (overPanel == null || againButton == null) return null;
        Transform existing = overPanel.transform.Find("MainMenu");
        if (existing != null) return existing.GetComponent<Button>();
        Button b = MakeButton(overPanel.transform, "Main menu", 32);
        b.name = "MainMenu";
        Anchor((RectTransform)againButton.transform, new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.28f), new Vector2(-330, -45), new Vector2(-30, 45));
        Anchor((RectTransform)b.transform, new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.28f), new Vector2(30, -45), new Vector2(330, 45));
        return b;
    }

    // Start menu only: a note at the foot of the rules book's category list, so players know they can read
    // the same rules once the game has started.
    void AddInGameRulesNote()
    {
        Transform menu = canvasRoot != null ? canvasRoot.transform.Find("HelpPanel/RulesSheet/RulesMenu") : null;
        if (menu == null || menu.Find("InGameNote") != null) return;
        TMP_Text note = B6Text(menu, "InGameNote", "These rules are also available during a game: press <b>H</b> or click <b>Help</b> at any time.",
            20, FontStyles.Italic, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.85f));
        note.richText = true;
        B6Place(note.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 80), new Vector2(290, 120));
    }

    // ---------------- Rules book (Help) ----------------

    // Splits the rules file at every "## Heading" line: one menu category per heading.
    static List<KeyValuePair<string, string>> ParseRules(string text)
    {
        List<KeyValuePair<string, string>> sections = new List<KeyValuePair<string, string>>();
        string title = null;
        System.Text.StringBuilder body = new System.Text.StringBuilder();
        foreach (string raw in text.Replace("\r", "").Split('\n'))
        {
            if (raw.StartsWith("## "))
            {
                if (title != null) sections.Add(new KeyValuePair<string, string>(title, body.ToString().Trim()));
                title = raw.Substring(3).Trim();
                body.Length = 0;
            }
            else if (title != null) body.AppendLine(raw);
        }
        if (title != null) sections.Add(new KeyValuePair<string, string>(title, body.ToString().Trim()));
        return sections;
    }

    // Builds the sheet inside the Help panel and returns its close button.
    Button BuildRulesBook(Transform panel)
    {
        GameObject sheet = new GameObject("RulesSheet", typeof(RectTransform));
        sheet.transform.SetParent(panel, false);
        B6Place((RectTransform)sheet.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500, 960));
        Image sheetImg = sheet.AddComponent<Image>();
        sheetImg.sprite = roundedSprite;
        sheetImg.type = roundedSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        sheetImg.color = Color.white;

        // Left: category menu
        GameObject menu = B6Box(sheet.transform, "RulesMenu", new Vector2(0, 0.5f), new Vector2(190, 0), new Vector2(340, 920));
        TMP_Text menuTitle = B6Text(menu.transform, "Title", "QUAKER RULES", 28, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        B6Place(menuTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(300, 50));

        // Right: the selected category, scrolling
        GameObject scrollGo = new GameObject("RulesScroll", typeof(RectTransform));
        scrollGo.transform.SetParent(sheet.transform, false);
        RectTransform scrollRect = (RectTransform)scrollGo.transform;
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(400, 30);
        scrollRect.offsetMax = new Vector2(-40, -30);
        ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40;
        GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform));
        viewportGo.transform.SetParent(scrollGo.transform, false);
        RectTransform viewport = (RectTransform)viewportGo.transform;
        B6Stretch(viewport);
        viewportGo.AddComponent<Image>().color = new Color(1, 1, 1, 0.01f);   // catches the mouse wheel
        viewportGo.AddComponent<RectMask2D>();

        TMP_Text heading = B6Text(viewport, "Heading", "", 36, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color(0.15f, 0.15f, 0.2f));
        TMP_Text body = B6Text(viewport, "Body", "", 24, FontStyles.Normal, TextAlignmentOptions.TopLeft, Color.black);
        body.richText = true;
        // Heading and body share one content object so they scroll together.
        RectTransform content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;   // exactly as wide as the page; the fitter sets the height
        VerticalLayoutGroup vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 18;
        vlg.padding = new RectOffset(10, 20, 10, 30);
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        heading.transform.SetParent(content, false);
        body.transform.SetParent(content, false);
        scroll.viewport = viewport;
        scroll.content = content;

        rulesHeading = heading;
        rulesBody = body;
        rulesContent = content;
        List<KeyValuePair<string, string>> sections = RuleSections();
        rulesTabs.Clear();
        float top = -100f, step = Mathf.Min(68f, 800f / Mathf.Max(1, sections.Count));
        for (int i = 0; i < sections.Count; i++)
        {
            Button tab = B6Button(menu.transform, "Tab " + sections[i].Key, sections[i].Key, new Vector2(0.5f, 1), new Vector2(0, top - i * step), new Vector2(300, step - 10));
            TMP_Text label = tab.GetComponentInChildren<TMP_Text>();
            label.fontSize = 22;
            rulesTabs.Add(tab);
        }
        if (sections.Count > 0) { heading.text = sections[0].Key; body.text = sections[0].Value; }   // so the Editor shows a page

        return B6Button(sheet.transform, "CloseButton", "X", new Vector2(1, 1), new Vector2(-50, -45), new Vector2(70, 60));
    }

    GameObject B6Box(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        B6Place((RectTransform)go.transform, anchor, pos, size);
        Image img = go.AddComponent<Image>();
        img.sprite = roundedSprite;
        img.type = roundedSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        img.color = new Color(0.10f, 0.11f, 0.14f, 0.88f);
        return go;
    }

    static GameObject B6Panel(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        B6Stretch((RectTransform)go.transform);
        go.AddComponent<Image>().color = new Color(0, 0, 0, 0.8f);
        return go;
    }

    Button B6Button(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        B6Place((RectTransform)go.transform, anchor, pos, size);
        Image img = go.AddComponent<Image>();
        img.sprite = roundedSprite;
        img.type = roundedSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        img.color = Color.white;
        Button b = go.AddComponent<Button>();
        b.targetGraphic = img;
        ColorBlock cb = b.colors;
        cb.normalColor = new Color(0.96f, 0.96f, 0.96f);
        cb.highlightedColor = new Color(1f, 0.85f, 0.45f);
        cb.pressedColor = new Color(0.85f, 0.65f, 0.25f);
        cb.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.6f);
        b.colors = cb;
        TMP_Text t = B6Text(go.transform, "Label", label, 24, FontStyles.Bold, TextAlignmentOptions.Center, Color.black);
        B6Stretch(t.rectTransform);
        return b;
    }

    static TMP_Text B6Text(Transform parent, string name, string text, float size, FontStyles style, TextAlignmentOptions align, Color color)
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

    static void B6Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void B6Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void Clear(RectTransform area)
    {
        for (int i = area.childCount - 1; i >= 0; i--) Destroy(area.GetChild(i).gameObject);
    }

    static RectTransform Rect(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
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
        t.overflowMode = TextOverflowModes.Overflow;
        t.richText = true;
        t.raycastTarget = false;
        return t;
    }

    static Button MakeButton(Transform parent, string label, float size)
    {
        Image img = Panel(parent, Color.white);
        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        TMP_Text t = Label(img.transform, size);
        t.color = Color.black;
        t.alignment = TextAlignmentOptions.Center;
        t.text = label;
        Anchor(t.rectTransform, Vector2.zero, Vector2.one, new Vector2(6, 4), new Vector2(-6, -4));
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
