using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Build 9: a day and night cycle. One real minute is one hour in the game, so a full day takes 24 minutes.
// The sun moves across the sky, the light warms up at sunrise and sunset, and at night a dim blue moonlight
// takes over so the tower stays playable. A small clock in the corner shows the time of day.
public class DayNightCycle : MonoBehaviour
{
    [Header("Time")]
    [Tooltip("Real seconds for one hour in the game. 60 = one minute per hour, so a full day takes 24 minutes.")]
    [Min(1f)] public float secondsPerGameHour = 60f;
    [Tooltip("The hour the scene starts at (0-24). 8 = eight in the morning.")]
    [Range(0f, 24f)] public float startHour = 8f;
    [Tooltip("Stop the clock (for screenshots or testing a time of day).")]
    public bool paused;

    [Header("Sun and moon")]
    [Tooltip("The scene's directional light. Left empty, the scene's sun (Lighting settings) is used.")]
    public Light sun;
    [Tooltip("Which way the sun travels across the sky (degrees around the up axis).")]
    public float sunHeading = 330f;
    public float dayIntensity = 1.1f;
    public Color dayColor = new Color(1f, 0.957f, 0.839f);
    public Color sunriseColor = new Color(1f, 0.55f, 0.28f);
    [Tooltip("Moonlight is kept bright enough to see the tower at night.")]
    public float moonIntensity = 0.5f;
    public Color moonColor = new Color(0.55f, 0.65f, 1f);

    [Header("Sky and ambient light")]
    public float dayAmbient = 1f;
    public float nightAmbient = 0.55f;
    public float daySkyExposure = 1.3f;
    public float nightSkyExposure = 0.12f;

    [Header("Clock")]
    public bool showClock = true;
    [Tooltip("Optional: a text to write the time into. Left empty, a small clock is added in the lower right corner.")]
    public TMP_Text clockText;

    // 0 to 24. Read it from other scripts; set it with SetHour.
    public float Hour { get; private set; }
    public int Day { get; private set; } = 1;
    public bool IsNight { get { return Hour < 6f || Hour >= 18f; } }

    Material sky;              // a copy of the skybox, so the asset on disk never changes
    float giTimer;
    int shownMinute = -1;

    void Start()
    {
        if (sun == null) sun = RenderSettings.sun;
        if (sun == null)
            foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { sun = l; break; }
        if (RenderSettings.skybox != null)
        {
            sky = new Material(RenderSettings.skybox);
            RenderSettings.skybox = sky;
        }
        if (showClock && clockText == null) clockText = MakeClock();
        SetHour(startHour);
    }

    void OnDestroy()
    {
        if (sky != null) Destroy(sky);
    }

    public void SetHour(float hour)
    {
        Hour = Mathf.Repeat(hour, 24f);
        Apply();
        DynamicGI.UpdateEnvironment();
    }

    void Update()
    {
        if (!paused)
        {
            float h = Hour + Time.deltaTime / Mathf.Max(1f, secondsPerGameHour);
            if (h >= 24f) { h -= 24f; Day++; }
            Hour = h;
        }
        Apply();

        // The ambient light comes from the sky, so refresh it every few seconds as the sky changes.
        giTimer += Time.deltaTime;
        if (giTimer >= 2f) { giTimer = 0f; DynamicGI.UpdateEnvironment(); }
    }

    void Apply()
    {
        // 6:00 sunrise (on the horizon), 12:00 overhead, 18:00 sunset.
        float angle = (Hour - 6f) / 24f * 360f;
        float elevation = Mathf.Sin(angle * Mathf.Deg2Rad);      // 1 at noon, -1 at midnight
        float daylight = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elevation * 4f + 0.1f));

        if (sun != null)
        {
            if (elevation > 0f)
            {
                sun.transform.rotation = Quaternion.Euler(angle, sunHeading, 0f);
                sun.color = Color.Lerp(sunriseColor, dayColor, Mathf.Clamp01(elevation * 2.5f));
                sun.intensity = dayIntensity * Mathf.Clamp01(elevation * 4f);
            }
            else
            {
                // Night: the same light becomes the moon, on the far side of the sky.
                sun.transform.rotation = Quaternion.Euler(angle - 180f, sunHeading, 0f);
                sun.color = moonColor;
                sun.intensity = moonIntensity * Mathf.Clamp01(-elevation * 4f);
            }
        }

        RenderSettings.ambientIntensity = Mathf.Lerp(nightAmbient, dayAmbient, daylight);
        if (sky != null && sky.HasProperty("_Exposure"))
            sky.SetFloat("_Exposure", Mathf.Lerp(nightSkyExposure, daySkyExposure, daylight));

        if (clockText != null)
        {
            int minute = Mathf.FloorToInt(Hour * 60f);
            if (minute != shownMinute)
            {
                shownMinute = minute;
                clockText.text = "Day " + Day + "   " + (minute / 60).ToString("00") + ":" + (minute % 60).ToString("00") + (IsNight ? "  night" : "");
            }
        }
    }

    // A small clock at the lower right, above the block supply bar, on its own canvas so it works in any scene.
    TMP_Text MakeClock()
    {
        GameObject canvasGo = new GameObject("DayNightClock", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;   // under the game's panels and banners
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject box = new GameObject("Clock", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(canvasGo.transform, false);
        RectTransform rt = (RectTransform)box.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-20f, 110f);   // above the supply bar, below the Current Block panel
        rt.sizeDelta = new Vector2(290f, 48f);
        Image bg = box.GetComponent<Image>();
        bg.color = new Color(0.04f, 0.05f, 0.07f, 0.7f);
        bg.raycastTarget = false;

        GameObject textGo = new GameObject("Time", typeof(RectTransform));
        textGo.transform.SetParent(box.transform, false);
        RectTransform trt = (RectTransform)textGo.transform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        TextMeshProUGUI t = textGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
        t.fontSize = 26;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }
}
