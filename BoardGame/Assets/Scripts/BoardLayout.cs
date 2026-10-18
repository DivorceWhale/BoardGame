using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SpaceData
{
    public int id;
    public string displayName;
    [Tooltip("Where the space sits when the board is drawn. Re-read from the foam board.")]
    public Vector2Int gridPosition;
    [Tooltip("Ids of the spaces connected to this one, as on the foam board now.")]
    public List<int> neighbors = new List<int>();
    [Tooltip("Labels such as edge, corner, center. Scenarios use them (for example a harder shake for edge towers).")]
    public List<string> tags = new List<string>();
}

// The repaired foam board, re-counted and re-derived. Replace this data; do not patch the old board.
[CreateAssetMenu(fileName = "Board", menuName = "QUAKER/Board Layout")]
public class BoardLayout : ScriptableObject
{
    public List<SpaceData> spaces = new List<SpaceData>();

    public bool HasTag(int id, string tag)
    {
        SpaceData s = Get(id);
        return s != null && s.tags.Contains(tag);
    }

    public SpaceData Get(int id)
    {
        foreach (SpaceData s in spaces)
        {
            if (s.id == id) return s;
        }
        return null;
    }

    // Starting point for a 4x4 board: 16 spaces, ids 1-16 row by row, links up/down/left/right.
    // Check every link and name against the foam board as it is now, then fix anything that differs.
    [ContextMenu("Fill 4x4 grid (verify against the foam board)")]
    public void FillFourByFour()
    {
        spaces.Clear();
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                bool edgeX = x == 0 || x == 3;
                bool edgeY = y == 0 || y == 3;
                string kind = (edgeX && edgeY) ? "Corner Zone" : (edgeX || edgeY) ? "Edge Zone" : "Center Hub";

                SpaceData s = new SpaceData();
                s.id = y * 4 + x + 1;
                s.displayName = kind;
                s.gridPosition = new Vector2Int(x, y);
                if (edgeX && edgeY) { s.tags.Add("corner"); s.tags.Add("edge"); }
                else if (edgeX || edgeY) s.tags.Add("edge");
                else s.tags.Add("center");
                if (x > 0) s.neighbors.Add(y * 4 + (x - 1) + 1);
                if (x < 3) s.neighbors.Add(y * 4 + (x + 1) + 1);
                if (y > 0) s.neighbors.Add((y - 1) * 4 + x + 1);
                if (y < 3) s.neighbors.Add((y + 1) * 4 + x + 1);
                spaces.Add(s);
            }
        }
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    [ContextMenu("Validate Board")]
    public void ValidateFromMenu()
    {
        Validate();
    }

    // Checks the space count, unique ids, and that every connection exists and runs both ways.
    public bool Validate()
    {
        bool ok = true;
        HashSet<int> seen = new HashSet<int>();
        foreach (SpaceData s in spaces)
        {
            if (!seen.Add(s.id))
            {
                Debug.LogError(name + ": duplicate space id " + s.id);
                ok = false;
            }
        }
        foreach (SpaceData s in spaces)
        {
            foreach (int n in s.neighbors)
            {
                SpaceData other = Get(n);
                if (other == null)
                {
                    Debug.LogError(name + ": space " + s.id + " lists missing neighbor " + n);
                    ok = false;
                }
                else if (!other.neighbors.Contains(s.id))
                {
                    Debug.LogError(name + ": link " + s.id + " -> " + n + " is not mirrored");
                    ok = false;
                }
            }
        }
        Debug.Log(name + ": " + spaces.Count + " spaces, " + (ok ? "adjacency list OK" : "ERRORS above"));
        return ok;
    }
}
