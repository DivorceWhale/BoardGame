using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Every sound event in the game. New ones go at the END, so the slots already filled in keep their sound.
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
    Warning,        // the flashing warning sign before the secret card (one beep per flash)
    SecretCard,     // the giant rubber duck flies in
    Strike,         // a player takes a strike (R-27)
}

// The audio manager. The sounds themselves are set in the Sound Library (Resources/QuakerSounds.asset); this plays
// them. It starts by itself in every scene and stays between scenes, so music carries on from the menu into the game.
// Other scripts call GameSounds.Play(GameSound.BlockPlace) and so on.
public class GameSounds : MonoBehaviour
{
    public static GameSounds Instance { get; private set; }

    readonly List<AudioSource> pool = new List<AudioSource>();
    readonly Dictionary<GameSound, AudioClip> placeholders = new Dictionary<GameSound, AudioClip>();
    readonly Dictionary<GameSound, float> lastPlayed = new Dictionary<GameSound, float>();
    readonly HashSet<Button> wired = new HashSet<Button>();
    AudioSource loopSource, musicSource;

    static SoundLibrary Lib { get { return SoundLibrary.Current; } }

    // Starts the manager in whatever scene the game opens with (a scene doesn't need its own GameSounds).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (Instance == null) new GameObject("Game Sounds").AddComponent<GameSounds>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }   // one for the whole game
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        Lib.EnsureSlots();
        for (int i = 0; i < 8; i++) pool.Add(NewSource("Effect " + (i + 1)));
        loopSource = NewSource("Loop");
        loopSource.loop = true;
        musicSource = NewSource("Music");
        musicSource.loop = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StopLoop();
        WireButtons();
        // The menu scene plays the menu music; game scenes play the game music (or keep the menu music if none is set).
        bool menu = false;
        foreach (QuakerPlay q in FindObjectsByType<QuakerPlay>(FindObjectsSortMode.None)) if (q.menuScene) menu = true;
        AudioClip want = menu ? Lib.menuMusic : (Lib.gameMusic != null ? Lib.gameMusic : Lib.menuMusic);
        if (want != null && musicSource.clip != want) { musicSource.clip = want; musicSource.Play(); }
        if (want == null) musicSource.Stop();
    }

    // Every button clicks, including ones on hidden panels and ones made later (checked again every second).
    void WireButtons()
    {
        wired.RemoveWhere(b => b == null);
        foreach (Button b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (wired.Add(b)) b.onClick.AddListener(() => Play(GameSound.ButtonClick));
    }

    float nextWire;
    void Update()
    {
        if (musicSource != null) musicSource.volume = Lib.masterVolume * Lib.musicVolume;
        if (Time.unscaledTime >= nextWire) { nextWire = Time.unscaledTime + 1f; WireButtons(); }
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
        SoundLibrary.Slot slot = Lib.SlotFor(sound);
        AudioClip clip = g.ClipFor(sound, slot);
        if (clip == null) return;
        g.loopSource.clip = clip;
        g.loopSource.volume = Lib.masterVolume * Lib.effectsVolume * (slot != null ? slot.volume : 0.8f);
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
        if (impactSpeed < Lib.minHitSpeed) return;
        Instance.PlayNow(GameSound.BlockHit, Mathf.InverseLerp(Lib.minHitSpeed, Lib.fullHitSpeed, impactSpeed) * 0.85f + 0.15f);
    }

    // The clip that played last for a sound (for checking a slot from a test or the Console).
    public string LastClip { get; private set; }

    void PlayNow(GameSound sound, float volumeScale)
    {
        // Many blocks can hit in the same instant; don't stack the same sound more than every 40 ms.
        float last;
        if (lastPlayed.TryGetValue(sound, out last) && Time.unscaledTime - last < 0.04f) return;
        lastPlayed[sound] = Time.unscaledTime;

        SoundLibrary.Slot slot = Lib.SlotFor(sound);
        AudioClip clip = ClipFor(sound, slot);
        if (clip == null) return;
        AudioSource src = FreeSource();
        src.clip = clip;
        src.volume = Lib.masterVolume * Lib.effectsVolume * (slot != null ? slot.volume : 0.8f) * Mathf.Clamp01(volumeScale);
        float v = slot != null ? slot.pitchVariation : 0.05f;
        src.pitch = 1f + Random.Range(-v, v);
        src.Play();
        LastClip = sound + ": " + clip.name;
    }

    AudioClip ClipFor(GameSound sound, SoundLibrary.Slot slot)
    {
        if (slot != null && slot.clips != null)
        {
            List<AudioClip> usable = new List<AudioClip>();
            foreach (AudioClip c in slot.clips) if (c != null) usable.Add(c);
            if (usable.Count > 0) return usable[Random.Range(0, usable.Count)];
        }
        return Lib.placeholderSounds ? Placeholder(sound) : null;
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
            case GameSound.Warning:     c = Tone("warning", 0.16f, 920f, 880f, 0.05f, 6f); break;     // an alarm beep
            case GameSound.SecretCard:  c = Tone("quack", 0.3f, 420f, 260f, 0.35f, 7f); break;        // a rough quack
            case GameSound.Strike:      c = Tone("strike", 0.35f, 240f, 110f, 0.2f, 6f); break;
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
