using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Build 9: every sound in the game goes through here. Put one on an object in the scene, then drop audio clips
// into the slots in the Inspector (Assets/Audio is the place for the files). A slot with several clips picks one
// at random each time, so repeated sounds don't feel mechanical.
// Until a slot has a clip, a simple made-up placeholder sound plays, so every event is audible right away.
// Other scripts call GameSounds.Play(GameSound.BlockPlace) and so on; it does nothing if there is no GameSounds.
public enum GameSound
{
    ButtonClick,    // any UI button
    CardDraw,       // a block card is drawn
    EventFlip,      // the event card is flipped
    BlockPlace,     // a player lets go of a block
    BlockHit,       // blocks knocking into each other or the base (louder for harder hits)
    BlockFall,      // a block falls off the tower base
    ShakeLoop,      // the rumble while the board shakes (loops)
    Collapse,       // the tower collapses
    TurnBanner,     // the turn passes / a banner appears
    GameOver,       // a game ends
    MatchWin,       // the match is won
}

public class GameSounds : MonoBehaviour
{
    [Serializable]
    public class Slot
    {
        public GameSound sound;
        [Tooltip("One clip, or several to pick from at random.")]
        public AudioClip[] clips = new AudioClip[0];
        [Range(0f, 1f)] public float volume = 0.8f;
        [Tooltip("Random pitch change each time (0.1 = up to 10% higher or lower).")]
        [Range(0f, 0.5f)] public float pitchVariation = 0.05f;
    }

    [Header("Volume")]
    [Range(0f, 1f)] public float masterVolume = 0.8f;
    [Range(0f, 1f)] public float effectsVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.4f;
    [Tooltip("Play simple made-up sounds for slots that have no clip yet.")]
    public bool placeholderSounds = true;

    [Header("Music (optional, loops)")]
    public AudioClip music;

    [Header("Sound effects")]
    public List<Slot> slots = new List<Slot>();

    [Header("Block hits")]
    [Tooltip("Hits softer than this (impact speed) make no sound.")]
    public float minHitSpeed = 1.5f;
    [Tooltip("Impact speed that plays a hit at full volume.")]
    public float fullHitSpeed = 8f;

    public static GameSounds Instance { get; private set; }

    readonly List<AudioSource> pool = new List<AudioSource>();
    readonly Dictionary<GameSound, AudioClip> placeholders = new Dictionary<GameSound, AudioClip>();
    readonly Dictionary<GameSound, float> lastPlayed = new Dictionary<GameSound, float>();
    AudioSource loopSource, musicSource;

    // Fills in a slot for every sound, so the Inspector always shows the full list.
    void Reset() { EnsureSlots(); }
    void OnValidate() { EnsureSlots(); }

    public void EnsureSlots()
    {
        foreach (GameSound g in Enum.GetValues(typeof(GameSound)))
            if (slots.Find(s => s.sound == g) == null) slots.Add(new Slot { sound = g });
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        EnsureSlots();
        for (int i = 0; i < 8; i++) pool.Add(NewSource("Effect " + (i + 1)));
        loopSource = NewSource("Loop");
        loopSource.loop = true;
        musicSource = NewSource("Music");
        musicSource.loop = true;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        // Every button in the scene clicks, including ones on hidden panels.
        foreach (Button b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            b.onClick.AddListener(() => Play(GameSound.ButtonClick));
        if (music != null) { musicSource.clip = music; musicSource.Play(); }
    }

    void Update()
    {
        if (musicSource != null) musicSource.volume = masterVolume * musicVolume;
    }

    AudioSource NewSource(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        AudioSource a = go.AddComponent<AudioSource>();
        a.playOnAwake = false;
        a.spatialBlend = 0f;   // 2D: the table is always in view
        return a;
    }

    // ---------------- Playing ----------------

    public static void Play(GameSound sound, float volumeScale = 1f)
    {
        if (Instance != null) Instance.PlayNow(sound, volumeScale);
    }

    // The rumble while the board shakes.
    public static void StartLoop(GameSound sound)
    {
        if (Instance == null) return;
        GameSounds g = Instance;
        Slot slot = g.SlotFor(sound);
        AudioClip clip = g.ClipFor(sound, slot);
        if (clip == null) return;
        g.loopSource.clip = clip;
        g.loopSource.volume = g.masterVolume * g.effectsVolume * (slot != null ? slot.volume : 0.8f);
        g.loopSource.pitch = 1f;
        if (!g.loopSource.isPlaying) g.loopSource.Play();
    }

    public static void StopLoop()
    {
        if (Instance != null && Instance.loopSource != null) Instance.loopSource.Stop();
    }

    // A block hit something: the harder the hit, the louder.
    public static void Hit(float impactSpeed)
    {
        if (Instance == null) return;
        GameSounds g = Instance;
        if (impactSpeed < g.minHitSpeed) return;
        g.PlayNow(GameSound.BlockHit, Mathf.InverseLerp(g.minHitSpeed, g.fullHitSpeed, impactSpeed) * 0.85f + 0.15f);
    }

    void PlayNow(GameSound sound, float volumeScale)
    {
        // Many blocks can hit in the same instant; don't stack the same sound more than every 40 ms.
        float last;
        if (lastPlayed.TryGetValue(sound, out last) && Time.unscaledTime - last < 0.04f) return;
        lastPlayed[sound] = Time.unscaledTime;

        Slot slot = SlotFor(sound);
        AudioClip clip = ClipFor(sound, slot);
        if (clip == null) return;
        AudioSource src = FreeSource();
        src.clip = clip;
        src.volume = masterVolume * effectsVolume * (slot != null ? slot.volume : 0.8f) * Mathf.Clamp01(volumeScale);
        float v = slot != null ? slot.pitchVariation : 0.05f;
        src.pitch = 1f + UnityEngine.Random.Range(-v, v);
        src.Play();
    }

    Slot SlotFor(GameSound sound)
    {
        return slots.Find(s => s.sound == sound);
    }

    AudioClip ClipFor(GameSound sound, Slot slot)
    {
        if (slot != null && slot.clips != null)
        {
            List<AudioClip> usable = new List<AudioClip>();
            foreach (AudioClip c in slot.clips) if (c != null) usable.Add(c);
            if (usable.Count > 0) return usable[UnityEngine.Random.Range(0, usable.Count)];
        }
        return placeholderSounds ? Placeholder(sound) : null;
    }

    AudioSource FreeSource()
    {
        foreach (AudioSource a in pool) if (!a.isPlaying) return a;
        AudioSource oldest = pool[0];   // all busy: reuse the first one
        pool.RemoveAt(0);
        pool.Add(oldest);
        return oldest;
    }

    // ---------------- Placeholder sounds (made in code, no files) ----------------

    AudioClip Placeholder(GameSound sound)
    {
        AudioClip c;
        if (placeholders.TryGetValue(sound, out c)) return c;
        switch (sound)
        {
            case GameSound.ButtonClick: c = Tone("click", 0.05f, 1200f, 1200f, 0.0f, 60f); break;
            case GameSound.CardDraw:    c = Tone("card", 0.12f, 0f, 0f, 1.0f, 25f); break;             // a soft swish
            case GameSound.EventFlip:   c = Tone("flip", 0.25f, 520f, 780f, 0.3f, 10f); break;
            case GameSound.BlockPlace:  c = Tone("place", 0.12f, 220f, 140f, 0.2f, 30f); break;
            case GameSound.BlockHit:    c = Tone("hit", 0.08f, 300f, 160f, 0.5f, 45f); break;          // a wooden knock
            case GameSound.BlockFall:   c = Tone("fall", 0.35f, 400f, 120f, 0.3f, 8f); break;
            case GameSound.ShakeLoop:   c = Tone("shake", 1.0f, 55f, 55f, 0.6f, 0f, true); break;      // a low rumble
            case GameSound.Collapse:    c = Tone("collapse", 1.2f, 90f, 40f, 0.8f, 3f); break;
            case GameSound.TurnBanner:  c = Tone("banner", 0.3f, 660f, 660f, 0.0f, 9f); break;
            case GameSound.GameOver:    c = Tone("game over", 0.8f, 440f, 220f, 0.1f, 4f); break;
            case GameSound.MatchWin:    c = Tone("match win", 1.0f, 440f, 880f, 0.0f, 3f); break;
        }
        placeholders[sound] = c;
        return c;
    }

    // A tone gliding from one pitch to another, mixed with some noise, fading out (decay per second).
    // loop: no fade, so it can repeat seamlessly.
    static AudioClip Tone(string name, float seconds, float startHz, float endHz, float noise, float decay, bool loop = false)
    {
        const int rate = 44100;
        int n = Mathf.Max(1, Mathf.RoundToInt(seconds * rate));
        float[] data = new float[n];
        System.Random rnd = new System.Random(name.GetHashCode());
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            float hz = Mathf.Lerp(startHz, endHz, (float)i / n);
            phase += 2f * Mathf.PI * hz / rate;
            float tone = startHz > 0f ? Mathf.Sin(phase) : 0f;
            float hiss = (float)(rnd.NextDouble() * 2.0 - 1.0);
            float env = loop ? 1f : Mathf.Exp(-decay * t) * Mathf.Clamp01(t * 400f);   // quick attack, then fade
            if (loop) env *= 0.6f + 0.4f * Mathf.Sin(t * 2f * Mathf.PI * 7f);          // rattle of the shake
            data[i] = (tone * (1f - noise) + hiss * noise) * env * 0.5f;
        }
        AudioClip c = AudioClip.Create("Placeholder " + name, n, 1, rate, false);
        c.SetData(data, 0);
        return c;
    }
}
