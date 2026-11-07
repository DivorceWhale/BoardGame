using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Build 8: the balance config asset and the Build-8 scene (a copy of the Play scene that reads the asset).
public static class Build8Setup
{
    public const string BalancePath = "Assets/Resources/" + BalanceConfig.ResourceName + ".asset";
    public const string PlayScenePath = "Assets/Scenes/Play.unity";
    public const string Build8ScenePath = "Assets/Scenes/Build-8.unity";
    public const string MenuScenePath = "Assets/Scenes/Menu.unity";

    [MenuItem("QUAKER/Balance/Select Balance Config")]
    public static void SelectBalance()
    {
        BalanceConfig b = EnsureBalanceAsset();
        Selection.activeObject = b;
        EditorGUIUtility.PingObject(b);
    }

    public static BalanceConfig EnsureBalanceAsset()
    {
        BalanceConfig b = AssetDatabase.LoadAssetAtPath<BalanceConfig>(BalancePath);
        if (b != null) return b;
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        b = ScriptableObject.CreateInstance<BalanceConfig>();
        AssetDatabase.CreateAsset(b, BalancePath);
        AssetDatabase.SaveAssets();
        return b;
    }

    [MenuItem("QUAKER/Balance/Create Build-8 Scene")]
    public static void CreateBuild8Scene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        BalanceConfig b = EnsureBalanceAsset();

        if (!File.Exists(Build8ScenePath)) AssetDatabase.CopyAsset(PlayScenePath, Build8ScenePath);

        // Build-8: the game scene, wired to the balance asset
        EditorSceneManager.OpenScene(Build8ScenePath, OpenSceneMode.Single);
        foreach (QuakerPlay q in Object.FindObjectsByType<QuakerPlay>(FindObjectsSortMode.None))
        {
            q.balance = b;
            EditorUtility.SetDirty(q);
        }
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        // Menu: Play now opens Build-8, with the same balance asset for the background demo
        if (File.Exists(MenuScenePath))
        {
            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            foreach (QuakerPlay q in Object.FindObjectsByType<QuakerPlay>(FindObjectsSortMode.None))
            {
                q.balance = b;
                q.gameSceneName = "Build-8";
                EditorUtility.SetDirty(q);
            }
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        // Build Settings: Menu, Build-8, then everything else
        var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(MenuScenePath, true), new EditorBuildSettingsScene(Build8ScenePath, true) };
        foreach (var s in EditorBuildSettings.scenes)
            if (s.path != MenuScenePath && s.path != Build8ScenePath) scenes.Add(s);
        EditorBuildSettings.scenes = scenes.ToArray();

        EditorSceneManager.OpenScene(Build8ScenePath, OpenSceneMode.Single);
        Selection.activeObject = b;
        Debug.Log("[Quaker] Build-8 scene ready. Balance values: " + BalancePath);
    }
}
