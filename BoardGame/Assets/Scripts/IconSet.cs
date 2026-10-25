using System.Collections.Generic;
using UnityEngine;

// Maps each icon on the icon sheet to its sprite. One concept, one mark.
[CreateAssetMenu(fileName = "IconSet", menuName = "QUAKER/Icon Set")]
public class IconSet : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public CardIcon icon;
        public Sprite sprite;
    }

    public List<Entry> entries = new List<Entry>();

    public Sprite Get(CardIcon icon)
    {
        foreach (Entry e in entries)
        {
            if (e.icon == icon) return e.sprite;
        }
        Debug.LogWarning(name + ": no sprite for icon " + icon);
        return null;
    }

    void OnValidate()
    {
        HashSet<CardIcon> seen = new HashSet<CardIcon>();
        foreach (Entry e in entries)
        {
            if (!seen.Add(e.icon))
                Debug.LogWarning(name + ": " + e.icon + " has two sprites. One concept, one mark.");
        }
    }
}
