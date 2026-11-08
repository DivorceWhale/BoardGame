using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Dev tool: plays whole games of Quaker with computer players to check the shake settings.
// Run it from QUAKER > Play > Run Shake Tests while in Play mode. The report goes to the Console and to
// Logs/QuakerShakeTests.txt.
//
// Each test is a real game: the opening placement, then rounds where every player places the block style
// they draw (5 of each style, 30 cards, no reshuffle), then the shake, then the check. Players use the same
// aiming rules as people do (snap to the 4x4 grid, all 12 ways a block can face, on top or tucked under an
// overhang) and each has a habit:
//   Careful - keeps the tower low and wide.
//   Builder - stacks on blocks near the middle.
//   Random  - any legal spot, any way up.
//   Risky   - goes for the highest spot, blocks stood on end.
public partial class QuakerPlay
{
    [Header("Shake tests (QUAKER > Play > Run Shake Tests, in Play mode)")]
    [Tooltip("Runs the test games faster than real time. Physics steps are the same, so results don't change.")]
    public float shakeTestTimeScale = 4f;
    [HideInInspector] public bool shakeTestRunning;
    [HideInInspector] public string shakeTestReport = "";

    enum Bot { Careful, Builder, Random, Risky, Showcase }   // Showcase: the start menu's tidy, stable tower

    class TestGame
    {
        public string name;
        public int seed;
        public Bot[] players;
        public Dictionary<int, int> events = new Dictionary<int, int>();   // round -> event 15 (double speed) or 19 (two shakes)
    }

    class TestResult
    {
        public string name;
        public int rounds, blocksPlaced, fellPlacing, fellShaking, maxOnTower, collapseRound;
        public float maxHeight;
        public string ending;
        public List<string> fallsByRound = new List<string>();
    }

    static List<TestGame> ShakeTestGames()
    {
        return new List<TestGame>
        {
            new TestGame { name = "1. Two careful players", seed = 1, players = new[] { Bot.Careful, Bot.Careful } },
            new TestGame { name = "2. Builder vs random", seed = 2, players = new[] { Bot.Builder, Bot.Random } },
            new TestGame { name = "3. Three mixed players", seed = 3, players = new[] { Bot.Careful, Bot.Builder, Bot.Random } },
            new TestGame { name = "4. Three builders (tall middle tower)", seed = 4, players = new[] { Bot.Builder, Bot.Builder, Bot.Builder } },
            new TestGame { name = "5. Four mixed players", seed = 5, players = new[] { Bot.Careful, Bot.Random, Bot.Builder, Bot.Random } },
            new TestGame { name = "6. Four players, one risky", seed = 6, players = new[] { Bot.Risky, Bot.Builder, Bot.Careful, Bot.Random } },
            new TestGame { name = "7. Three random players", seed = 7, players = new[] { Bot.Random, Bot.Random, Bot.Random } },
            new TestGame { name = "8. Two risky players", seed = 8, players = new[] { Bot.Risky, Bot.Risky } },
            new TestGame { name = "9. Four builders", seed = 9, players = new[] { Bot.Builder, Bot.Builder, Bot.Builder, Bot.Builder } },
            new TestGame
            {
                name = "10. Three mixed players with events 15 and 19", seed = 10,
                players = new[] { Bot.Builder, Bot.Random, Bot.Careful },
                events = new Dictionary<int, int> { { 3, 15 }, { 5, 19 }, { 7, 15 } }
            },
        };
    }

    public void RunShakeTests()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[Quaker] The shake tests only run in Play mode. Press Play first."); return; }
        if (shakeTestRunning) return;
        StopDemo();
        if (startMenu != null) startMenu.SetActive(false);
        StartCoroutine(ShakeTests());
    }

    IEnumerator ShakeTests()
    {
        shakeTestRunning = true;
        shakeTestReport = "";
        if (setupPanel != null) setupPanel.SetActive(false);
        float oldScale = Time.timeScale;
        Time.timeScale = shakeTestTimeScale;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("QUAKER shake tests: strength " + shakeStrength + " x gravity, " + shakeHz + " Hz, " + shakeSeconds + " s per shake, ramp " + shakeRamp + " s");
        sb.AppendLine("Shake-only rounds: +" + shakeOnlyStep + " strength each, up to " + shakeMaxStrength + ".");
        sb.AppendLine("Collapse: " + Mathf.RoundToInt(collapseShare * 100) + "% of the game's tallest tower (and at least " + collapseMinBlocks + " blocks) has come down, counted over the whole game.");
        sb.AppendLine();
        List<TestResult> results = new List<TestResult>();
        foreach (TestGame g in ShakeTestGames())
        {
            TestResult r = new TestResult { name = g.name };
            results.Add(r);
            yield return RunTestGame(g, r, sb);
        }

        sb.AppendLine("SUMMARY");
        foreach (TestResult r in results)
            sb.AppendLine("  " + r.name + ": " + r.rounds + " rounds, " + r.blocksPlaced + " placed, most on tower " + r.maxOnTower
                + ", tallest " + r.maxHeight.ToString("0") + " (" + (r.maxHeight / cell).ToString("0.0") + " blocks), fell while placing " + r.fellPlacing
                + ", fell off in shakes " + r.fellShaking + ". " + r.ending);

        Time.timeScale = oldScale;
        ClearBlocks();
        if (setupPanel != null) setupPanel.SetActive(true);
        shakeTestReport = sb.ToString();
        Debug.Log(shakeTestReport);
        try
        {
            string path = System.IO.Path.Combine(Application.dataPath, "..", "Logs", "QuakerShakeTests.txt");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            System.IO.File.WriteAllText(path, shakeTestReport);
        }
        catch (System.Exception e) { Debug.LogWarning("Shake tests: could not write the report file: " + e.Message); }
        shakeTestRunning = false;
    }

    IEnumerator RunTestGame(TestGame g, TestResult r, StringBuilder sb)
    {
        ClearBlocks();
        yield return new WaitForFixedUpdate();
        System.Random rnd = new System.Random(g.seed);
        int[] supply = new int[blockStyles.Count];
        List<int> deck = new List<int>();
        for (int k = 0; k < blockStyles.Count; k++)
        {
            supply[k] = blockStyles[k].copies;   // copies live on each block card
            for (int n = 0; n < supply[k]; n++) deck.Add(k);
        }
        for (int i = deck.Count - 1; i > 0; i--) { int j = rnd.Next(i + 1); int t = deck[i]; deck[i] = deck[j]; deck[j] = t; }

        List<string> who = new List<string>();
        foreach (Bot b in g.players) who.Add(b.ToString());
        sb.AppendLine(g.name + "  (" + string.Join(", ", who.ToArray()) + ", seed " + g.seed + ")");

        int[] fell = new int[3];   // [0] fell off the base, [1] came down, [2] blocks on the tower before
        bool over = false;
        int lastPlacer = -1;

        // Opening placement: each player picks any style from the supply.
        int openingFell = 0;
        for (int p = 0; p < g.players.Length && !over; p++)
        {
            List<int> left = new List<int>();
            for (int k = 0; k < supply.Length; k++) if (supply[k] > 0) left.Add(k);
            int style = left[rnd.Next(left.Count)];
            if (g.players[p] == Bot.Careful && supply[blockStyles.FindIndex(c => c.title == "The Cube")] > 0)
                style = blockStyles.FindIndex(c => c.title == "The Cube");   // careful players open with the flat 2x2
            supply[style]--;
            r.blocksPlaced++;
            lastPlacer = p;
            yield return BotPlace(blockStyles[style], g.players[p], rnd, fell);
            openingFell += fell[0];
            r.fellPlacing += fell[0];
            if (placed.Count == 0 || IsCollapse(fell[1], fell[2]))
            {
                over = true;
                r.ending = "Collapsed during the opening (" + fell[1] + " of " + fell[2] + " came down; player " + (p + 1) + " loses).";
            }
        }
        sb.AppendLine("  Opening: " + g.players.Length + " placed, " + openingFell + " fell off" + Stage());

        int shakeOnly = 0;
        for (int round = 1; !over && round <= 40; round++)
        {
            r.rounds = round;
            bool deckShort = deck.Count < g.players.Length;
            int placedNow = 0, skipped = 0, fellPlacing = 0;
            if (deckShort) shakeOnly++;
            else
            {
                for (int p = 0; p < g.players.Length && !over; p++)
                {
                    int style = deck[0];
                    deck.RemoveAt(0);
                    if (supply[style] <= 0) { skipped++; continue; }   // section 7: none of that style left
                    supply[style]--;
                    r.blocksPlaced++;
                    placedNow++;
                    lastPlacer = p;
                    yield return BotPlace(blockStyles[style], g.players[p], rnd, fell);
                    fellPlacing += fell[0];
                    r.fellPlacing += fell[0];
                    if (placed.Count == 0 || IsCollapse(fell[1], fell[2]))
                    {
                        over = true;
                        r.collapseRound = round;
                        r.ending = "COLLAPSED while player " + (p + 1) + " (" + g.players[p] + ") was placing in round " + round + ": "
                            + fell[1] + " of " + fell[2] + " blocks came down. Player " + (p + 1) + " loses.";
                    }
                }
            }
            r.maxOnTower = Mathf.Max(r.maxOnTower, placed.Count);
            r.maxHeight = Mathf.Max(r.maxHeight, TowerHeight());
            if (over) { sb.AppendLine("  Round " + round + ": the tower collapsed while placing." + Stage()); break; }

            int ev = 0;
            g.events.TryGetValue(round, out ev);
            int before = placed.Count;
            float h = TowerHeight();
            shakeExtra = deckShort ? ShakeOnlyStrength(shakeOnly) - shakeStrength : 0f;   // same escalation as the game
            yield return TestShake(ev == 5 || ev == 10 || ev == 19 ? B.doubleShakeCount : 1, ev == 15 ? B.fastShakeSpeed : 1f, fell);   // same values as the game
            shakeExtra = 0f;
            r.fellShaking += fell[0];
            if (fell[0] > 0) r.fallsByRound.Add("round " + round + ": " + fell[0]);
            sb.AppendLine("  Round " + round + (deckShort ? " (deck short, shake-only round " + shakeOnly + ", strength " + ShakeOnlyStrength(shakeOnly).ToString("0.00") + ")" : "") + (ev > 0 ? " [event " + ev + "]" : "") + ": placed " + placedNow
                + (skipped > 0 ? " (" + skipped + " skipped, none left)" : "") + (fellPlacing > 0 ? ", " + fellPlacing + " fell off while placing" : "")
                + "; tower " + before + " blocks, " + (h / cell).ToString("0.0") + " blocks tall; shake knocked " + fell[0] + " off the base; " + fell[1] + " of " + fell[2] + " down this game" + Stage());
            if (placed.Count == 0 || IsCollapse(fell[1], fell[2]))
            {
                over = true;
                r.collapseRound = round;
                r.ending = "COLLAPSED in the round " + round + " shake: " + fell[1] + " of " + fell[2] + " blocks came down. Player " + (lastPlacer + 1)
                    + " (" + g.players[lastPlacer] + ") placed last and loses.";
            }
            if (!over && shakeOnly >= 12)
            {
                r.ending = "Deck ran out; tower still standing (" + placed.Count + " blocks) after 12 shake-only rounds.";
                break;
            }
        }
        if (string.IsNullOrEmpty(r.ending)) r.ending = "Stopped after 40 rounds with " + placed.Count + " blocks standing.";
        sb.AppendLine("  Result: " + r.ending);
        sb.AppendLine();
    }

    string Stage()
    {
        return "  [now " + placed.Count + " on tower, " + setAside.Count + " set aside]";
    }

    float TowerHeight()
    {
        float top = baseTop;
        foreach (Rigidbody rb in placed) if (rb != null) top = Mathf.Max(top, RenderBounds(rb.gameObject).max.y);
        return top - baseTop;
    }

    // The 12 ways the bots try a block: 4 turns, each flat, tipped up, or rolled on its side.
    static List<Quaternion> TestTurns()
    {
        List<Quaternion> list = new List<Quaternion>();
        Quaternion[] tips = { Quaternion.identity, Quaternion.AngleAxis(90, Vector3.right), Quaternion.AngleAxis(90, Vector3.forward) };
        foreach (Quaternion tip in tips)
            for (int y = 0; y < 4; y++) list.Add(SnapRotation(Quaternion.AngleAxis(90 * y, Vector3.up) * tip));
        return list;
    }

    struct Candidate
    {
        public Quaternion turn;
        public Vector3 position;   // where the block's pivot goes
        public float top, bottom, centreDistance, height;
        public bool onBlock;
        public int unsupported;    // Showcase only: cubes with nothing directly under them
        public bool balanced;      // Showcase only: the block's weight is over the cubes that hold it up
    }

    // Showcase: how well a block resting with its underside at `bottom`, footprint centred at c, is held up.
    // A cube is held up when it sits on the tower base, on a block, or on another cube of the same block.
    // unsupported = cubes resting on nothing; balanced = the block's centre is over the held-up cubes.
    void Support(List<Bounds> cubes, Vector3 c, float bottom, out int unsupported, out bool balanced)
    {
        unsupported = 0;
        float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
        float comX = 0f, comZ = 0f;
        foreach (Bounds cube in cubes)
        {
            float cx = c.x + cube.center.x, cz = c.z + cube.center.z;
            comX += cx; comZ += cz;
            float cubeBottom = bottom + cube.min.y;
            bool held = Mathf.Abs(cubeBottom - baseTop) < 0.3f;
            if (!held)
                foreach (Bounds other in cubes)   // on top of a cube of the same block
                    if (Mathf.Abs(bottom + other.max.y - cubeBottom) < 0.3f && Mathf.Abs(other.center.x - cube.center.x) < 1f && Mathf.Abs(other.center.z - cube.center.z) < 1f)
                    { held = true; break; }
            bool onSomething = Mathf.Abs(cubeBottom - baseTop) < 0.3f;
            if (!onSomething)
                foreach (Rigidbody rb in placed)
                {
                    if (rb == null) continue;
                    foreach (Collider col in rb.GetComponentsInChildren<Collider>())
                    {
                        Bounds cb = col.bounds;
                        if (Mathf.Abs(cb.max.y - cubeBottom) < 0.5f
                            && Mathf.Abs(cx - cb.center.x) < cube.extents.x + cb.extents.x - 0.3f
                            && Mathf.Abs(cz - cb.center.z) < cube.extents.z + cb.extents.z - 0.3f) { onSomething = true; break; }
                    }
                    if (onSomething) break;
                }
            if (onSomething)
            {
                minX = Mathf.Min(minX, cx - cube.extents.x); maxX = Mathf.Max(maxX, cx + cube.extents.x);
                minZ = Mathf.Min(minZ, cz - cube.extents.z); maxZ = Mathf.Max(maxZ, cz + cube.extents.z);
            }
            else if (!held) unsupported++;
        }
        comX /= Mathf.Max(1, cubes.Count); comZ /= Mathf.Max(1, cubes.Count);
        balanced = minX <= maxX && comX > minX + 0.4f && comX < maxX - 0.4f && comZ > minZ + 0.4f && comZ < maxZ - 0.4f;
    }

    // One bot placement: find every legal spot, pick one by the bot's habit, drop the block, let it settle,
    // and set aside anything that fell. fell[0] = blocks that came off.
    // lowerFrom > 0: the block is lowered from that far above its spot before it is let go (start menu demo).
    IEnumerator BotPlace(CardData style, Bot bot, System.Random rnd, int[] fell, float lowerFrom = 0f)
    {
        fell[0] = fell[1] = fell[2] = 0;
        List<Candidate> options = new List<Candidate>();
        GameObject probe = SpawnBlock(style, true);
        GameObject oldGhost = ghost;
        ghost = probe;
        foreach (Quaternion turn in TestTurns())
        {
            probe.transform.rotation = turn;
            Bounds gb = RenderBounds(probe);
            Vector3 pivotOffset = probe.transform.position - gb.center;
            int wx = Mathf.Max(1, Mathf.RoundToInt(gb.size.x / cell));
            int wz = Mathf.Max(1, Mathf.RoundToInt(gb.size.z / cell));
            if (wx > 4 || wz > 4) continue;
            List<Bounds> cubes = null;
            if (bot == Bot.Showcase)
            {
                cubes = new List<Bounds>();
                foreach (Renderer r in probe.GetComponentsInChildren<Renderer>())
                {
                    Bounds b = r.bounds;
                    b.center = new Vector3(b.center.x - gb.center.x, b.center.y - gb.min.y, b.center.z - gb.center.z);
                    cubes.Add(b);
                }
            }
            for (int sx = 0; sx + wx <= 4; sx++)
                for (int sz = 0; sz + wz <= 4; sz++)
                {
                    Vector3 c = new Vector3(gridMin.x + (sx + wx * 0.5f) * cell, 0f, gridMin.z + (sz + wz * 0.5f) * cell);
                    foreach (Level lv in RestingLevels(gb, c))
                    {
                        Candidate o = new Candidate
                        {
                            turn = turn,
                            position = new Vector3(c.x, lv.bottom + gb.extents.y + 0.15f, c.z) + pivotOffset,
                            bottom = lv.bottom,
                            top = lv.bottom + gb.size.y,
                            height = gb.size.y,
                            onBlock = lv.onBlock,
                            centreDistance = new Vector2(c.x - baseCentre.x, c.z - baseCentre.z).magnitude
                        };
                        if (cubes != null) Support(cubes, c, lv.bottom, out o.unsupported, out o.balanced);
                        options.Add(o);
                    }
                }
        }
        ghost = oldGhost;
        Destroy(probe);
        if (options.Count == 0) yield break;

        Candidate pick = options[0];
        float best = float.MaxValue;
        if (bot == Bot.Showcase) pick = ShowcasePick(options, rnd);
        else foreach (Candidate o in options)
        {
            float noise = (float)rnd.NextDouble();
            float score;
            switch (bot)
            {
                case Bot.Careful: score = o.top + noise * 2f + o.centreDistance * 0.05f; break;                         // low and wide
                case Bot.Builder: score = (o.onBlock ? 0f : 20f) + o.centreDistance * 1.5f + o.bottom * 0.3f + noise * 6f; break; // stack in the middle
                case Bot.Risky: score = -o.top + noise * 3f; break;                                                 // as high as it goes
                default: score = noise; break;                                                                      // anything legal
            }
            if (score < best) { best = score; pick = o; }
        }

        Dictionary<Rigidbody, float> before = TowerSnapshot(null);
        GameObject block = SpawnBlock(style, false);
        block.transform.rotation = pick.turn;
        block.transform.position = pick.position;
        if (lowerFrom > 0f)
        {
            // Hand-lower it like a player would, without physics, so it can't knock anything on the way down.
            for (float t = 0f; t < 1f && block != null; t += Time.deltaTime / 0.6f)
            {
                float e = 1f - (1f - t) * (1f - t);
                block.transform.position = pick.position + Vector3.up * lowerFrom * (1f - e);
                yield return null;
            }
            if (block == null) yield break;
            block.transform.position = pick.position;
        }
        Rigidbody rb = block.AddComponent<Rigidbody>();
        rb.mass = blockMass;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        PhysicsMaterial blockGrip = new PhysicsMaterial("Block") { staticFriction = 0.7f, dynamicFriction = 0.6f, bounciness = 0f };
        foreach (Collider col in block.GetComponentsInChildren<Collider>()) col.sharedMaterial = blockGrip;
        placed.Add(rb);

        yield return SettleAndSetAside(fell, before);
    }

    // Showcase: build a tidy stepped tower. Every cube must rest on something and the block's weight must sit
    // over what holds it up; fill the lower layers from the middle outward, pull each higher layer further in,
    // and prefer blocks lying flat. Picks at random among the 3 best spots so every tower looks different.
    Candidate ShowcasePick(List<Candidate> options, System.Random rnd)
    {
        List<KeyValuePair<float, int>> scored = new List<KeyValuePair<float, int>>();
        for (int i = 0; i < options.Count; i++)
        {
            Candidate o = options[i];
            float layer = (o.bottom - baseTop) / cell;
            float score = o.unsupported * 60f + (o.balanced ? 0f : 45f)
                + layer * 1.6f                                   // fill low before going high
                + o.centreDistance * (0.55f + layer * 0.35f)     // higher layers pulled toward the middle
                + (o.height > cell * 1.2f ? 5f : 0f)             // lying flat looks steadier than standing up
                + (float)rnd.NextDouble() * 2.5f;
            scored.Add(new KeyValuePair<float, int>(score, i));
        }
        scored.Sort((a, b) => a.Key.CompareTo(b.Key));
        int k = Mathf.Min(3, scored.Count);
        return options[scored[rnd.Next(k)].Value];
    }

    IEnumerator TestShake(int count, float speed, int[] fell)
    {
        Dictionary<Rigidbody, float> before = TowerSnapshot(null);
        for (int i = 0; i < count; i++)
        {
            shakeSpeed = speed;
            shakeStartedAt = Time.time;
            shakeEndsAt = Time.time + shakeSeconds;
            shaking = true;
            while (Time.time < shakeEndsAt) yield return null;
            shaking = false;
            if (i < count - 1) yield return new WaitForSeconds(0.8f);
        }
        shakeSpeed = 1f;
        yield return SettleAndSetAside(fell, before);
    }

    // Same as the game: wait for the blocks to stop, then set aside every block that dropped off the base.
    IEnumerator SettleAndSetAside(int[] fell, Dictionary<Rigidbody, float> before)
    {
        fell[0] = 0;
        float t0 = Time.time;
        yield return new WaitForSeconds(0.6f);
        while (Time.time - t0 < settleTimeout && AnyMoving()) yield return null;
        int blocksBefore = placed.Count;
        for (int i = placed.Count - 1; i >= 0; i--)
        {
            if (OnTower(placed[i])) continue;
            fell[0]++;
            SetAside(placed[i]);
            placed.RemoveAt(i);
        }
        RecordTower(blocksBefore);
        fell[1] = BlocksDown();   // the same rule as the game: half of this game's tallest tower
        fell[2] = towerPeak;
    }
}
