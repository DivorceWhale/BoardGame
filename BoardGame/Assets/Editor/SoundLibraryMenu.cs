using UnityEditor;
using UnityEngine;

// QUAKER > Sounds: where to add the sounds you find.
public static class SoundLibraryMenu
{
    public const string LibraryPath = "Assets/Resources/" + SoundLibrary.ResourceName + ".asset";
    public const string AudioFolder = "Assets/Audio";

    [MenuItem("QUAKER/Sounds/Open Sound Library")]
    public static void Open()
    {
        SoundLibrary lib = Library();
        Selection.activeObject = lib;
        EditorGUIUtility.PingObject(lib);
    }

    [MenuItem("QUAKER/Sounds/Show Audio Folder")]
    public static void ShowFolder()
    {
        if (!AssetDatabase.IsValidFolder(AudioFolder)) AssetDatabase.CreateFolder("Assets", "Audio");
        Object folder = AssetDatabase.LoadAssetAtPath<Object>(AudioFolder);
        Selection.activeObject = folder;
        EditorGUIUtility.PingObject(folder);
    }

    // The library asset, made the first time it is asked for, with a slot for every sound.
    public static SoundLibrary Library()
    {
        SoundLibrary lib = AssetDatabase.LoadAssetAtPath<SoundLibrary>(LibraryPath);
        if (lib == null)
        {
            lib = ScriptableObject.CreateInstance<SoundLibrary>();
            AssetDatabase.CreateAsset(lib, LibraryPath);
        }
        lib.EnsureSlots();
        EditorUtility.SetDirty(lib);
        AssetDatabase.SaveAssets();
        return lib;
    }
}
