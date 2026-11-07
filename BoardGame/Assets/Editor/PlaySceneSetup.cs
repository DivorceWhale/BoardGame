using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

// Creates Assets/Scenes/Play.unity: Quaker Rulesheet v2 played hotseat, with the build as game master.
// Run once from: QUAKER > Play > Create Play Scene.
public static class PlaySceneSetup
{
    const string ScenePath = "Assets/Scenes/Play.unity";

    static readonly string[] BlockStyles = { "The Cube", "Long Block", "L-Block", "BL-Block", "T-Block", "Z-Block" };

    [MenuItem("QUAKER/Play/Create Play Scene")]
    public static void Create()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
            !EditorUtility.DisplayDialog("Quaker", "Play.unity already exists. Replace it?", "Replace", "Cancel")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();

        GameObject go = new GameObject("QuakerPlay");
        QuakerPlay play = go.AddComponent<QuakerPlay>();
        play.towerBase = AssetDatabase.LoadAssetAtPath<BoardLayout>("Assets/Prefabs/Tower Base.asset");
        play.towerBasePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tower Base.prefab");
        if (play.towerBasePrefab == null) Debug.LogWarning("Play scene: Tower Base.prefab is missing; assign it on QuakerPlay.");

        foreach (string style in BlockStyles)
        {
            CardData c = AssetDatabase.LoadAssetAtPath<CardData>("Assets/Prefabs/Cards/Blocks/" + style + ".asset");
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Blocks/" + style + ".prefab");
            if (c == null || model == null)
            {
                Debug.LogWarning("Play scene: block style " + style + " is missing its card asset or its 3D prefab.");
                continue;
            }
            play.blockStyles.Add(c);
            play.blockPrefabs.Add(model);
        }
        for (int i = 1; i <= 20; i++)
        {
            CardData c = AssetDatabase.LoadAssetAtPath<CardData>("Assets/Prefabs/Cards/Event/EC" + i + ".asset");
            if (c == null) Debug.LogWarning("Play scene: event card asset missing: EC" + i);
            else play.eventCards.Add(c);
        }
        if (play.towerBase == null) Debug.LogWarning("Play scene: Tower Base.asset is missing; assign it on QuakerPlay.");

        // Build 6 card UI
        play.cardPrefab = AssetDatabase.LoadAssetAtPath<CardView>("Assets/Prefabs/Cards/CardUI.prefab");
        play.rulesText = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Art/Rules_v2.txt");
        play.roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        if (play.cardPrefab == null) Debug.LogWarning("Play scene: CardUI.prefab is missing; run QUAKER > Build 6 > Create Card UI Prefab.");

        BuildSceneObjects(play);
        EditorSceneManager.SaveScene(scene, ScenePath);

        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool listed = false;
        foreach (var s in scenes) if (s.path == ScenePath) listed = true;
        if (!listed)
        {
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
        Debug.Log("Quaker: created " + ScenePath + " with " + play.blockStyles.Count + " block styles and "
            + play.eventCards.Count + " event cards. Press Play.");
    }

    // Rebuilds the shake board, Tower Base, colliders, table and the whole UI in the open Play scene, and saves it.
    // Run it again after changing Tower Base.prefab or adding a rules category. It replaces the old objects,
    // so edits you made to them in the scene are lost.
    [MenuItem("QUAKER/Play/Rebuild Scene Objects")]
    public static void RebuildSceneObjects()
    {
        QuakerPlay play = Object.FindFirstObjectByType<QuakerPlay>();
        if (play == null)
        {
            EditorUtility.DisplayDialog("Quaker", "Open Play.unity first (no QuakerPlay in this scene).", "OK");
            return;
        }
        BuildSceneObjects(play);
        EditorSceneManager.MarkSceneDirty(play.gameObject.scene);
        EditorSceneManager.SaveScene(play.gameObject.scene);
        Selection.activeGameObject = play.shakeBoard != null ? play.shakeBoard.gameObject : play.gameObject;
        Debug.Log("Quaker: rebuilt the Shake Board, Tower Base, Table and UI in " + play.gameObject.scene.path + ".");
    }

    // Plays 10 test games with computer players at the current shake settings (Play mode only).
    // The report goes to the Console and to Logs/QuakerShakeTests.txt.
    [MenuItem("QUAKER/Play/Run Shake Tests")]
    public static void RunShakeTests()
    {
        QuakerPlay play = Object.FindFirstObjectByType<QuakerPlay>();
        if (!EditorApplication.isPlaying || play == null)
        {
            EditorUtility.DisplayDialog("Quaker", "Open Play.unity and press Play first, then run the shake tests (before starting a match).", "OK");
            return;
        }
        play.RunShakeTests();
        Debug.Log("Quaker: shake tests started. They take a few minutes; the report appears in the Console when they finish.");
    }

    const string MenuScenePath = "Assets/Scenes/Menu.unity";

    // Makes Assets/Scenes/Menu.unity from Play.unity: the same tower base, table, camera and UI, with
    // Menu Scene ticked so it opens on the start menu. Play.unity loses its start menu and opens on setup.
    // Build Settings: Menu first (the game starts there), then Play.
    [MenuItem("QUAKER/Play/Create Menu Scene")]
    public static void CreateMenuScene() { CreateMenuScene(true); }

    public static void CreateMenuScene(bool askBeforeReplacing)
    {
        if (askBeforeReplacing && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
        {
            EditorUtility.DisplayDialog("Quaker", "Create Play.unity first (QUAKER > Play > Create Play Scene).", "OK");
            return;
        }
        if (askBeforeReplacing && AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath) != null &&
            !EditorUtility.DisplayDialog("Quaker", "Menu.unity already exists. Replace it?", "Replace", "Cancel")) return;

        // Play.unity: make sure it has the menu (so the copy does), then copy it
        var play = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        QuakerPlay q = Object.FindFirstObjectByType<QuakerPlay>();
        if (q.transform.Find("QuakerCanvas/StartMenu") == null) q.AddStartMenuToScene();
        EditorSceneManager.SaveScene(play);
        AssetDatabase.DeleteAsset(MenuScenePath);
        AssetDatabase.CopyAsset(ScenePath, MenuScenePath);

        // Menu.unity: opens on the start menu
        var menu = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        q = Object.FindFirstObjectByType<QuakerPlay>();
        q.StripForMenu();   // only the menu, How to Play and Cards stay
        q.gameObject.name = "QuakerMenu";
        EditorUtility.SetDirty(q);
        EditorSceneManager.MarkSceneDirty(menu);
        EditorSceneManager.SaveScene(menu);

        // Play.unity: no start menu; setup's Back button goes to Menu.unity
        play = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        q = Object.FindFirstObjectByType<QuakerPlay>();
        q.menuScene = false;
        Transform startMenu = q.transform.Find("QuakerCanvas/StartMenu");
        if (startMenu != null) Object.DestroyImmediate(startMenu.gameObject);
        EditorUtility.SetDirty(q);
        EditorSceneManager.MarkSceneDirty(play);
        EditorSceneManager.SaveScene(play);

        // Build Settings: Menu first, then Play, then anything else already listed
        var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(MenuScenePath, true), new EditorBuildSettingsScene(ScenePath, true) };
        foreach (var s in EditorBuildSettings.scenes)
            if (s.path != MenuScenePath && s.path != ScenePath) scenes.Add(s);
        EditorBuildSettings.scenes = scenes.ToArray();

        EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        Debug.Log("Quaker: created " + MenuScenePath + " (opens on the start menu) and removed the menu from " + ScenePath + ". Build Settings: Menu, then Play.");
    }

    // Plays 10 full matches through the real game and checks the rules, cards, turns, scores and screen
    // (Play mode, Play.unity). The report goes to the Console and to Logs/QuakerGameTests.txt.
    [MenuItem("QUAKER/Play/Run Game Tests")]
    public static void RunGameTests()
    {
        QuakerPlay play = Object.FindFirstObjectByType<QuakerPlay>();
        if (!EditorApplication.isPlaying || play == null || play.menuScene)
        {
            EditorUtility.DisplayDialog("Quaker", "Open Play.unity and press Play first, then run the game tests (on the setup screen).", "OK");
            return;
        }
        play.RunGameTests();
        Debug.Log("Quaker: game tests started. They take several minutes; the report appears in the Console when they finish.");
    }

    const string GripPath = "Assets/Prefabs/Grip.physicsMaterial";
    const string TableMatPath = "Assets/Art/Table.mat";

    static void BuildSceneObjects(QuakerPlay play)
    {
        PhysicsMaterial grip = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(GripPath);
        if (grip == null)
        {
            grip = new PhysicsMaterial("Grip") { staticFriction = 0.7f, dynamicFriction = 0.6f, bounciness = 0f };
            AssetDatabase.CreateAsset(grip, GripPath);
        }
        Material tableMat = AssetDatabase.LoadAssetAtPath<Material>(TableMatPath);
        if (tableMat == null)
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) lit = Shader.Find("Standard");
            tableMat = new Material(lit) { name = "Table" };
            Color wood = new Color(0.35f, 0.27f, 0.2f);
            if (tableMat.HasProperty("_BaseColor")) tableMat.SetColor("_BaseColor", wood);
            if (tableMat.HasProperty("_Color")) tableMat.SetColor("_Color", wood);
            AssetDatabase.CreateAsset(tableMat, TableMatPath);
        }
        play.grip = grip;
        play.tableMaterial = tableMat;
        System.Func<GameObject, Transform, GameObject> spawn = (prefab, parent) => (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        play.CreateWorldObjects(spawn);
        play.CreateUIObjects(spawn);
        play.PreviewInEditor();
        EditorUtility.SetDirty(play);
        AssetDatabase.SaveAssets();
    }
}
