using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Adds deck buttons to the Scenario inspector so you don't add 26 cards by hand per scenario.
// This file must live in a folder named exactly "Editor" (for example Assets/Editor).
[CustomEditor(typeof(ScenarioConfig))]
public class ScenarioConfigEditor : Editor
{
    static int totalBlocks = 30;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        ScenarioConfig cfg = (ScenarioConfig)target;

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Deck helpers", EditorStyles.boldLabel);
        totalBlocks = EditorGUILayout.IntField("Total blocks", totalBlocks);

        if (GUILayout.Button("Fill block deck (all non-event cards, split evenly)"))
        {
            Undo.RecordObject(cfg, "Fill block deck");
            FillBlocks(cfg, totalBlocks);
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
        }

        if (GUILayout.Button("Fill event deck (all cards titled Event..., 1 copy each)"))
        {
            Undo.RecordObject(cfg, "Fill event deck");
            FillEvents(cfg);
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
        }

        EditorGUILayout.HelpBox("Event cards are recognised by a title that starts with \"Event\". Check the counts in the lists above and in the Console.", MessageType.Info);
    }

    static List<CardData> AllCards()
    {
        List<CardData> list = new List<CardData>();
        foreach (string guid in AssetDatabase.FindAssets("t:CardData"))
        {
            CardData c = AssetDatabase.LoadAssetAtPath<CardData>(AssetDatabase.GUIDToAssetPath(guid));
            if (c != null) list.Add(c);
        }
        list.Sort((a, b) => string.Compare(a.title, b.title));
        return list;
    }

    static bool IsEvent(CardData c)
    {
        return c.title != null && c.title.StartsWith("Event");
    }

    static void FillBlocks(ScenarioConfig cfg, int total)
    {
        List<CardData> blocks = new List<CardData>();
        foreach (CardData c in AllCards()) if (!IsEvent(c)) blocks.Add(c);
        if (blocks.Count == 0)
        {
            Debug.LogError("No block cards found.");
            return;
        }

        cfg.blockDeck.Clear();
        int each = total / blocks.Count;
        int extra = total % blocks.Count;
        for (int i = 0; i < blocks.Count; i++)
        {
            cfg.blockDeck.Add(new DeckEntry { card = blocks[i], copies = each + (i < extra ? 1 : 0) });
        }
        Debug.Log(cfg.name + ": block deck is " + total + " cards across " + blocks.Count + " styles.");
    }

    static void FillEvents(ScenarioConfig cfg)
    {
        cfg.eventDeck.Clear();
        foreach (CardData c in AllCards())
        {
            if (IsEvent(c)) cfg.eventDeck.Add(new DeckEntry { card = c, copies = 1 });
        }
        Debug.Log(cfg.name + ": event deck is " + cfg.eventDeck.Count + " cards.");
    }
}
