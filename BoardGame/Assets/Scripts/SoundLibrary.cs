using System;
using System.Collections.Generic;
using UnityEngine;

// Every sound in the game, in one asset: Assets/Resources/QuakerSounds.asset (QUAKER > Sounds > Open Sound Library).
// To add a sound you found: put the file (.wav, .mp3 or .ogg) in Assets/Audio, then drag it into the slot for that
// event. A slot with several clips picks one at random each time. A slot left empty plays a simple made-up
// placeholder (untick Placeholder Sounds to make empty slots silent). GameSounds plays them in every scene.
[CreateAssetMenu(fileName = "QuakerSounds", menuName = "QUAKER/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    [Serializable]
    public class Slot
    {
        [HideInInspector] public string name;   // shows the sound's name in the Inspector list
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
    [Tooltip("Play simple made-up sounds for slots that have no clip yet. Off = empty slots are silent.")]
    public bool placeholderSounds = true;

    [Header("Debug")]
    [Tooltip("Write a line to the Console every time a sound plays, a loop starts or a loop stops: which event, which clip, "
        + "and the time. Use it to find out which sound you are hearing.")]
    public bool logSounds = false;

    [Header("Music (loops; keeps playing from the menu into the game)")]
    public AudioClip menuMusic;
    [Tooltip("Played during a match. Leave empty to keep the menu music going.")]
    public AudioClip gameMusic;

    [Header("Sound effects: one slot per game event")]
    public List<Slot> slots = new List<Slot>();

    [Header("Block hits")]
    [Tooltip("Hits softer than this (impact speed) make no sound.")]
    public float minHitSpeed = 1.5f;
    [Tooltip("Impact speed that plays a hit at full volume.")]
    public float fullHitSpeed = 8f;

    // ---------- the one copy every script reads ----------
    public const string ResourceName = "QuakerSounds";
    static SoundLibrary current;

    public static SoundLibrary Current
    {
        get
        {
            if (current == null) current = Resources.Load<SoundLibrary>(ResourceName);
            if (current == null)
            {
                current = CreateInstance<SoundLibrary>();   // no asset yet: placeholders only
                current.EnsureSlots();
            }
            return current;
        }
    }

    void OnValidate() { EnsureSlots(); }

    // A slot for every sound, in order, so the Inspector always shows the full list (new sounds are added at the end).
    public void EnsureSlots()
    {
        foreach (GameSound g in Enum.GetValues(typeof(GameSound)))
            if (slots.Find(s => s.sound == g) == null) slots.Add(new Slot { sound = g });
        foreach (Slot s in slots) s.name = Nice(s.sound);
    }

    public Slot SlotFor(GameSound sound)
    {
        return slots.Find(s => s.sound == sound);
    }

    // "BlockPlace" -> "Block Place"
    static string Nice(GameSound g)
    {
        string n = g.ToString(), r = "";
        for (int i = 0; i < n.Length; i++) r += (i > 0 && char.IsUpper(n[i]) ? " " : "") + n[i];
        return r;
    }
}
