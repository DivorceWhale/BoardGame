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
