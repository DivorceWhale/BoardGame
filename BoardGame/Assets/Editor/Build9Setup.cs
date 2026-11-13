using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Build 9: the Build-9 scene, a copy of Build-8 with the day and night cycle and the game sounds added.
public static class Build9Setup
{
    public const string Build8ScenePath = "Assets/Scenes/Build-8.unity";
    public const string Build9ScenePath = "Assets/Scenes/Build-9.unity";
    public const string MenuScenePath = "Assets/Scenes/Menu.unity";

    [MenuItem("QUAKER/Build 9/Create Build-9 Scene")]
    public static void CreateBuild9Scene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!File.Exists(Build9ScenePath)) AssetDatabase.CopyAsset(Build8ScenePath, Build9ScenePath);

        EditorSceneManager.OpenScene(Build9ScenePath, OpenSceneMode.Single);
        AddDayNightCycle();
        AddGameSounds();
        SetUpScenarios();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        // Menu: Play now opens Build-9
        if (File.Exists(MenuScenePath))
        {
            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            foreach (QuakerPlay q in Object.FindObjectsByType<QuakerPlay>(FindObjectsSortMode.None))
            {
                q.gameSceneName = "Build-9";
                EditorUtility.SetDirty(q);
            }
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        // Build Settings: Menu, Build-9, then the rest as they were (on or off)
        var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(MenuScenePath, true), new EditorBuildSettingsScene(Build9ScenePath, true) };
        foreach (var s in EditorBuildSettings.scenes)
            if (s.path != MenuScenePath && s.path != Build9ScenePath) scenes.Add(s);
        EditorBuildSettings.scenes = scenes.ToArray();

        EditorSceneManager.OpenScene(Build9ScenePath, OpenSceneMode.Single);
        Debug.Log("[Quaker] Build-9 scene ready: day and night cycle (1 minute = 1 hour) and game sounds added; the Menu's Play opens Build-9.");
    }

    public const string ScenarioFolder = "Assets/Prefabs/Scenarios";

    // The five Build 5 scenarios, in the order the setup screen shows them, and how each plays on the one shared tower.
    // The Build 5 posts were written for a tower per player; these lines are the team's shared-tower reading of them.
    static readonly string[,] SharedTower =
    {
        { "Four Corners", "Each player's opening block must cover their own gold space: the first player the centre (space 6), the others the edge spaces 1, 4 and 16." },
        { "Fuse Lit",     "Only 4 build rounds per game, counted out loud. After round 4 every round is shake-only, each shake stronger, until the tower falls." },
        { "Loose Change", "A generous supply: 10 blocks and 10 block cards of every style (60 of each), so nobody runs out." },
        { "The Sliver",   "Only a 2 x 4 strip of the tower base (spaces 1-8) is open. No part of a block may go over a grey space." },
        { "Thin Margins", "A thin margin for error: the tower collapses once a third of its tallest height has come down, not half." },
    };

    [MenuItem("QUAKER/Build 9/Set Up Scenarios")]
    public static void SetUpScenarios()
    {
        List<ScenarioConfig> ordered = new List<ScenarioConfig>();
        List<ScenarioConfig> all = new List<ScenarioConfig>();
        foreach (string guid in AssetDatabase.FindAssets("t:ScenarioConfig", new[] { ScenarioFolder }))
            all.Add(AssetDatabase.LoadAssetAtPath<ScenarioConfig>(AssetDatabase.GUIDToAssetPath(guid)));
        for (int i = 0; i < SharedTower.GetLength(0); i++)
        {
            ScenarioConfig sc = all.Find(a => a != null && a.displayName == SharedTower[i, 0]);
            if (sc == null) { Debug.LogWarning("[Quaker] No scenario named " + SharedTower[i, 0] + " in " + ScenarioFolder); continue; }
            sc.sharedTowerRule = SharedTower[i, 1];
            sc.blocksPerStyle = sc.displayName == "Loose Change" ? 10 : 0;
            sc.collapseShare = sc.displayName == "Thin Margins" ? 0.3333f : 0f;
            EditorUtility.SetDirty(sc);
            ordered.Add(sc);
        }
        AssetDatabase.SaveAssets();

        foreach (QuakerPlay q in Object.FindObjectsByType<QuakerPlay>(FindObjectsSortMode.None))
        {
            q.scenarios = new List<ScenarioConfig>(ordered);
            EditorUtility.SetDirty(q);
            EditorSceneManager.MarkSceneDirty(q.gameObject.scene);
        }
        Debug.Log("[Quaker] " + ordered.Count + " scenarios set up for the shared tower and added to the setup screen of the open scene.");
    }

    [MenuItem("QUAKER/Build 9/Select Game Sounds")]
    public static void SelectSounds()
    {
        GameSounds g = Object.FindFirstObjectByType<GameSounds>();
        if (g == null) { EditorUtility.DisplayDialog("Quaker", "Open Build-9.unity first (no GameSounds in this scene).", "OK"); return; }
        Selection.activeObject = g;
        EditorGUIUtility.PingObject(g);
    }

    static void AddDayNightCycle()
    {
        DayNightCycle d = Object.FindFirstObjectByType<DayNightCycle>();
        if (d == null) d = new GameObject("Day Night Cycle").AddComponent<DayNightCycle>();
        if (d.sun == null) d.sun = RenderSettings.sun;
        if (d.sun != null)
        {
            d.sunHeading = d.sun.transform.eulerAngles.y;   // keep the scene's sun direction
            d.dayColor = d.sun.color;
            d.dayIntensity = d.sun.intensity;
        }
        d.secondsPerGameHour = 60f;
        EditorUtility.SetDirty(d);
    }

    static void AddGameSounds()
    {
        GameSounds g = Object.FindFirstObjectByType<GameSounds>();
        if (g == null) g = new GameObject("Game Sounds").AddComponent<GameSounds>();
        g.EnsureSlots();
        EditorUtility.SetDirty(g);
    }
}
