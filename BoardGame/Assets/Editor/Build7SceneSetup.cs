using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

// Creates Assets/Scenes/Build-7.unity: the rule-check replay of the October 29 playtest.
// Run once from: QUAKER > Build 7 > Create Build-7 Scene.
public static class Build7SceneSetup
{
    const string ScenePath = "Assets/Scenes/Build-7.unity";

    [MenuItem("QUAKER/Build 7/Create Build-7 Scene")]
    public static void Create()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
            !EditorUtility.DisplayDialog("Build 7", "Build-7.unity already exists. Replace it?", "Replace", "Cancel")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();

        GameObject go = new GameObject("Build7RuleDemo");
        Build7RuleDemo demo = go.AddComponent<Build7RuleDemo>();
        demo.scenario = AssetDatabase.LoadAssetAtPath<ScenarioConfig>("Assets/Prefabs/Scenarios/Loose Change.asset");
        demo.towerBase = AssetDatabase.LoadAssetAtPath<BoardLayout>("Assets/Prefabs/Tower Base.asset");
        demo.lightTremor = AssetDatabase.LoadAssetAtPath<CardData>("Assets/Prefabs/Cards/Event/EC2.asset");
        if (demo.scenario == null || demo.towerBase == null || demo.lightTremor == null)
            Debug.LogWarning("Build 7: a reference is missing on Build7RuleDemo; assign it in the Inspector.");

        EditorSceneManager.SaveScene(scene, ScenePath);

        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool listed = false;
        foreach (var s in scenes) if (s.path == ScenePath) listed = true;
        if (!listed)
        {
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
        Debug.Log("Build 7: created " + ScenePath + ". Press Play, then Next step.");
    }
}
