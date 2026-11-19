using System.Collections;
using UnityEngine;

// The secret card (digital only). When an event flip turns up the secret card (QuakerBalance: secretCardOn,
// secretCardChance), a giant rubber duck (QuakerBalance: secretCardProp, Prop_RubberDuck) flies across the table
// straight through the tower and knocks every block off the tower base. The tower has collapsed, so the game ends
// the usual way (R-16): whoever placed the most recent block loses. Blocks it knocks off are never strikes (R-27).
public partial class QuakerPlay
{
    const float DuckLengthInCells = 3.5f;   // the duck is about 3.5 blocks long: big enough to clear the 4 x 4 base
    const float DuckSpeed = 28f;            // units per second
    const float DuckRunUp = 34f;            // how far from the tower it starts and ends

    IEnumerator SecretCardStrike()
    {
        busy = true;
        string title = s.eventCard != null ? s.eventCard.title : "Secret card";
        Add("  SECRET CARD: " + title + " flies through the tower.");
        QueueBanner("Secret card: " + title + "!", "It flies straight through the tower. The whole tower comes down.", new Color(1f, 0.85f, 0.2f));
        yield return FlashWarning(WarningSeconds);   // a flashing pixel-art warning sign: something is coming

        // Fly across the screen, left to right as the players see it, at the height of the bottom of the tower.
        Vector3 dir = Camera.main != null ? Camera.main.transform.right : Vector3.right;
        dir.y = 0f;
        dir = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.right;

        GameObject duck = SpawnSecretProp();
        duck.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);   // the duck's beak points along +z
        Bounds db = RenderBounds(duck);
        Vector3 lift = Vector3.up * (baseTop + 0.3f - db.min.y);              // its belly just above the tower base
        Vector3 from = baseCentre - dir * DuckRunUp, to = baseCentre + dir * DuckRunUp;
        Vector3 offset = duck.transform.position - db.center;                 // move by its middle, not its pivot
        offset.y = 0f;
        duck.transform.position = new Vector3(from.x, duck.transform.position.y, from.z) + offset + lift;

        Rigidbody body = duck.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        // The flight: straight through, with a little bob and waddle.
        Vector3 start = duck.transform.position, end = start + (to - from);
        Quaternion facing = duck.transform.rotation;
        float seconds = (to - from).magnitude / DuckSpeed, t = 0f;
        bool hit = false;
        while (t < seconds)
        {
            yield return new WaitForFixedUpdate();
            t += Time.fixedDeltaTime;
            float k = Mathf.Clamp01(t / seconds);
            Vector3 p = Vector3.Lerp(start, end, k) + Vector3.up * (Mathf.Sin(t * 9f) + 1f) * 0.3f;   // bobs up, never into the base
            body.MovePosition(p);
            body.MoveRotation(facing * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 12f) * 8f));
            if (!hit && k >= 0.4f) { hit = true; GameSounds.Play(GameSound.Collapse); }
        }
        Destroy(duck);

        // Anything the duck missed is swept off the same way, until nothing is left on the tower base.
        for (int pass = 0; pass < 4; pass++)
        {
            bool any = false;
            foreach (Rigidbody rb in placed)
            {
                if (rb == null || !OnTower(rb)) continue;
                any = true;
                rb.AddForce(dir * 14f + Vector3.up * 5f + Random.insideUnitSphere * 2f, ForceMode.VelocityChange);
                rb.AddTorque(Random.insideUnitSphere * 8f, ForceMode.VelocityChange);
            }
            if (!any) break;
            yield return new WaitForSeconds(1.2f);
        }

        // The usual check: every block is off the base, so the tower has collapsed (R-16).
        yield return SettleThenCheck(false);
    }

    // ---------------- The warning sign ----------------

    const float WarningSeconds = 2.2f;   // how long the sign flashes before the duck arrives
    const float WarningBlink = 0.18f;    // on for this long, off for this long
    static Texture2D warningTexture;

    // A red warning triangle with a black "!", drawn pixel by pixel (33 x 29). Point filtering keeps the pixels sharp.
    static Texture2D WarningTexture()
    {
        if (warningTexture != null) return warningTexture;
        const int W = 33, H = 29;
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color outline = new Color32(0x3A, 0x06, 0x06, 0xFF);   // dark edge so it reads on any background
        Color red = new Color32(0xE0, 0x1E, 0x1E, 0xFF);
        Color redLight = new Color32(0xFF, 0x5A, 0x4A, 0xFF);  // a one-pixel highlight on the left edge
        Color white = new Color32(0xFF, 0xF4, 0xE0, 0xFF);
        Color black = new Color32(0x14, 0x10, 0x10, 0xFF);

        Texture2D t = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "Warning sign" };
        float cx = (W - 1) / 2f;
        for (int row = 0; row < H; row++)          // row 0 = the top point
        {
            int y = H - 1 - row;                   // textures count from the bottom
            float half = row * (W - 1) / 2f / (H - 1) + 0.5f;
            for (int x = 0; x < W; x++)
            {
                float d = Mathf.Abs(x - cx);
                Color c = clear;
                if (d <= half)
                {
                    bool inner = row >= 9 && row <= H - 5 && d <= half - 5.2f;   // white middle
                    bool edge = d > half - 1f || row == H - 1;                  // 1 px outline
                    c = edge ? outline : inner ? white : (x < cx && d > half - 2f ? redLight : red);
                }
                // The "!": a 3 px bar and a 3 x 3 dot.
                if (Mathf.Abs(x - cx) <= 1f && ((row >= 11 && row <= 19) || (row >= 22 && row <= 24))) c = black;
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        warningTexture = t;
        return t;
    }

    IEnumerator FlashWarning(float seconds)
    {
        // Its own canvas on top of everything, so it shows over the card panels and banners.
        GameObject root = new GameObject("Warning", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        UnityEngine.UI.CanvasScaler scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // A red glow over the whole screen that pulses with the sign.
        GameObject glowGo = new GameObject("Red glow", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        glowGo.transform.SetParent(root.transform, false);
        RectTransform grt = (RectTransform)glowGo.transform;
        grt.anchorMin = Vector2.zero; grt.anchorMax = Vector2.one; grt.offsetMin = grt.offsetMax = Vector2.zero;
        UnityEngine.UI.Image glow = glowGo.GetComponent<UnityEngine.UI.Image>();
        glow.raycastTarget = false;

        // The sign: 33 x 29 pixels drawn at 10x, so each art pixel is a 10 x 10 block on a 1080p screen.
        Texture2D tex = WarningTexture();
        GameObject signGo = new GameObject("Sign", typeof(RectTransform), typeof(UnityEngine.UI.RawImage));
        signGo.transform.SetParent(root.transform, false);
        RectTransform srt = (RectTransform)signGo.transform;
        srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f);
        srt.anchoredPosition = new Vector2(0f, 40f);
        srt.sizeDelta = new Vector2(tex.width, tex.height) * 10f;
        UnityEngine.UI.RawImage sign = signGo.GetComponent<UnityEngine.UI.RawImage>();
        sign.texture = tex;
        sign.raycastTarget = false;

        TMPro.TMP_Text word = B6Text(root.transform, "Word", "<b>WARNING!</b>", 64, TMPro.FontStyles.Bold, TMPro.TextAlignmentOptions.Center, new Color(1f, 0.25f, 0.2f));
        B6Place(word.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(900f, 90f));
        word.outlineWidth = 0.25f;
        word.outlineColor = new Color32(0x3A, 0x06, 0x06, 0xFF);
        word.characterSpacing = 12f;

        float t0 = Time.time;
        bool lastOn = false;
        while (Time.time - t0 < seconds)
        {
            float t = Time.time - t0;
            bool on = Mathf.FloorToInt(t / WarningBlink) % 2 == 0;
            sign.enabled = on;
            word.enabled = on;
            glow.color = new Color(0.9f, 0.05f, 0.05f, on ? 0.22f : 0.06f);
            if (on && !lastOn) GameSounds.Play(GameSound.TurnBanner, 0.9f);   // a beep on every flash
            lastOn = on;
            yield return null;
        }
        Destroy(root);
    }

    // The prop from QuakerBalance, scaled up so it is about DuckLengthInCells blocks long, with one box collider.
    GameObject SpawnSecretProp()
    {
        GameObject prefab = B.secretCardProp;
        GameObject model = prefab != null ? Instantiate(prefab) : GameObject.CreatePrimitive(PrimitiveType.Sphere);
        model.name = "Secret card: " + (prefab != null ? prefab.name : "ball");
        foreach (Collider c in model.GetComponentsInChildren<Collider>()) Destroy(c);

        Bounds b = RenderBounds(model);
        float longest = Mathf.Max(b.size.x, b.size.z, 0.01f);
        model.transform.localScale *= cell * DuckLengthInCells / longest;

        // One solid box around the whole duck, so it pushes blocks the way you'd expect.
        Bounds world = RenderBounds(model);
        BoxCollider box = model.AddComponent<BoxCollider>();
        box.center = model.transform.InverseTransformPoint(world.center);
        Vector3 ls = model.transform.lossyScale;
        box.size = new Vector3(world.size.x / ls.x, world.size.y / ls.y, world.size.z / ls.z);
        return model;
    }
}
