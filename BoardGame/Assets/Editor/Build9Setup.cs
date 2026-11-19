using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Build 9 scenes:
//   Build-9o: a copy of Build-8 with the game sounds and the scenario picker (the Build 5 scenarios).
//   Build-9:  a copy of Build-9o without the scenario picker. The Menu's Play opens this one.
public static class Build9Setup
{
    public const string Build8ScenePath = "Assets/Scenes/Build-8.unity";
    public const string Build9ScenePath = "Assets/Scenes/Build-9o.unity";   // with scenarios
    public const string MenuScenePath = "Assets/Scenes/Menu.unity";

    [MenuItem("QUAKER/Build 9/Create Build-9o Scene (with scenarios)")]
    public static void CreateBuild9Scene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!File.Exists(Build9ScenePath)) AssetDatabase.CopyAsset(Build8ScenePath, Build9ScenePath);

        EditorSceneManager.OpenScene(Build9ScenePath, OpenSceneMode.Single);
        AddGameSounds();
        SetUpScenarios();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        // Menu: Play opens Build-9 (the copy without scenarios)
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
        Debug.Log("[Quaker] Build-9o scene ready: game sounds and scenarios added. The Menu's Play opens Build-9.");
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

    public const string NoScenariosScenePath = "Assets/Scenes/Build-9.unity";   // without scenarios

    // Build-9: a copy of Build-9o with the scenario list emptied, so the setup screen has no scenario picker and every
    // match plays the rulebook as printed. Build-9o keeps the scenarios.
    [MenuItem("QUAKER/Build 9/Create Build-9 Scene (without scenarios)")]
    public static void CreateNoScenariosScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!File.Exists(NoScenariosScenePath)) AssetDatabase.CopyAsset(Build9ScenePath, NoScenariosScenePath);
        EditorSceneManager.OpenScene(NoScenariosScenePath, OpenSceneMode.Single);
        foreach (QuakerPlay q in Object.FindObjectsByType<QuakerPlay>(FindObjectsSortMode.None))
        {
            q.scenarios = new List<ScenarioConfig>();
            EditorUtility.SetDirty(q);
        }
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        // Build Settings: add it after Build-9o
        var scenes = new List<EditorBuildSettingsScene>();
        bool added = false;
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.path == NoScenariosScenePath) continue;
            scenes.Add(s);
            if (s.path == Build9ScenePath) { scenes.Add(new EditorBuildSettingsScene(NoScenariosScenePath, true)); added = true; }
        }
        if (!added) scenes.Add(new EditorBuildSettingsScene(NoScenariosScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("[Quaker] Build-9 ready: a copy of Build-9o with no scenario picker.");
    }

    // The sounds live in the Sound Library asset now (QUAKER > Sounds > Open Sound Library); GameSounds starts by
    // itself in every scene, so a scene doesn't need its own.
    static void AddGameSounds()
    {
        SoundLibraryMenu.Library();
    }
}
