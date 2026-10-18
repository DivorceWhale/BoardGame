using UnityEditor;
using UnityEngine;

// Puts two buttons at the bottom of the Board Layout inspector.
// This file must live in a folder named exactly "Editor" (for example Assets/Editor).
[CustomEditor(typeof(BoardLayout))]
public class BoardLayoutEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BoardLayout layout = (BoardLayout)target;
        GUILayout.Space(10);

        if (GUILayout.Button("Fill 4x4 grid (verify against the foam board)"))
        {
            Undo.RecordObject(layout, "Fill 4x4 grid");
            layout.FillFourByFour();
            EditorUtility.SetDirty(layout);
            AssetDatabase.SaveAssets();
        }

        if (GUILayout.Button("Validate Board"))
        {
            layout.Validate();
        }

        EditorGUILayout.HelpBox("Validate Board writes its result to the Console window (Window > General > Console).", MessageType.Info);
    }
}
