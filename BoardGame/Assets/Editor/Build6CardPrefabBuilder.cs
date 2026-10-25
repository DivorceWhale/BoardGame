using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Builds the single Build 6 card UI prefab. Layout matches the 3D event card
// (white face, 0.97 : 1.24, black Liberation Sans, text centred from the top).
// Run once from the menu: QUAKER > Build 6 > Create Card UI Prefab.
public static class Build6CardPrefabBuilder
{
    const string PrefabPath = "Assets/Prefabs/Cards/CardUI.prefab";
    const string IconSetPath = "Assets/ScriptableObjects/IconSet.asset";
    static readonly Vector2 CardSize = new Vector2(388, 496);   // 0.97 : 1.24 x 400

    [MenuItem("QUAKER/Build 6/Create Card UI Prefab")]
    public static void Create()
    {
        GameObject root = new GameObject("CardUI", typeof(RectTransform));
        RectTransform rootRect = (RectTransform)root.transform;
        rootRect.sizeDelta = CardSize;

        Image frame = root.AddComponent<Image>();
        frame.color = Color.white;
        Outline border = root.AddComponent<Outline>();
        border.effectColor = Color.black;
        border.effectDistance = new Vector2(3, -3);

        // Regions, as fractions of the card (0 = bottom/left, 1 = top/right)
        TMP_Text title = MakeText(root.transform, "TitleText", 0.05f, 0.85f, 0.95f, 0.96f,
            30, FontStyles.Bold, TextAlignmentOptions.Center);
        // Parity fix: long titles ("Cracks Spider the Slab") overflowed at a fixed 30 pt
        title.enableAutoSizing = true;
        title.fontSizeMin = 20;
        title.fontSizeMax = 30;

        RectTransform iconRow = MakeRegion(root.transform, "IconRow", 0.05f, 0.73f, 0.95f, 0.84f);
        HorizontalLayoutGroup row = iconRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.childAlignment = TextAnchor.MiddleCenter;
        row.spacing = 12;
        row.childControlWidth = false;
        row.childControlHeight = false;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = false;

        GameObject templateGo = new GameObject("IconTemplate", typeof(RectTransform));
        templateGo.transform.SetParent(iconRow, false);
        ((RectTransform)templateGo.transform).sizeDelta = new Vector2(48, 48);
        Image iconTemplate = templateGo.AddComponent<Image>();
        iconTemplate.preserveAspect = true;
        templateGo.SetActive(false);

        // Art window: block cards show their block shape here; event cards have no art and it hides
        RectTransform artRect = MakeRegion(root.transform, "ArtWindow", 0.10f, 0.18f, 0.90f, 0.72f);
        Image artImage = artRect.gameObject.AddComponent<Image>();
        artImage.preserveAspect = true;
        artImage.raycastTarget = false;
        artImage.enabled = false;

        TMP_Text body = MakeText(root.transform, "BodyText", 0.08f, 0.16f, 0.92f, 0.72f,
            22, FontStyles.Normal, TextAlignmentOptions.Top);
        body.paragraphSpacing = 40;

        RectTransform footer = MakeRegion(root.transform, "Footer", 0.05f, 0.03f, 0.95f, 0.13f);
        TMP_Text footerText = MakeText(footer, "FooterText", 0f, 0f, 0.8f, 1f,
            16, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        RectTransform weightRect = MakeRegion(footer, "WeightIcon", 0.8f, 0f, 1f, 1f);
        Image weightIcon = weightRect.gameObject.AddComponent<Image>();
        weightIcon.preserveAspect = true;
        weightIcon.enabled = false;

        CardView view = root.AddComponent<CardView>();
        view.iconSet = AssetDatabase.LoadAssetAtPath<IconSet>(IconSetPath);
        view.frame = frame;
        view.titleText = title;
        view.artImage = artImage;
        view.iconRow = iconRow;
        view.iconTemplate = iconTemplate;
        view.bodyText = body;
        view.footerText = footerText;
        view.weightIcon = weightIcon;
        view.eventColour = Color.white;
        view.blockColour = Color.white;

        if (view.iconSet == null) Debug.LogWarning("Build 6: IconSet not found at " + IconSetPath);

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        Selection.activeObject = saved;
        EditorGUIUtility.PingObject(saved);
        Debug.Log("Build 6: saved " + PrefabPath);
    }

    static RectTransform MakeRegion(Transform parent, string name, float xMin, float yMin, float xMax, float yMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    static TMP_Text MakeText(Transform parent, string name, float xMin, float yMin, float xMax, float yMax,
        float size, FontStyles style, TextAlignmentOptions align)
    {
        RectTransform rt = MakeRegion(parent, name, xMin, yMin, xMax, yMax);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
        t.text = string.Empty;
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = align;
        t.color = Color.black;
        t.enableAutoSizing = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.overflowMode = TextOverflowModes.Ellipsis;
        t.raycastTarget = false;
        return t;
    }
}
