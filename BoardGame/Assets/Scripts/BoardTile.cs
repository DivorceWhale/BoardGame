using UnityEngine;

// Put this on each of the 16 tile objects of your board model and give it the space id from the foam board.
public class BoardTile : MonoBehaviour
{
    public int spaceId;

    private Renderer[] renderers;
    private Color[] originals;

    void Cache()
    {
        if (renderers != null) return;
        renderers = GetComponentsInChildren<Renderer>(true);
        originals = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originals[i] = GetColor(renderers[i].material);
        }
    }

    public void SetBlocked(bool blocked, Color blockedColor)
    {
        Cache();
        for (int i = 0; i < renderers.Length; i++)
        {
            Color c = blocked ? Color.Lerp(originals[i], blockedColor, 0.75f) : originals[i];
            SetColor(renderers[i].material, c);
        }
    }

    static Color GetColor(Material m)
    {
        if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
        if (m.HasProperty("_Color")) return m.GetColor("_Color");
        return Color.white;
    }

    static void SetColor(Material m, Color c)
    {
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        else if (m.HasProperty("_Color")) m.SetColor("_Color", c);
    }
}
