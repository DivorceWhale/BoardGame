using System.Collections.Generic;
using UnityEngine;

// Shows the board for the chosen scenario. Two modes:
//  1. Existing Board assigned: your own board model (tiles plus decorative edge) is used as is.
//     Each tile carries a BoardTile with its space id; blocked spaces are darkened, and tiles whose
//     id is not in the Board Layout asset are hidden.
//  2. Existing Board empty: simple cubes and link lines are generated from the Board Layout.
public class BoardBuilder : MonoBehaviour
{
    [Header("Your own board model (a scene object, not a per-tile prefab)")]
    public Transform existingBoard;

    [Header("Generated board (used only when Existing Board is empty)")]
    public GameObject spacePrefab;
    public float spacing = 2f;
    public Color openColor = Color.white;
    public Color blockedColor = new Color(0.15f, 0.15f, 0.15f);

    private readonly List<GameObject> built = new List<GameObject>();

    public void Build(BoardLayout layout, ScenarioConfig scenario)
    {
        Clear();
        if (layout == null) return;

        if (existingBoard != null)
        {
            ApplyToExistingBoard(layout, scenario);
            return;
        }

        foreach (SpaceData space in layout.spaces)
        {
            GameObject go;
            if (spacePrefab != null)
            {
                go = Instantiate(spacePrefab, transform);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.SetParent(transform);
                go.transform.localScale = new Vector3(spacing * 0.9f, 0.1f, spacing * 0.9f);
            }
            go.transform.localPosition = new Vector3(space.gridPosition.x * spacing, 0f, space.gridPosition.y * spacing);

            bool open = scenario == null || scenario.IsOpen(space.id);
            go.name = "Space " + space.id + ": " + space.displayName + (open ? "" : " (blocked)");

            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.material.color = open ? openColor : blockedColor;

            built.Add(go);
        }

        foreach (SpaceData a in layout.spaces)
        {
            foreach (int n in a.neighbors)
            {
                SpaceData b = layout.Get(n);
                if (b == null || a.id > b.id) continue;

                GameObject edge = new GameObject("Link " + a.id + "-" + b.id);
                edge.transform.SetParent(transform);
                LineRenderer lr = edge.AddComponent<LineRenderer>();
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.startColor = lr.endColor = Color.black;
                lr.startWidth = lr.endWidth = 0.05f;
                lr.positionCount = 2;
                lr.useWorldSpace = false;
                lr.SetPosition(0, new Vector3(a.gridPosition.x * spacing, 0.1f, a.gridPosition.y * spacing));
                lr.SetPosition(1, new Vector3(b.gridPosition.x * spacing, 0.1f, b.gridPosition.y * spacing));
                built.Add(edge);
            }
        }
    }

    void ApplyToExistingBoard(BoardLayout layout, ScenarioConfig scenario)
    {
        BoardTile[] tiles = existingBoard.GetComponentsInChildren<BoardTile>(true);
        if (tiles.Length == 0)
        {
            Debug.LogError("Existing Board has no BoardTile components. Add one to each tile and set its Space Id.");
            return;
        }

        HashSet<int> found = new HashSet<int>();
        foreach (BoardTile tile in tiles)
        {
            SpaceData space = layout.Get(tile.spaceId);
            if (space == null)
            {
                tile.gameObject.SetActive(false);   // this space does not exist on the repaired board
                continue;
            }
            tile.gameObject.SetActive(true);
            found.Add(space.id);
            bool open = scenario == null || scenario.IsOpen(space.id);
            tile.SetBlocked(!open, blockedColor);
        }

        foreach (SpaceData space in layout.spaces)
        {
            if (!found.Contains(space.id))
            {
                Debug.LogWarning("Board Layout space " + space.id + " (" + space.displayName + ") has no tile on the board model.");
            }
        }
    }

    // Numbers the 16 tiles 1-16, far row first (top of a top-down view), left to right.
    // Check the numbers against the foam board and renumber by hand if your board is oriented differently.
    [ContextMenu("Auto-number tiles by position")]
    public void AutoNumberTiles()
    {
        if (existingBoard == null)
        {
            Debug.LogError("Assign Existing Board first.");
            return;
        }
        List<BoardTile> tiles = new List<BoardTile>(existingBoard.GetComponentsInChildren<BoardTile>(true));
        if (tiles.Count != 16)
        {
            Debug.LogError("Expected 16 BoardTile components, found " + tiles.Count + ".");
            return;
        }

        tiles.Sort((a, b) => b.transform.position.z.CompareTo(a.transform.position.z));
        int id = 1;
        for (int row = 0; row < 4; row++)
        {
            List<BoardTile> line = tiles.GetRange(row * 4, 4);
            line.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            foreach (BoardTile t in line)
            {
                t.spaceId = id++;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(t);
#endif
            }
        }
        Debug.Log("Tiles numbered 1-16. Check each against the foam board.");
    }

    public void Clear()
    {
        foreach (GameObject go in built)
        {
            if (go != null) Destroy(go);
        }
        built.Clear();
    }
}