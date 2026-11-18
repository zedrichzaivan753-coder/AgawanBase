using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Week 14: the game's feedback layer. ONE place that turns a rule event into something the player
/// can hear or feel.
///
/// Why it exists at all: until this script there was not a single AudioClip in the project, so a
/// catch, a rescue, a flag steal and the end of a match all looked identical - the score line
/// changed and nothing else. On a phone, held at arm's length with the sound on, that is the
/// difference between understanding the rules and guessing at them.
///
/// It listens, it never decides:
///   MatchManager.CharacterCaptured -> catch clip + one haptic buzz
///   MatchManager.CharacterRescued  -> rescue clip
///   Flag.PickedUp                  -> flag pick-up clip
///   MatchManager.PlayerWon         -> victory clip
///   MatchManager.PlayerLost        -> defeat clip
///
/// The SOUND button on the pause screen calls ToggleSound(), and the choice is remembered in
/// PlayerPrefs under "audio.feedback", so a player who plays muted stays muted.
///
/// Every play writes one [Feedback] line, whether the sound is on or off. That line is what the
/// Week 14 evidence cites: it proves the event fired and names the clip it would have played.
/// </summary>
public class GameFeedback : MonoBehaviour
{
    public const string SoundPrefKey = "audio.feedback";

    [Header("Clips")]
    [Tooltip("A character was tagged and locked up.")]
    public AudioClip catchClip;

    [Tooltip("A team-mate was freed from prison.")]
    public AudioClip rescueClip;

    [Tooltip("A flag was taken off its pole.")]
    public AudioClip flagPickupClip;

    [Tooltip("The player's team carried the enemy flag home.")]
    public AudioClip victoryClip;

    [Tooltip("The player's team lost its own flag.")]
    public AudioClip defeatClip;

    [Header("Settings")]
    [Range(0f, 1f)]
    public float volume = 0.8f;

    [Tooltip("One short buzz on a catch. Android only, and only on a device.")]
    public bool hapticsEnabled = true;

    [Tooltip("The pause screen's SOUND button label. Set by the editor builder.")]
    public Text soundButtonLabel;

    /// <summary>The one instance the pause screen's button talks to.</summary>
    public static GameFeedback Instance { get; private set; }

    AudioSource source;
    MatchManager match;
    readonly List<Flag> flags = new List<Flag>();

    /// <summary>
    /// Domain reload is off in this project, so a static set to a destroyed instance would survive
    /// into the next play session and point at nothing. Same reset pattern as
    /// GameManager.ResetPlaySessionFlags.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
    }

    // ------------------------------------------------------------------ state

    /// <summary>Whether sound is on. Read from PlayerPrefs every time, so it can never go stale.</summary>
    public static bool SoundOn
    {
        get { return PlayerPrefs.GetInt(SoundPrefKey, 1) == 1; }
    }

    // ------------------------------------------------------------------ wiring

    void Awake()
    {
        Instance = this;

        source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;      // 2D: this is UI feedback, not a sound in the world

        match = FindAnyObjectByType<MatchManager>();
        if (match != null)
        {
            match.CharacterCaptured += OnCaptured;
            match.CharacterRescued += OnRescued;
            match.PlayerWon += OnVictory;
            match.PlayerLost += OnDefeat;
        }
        else
        {
            Debug.LogWarning("[Feedback] no MatchManager in this scene - catch/rescue/victory " +
                             "sounds will not fire.");
        }

        Flag[] found = FindObjectsByType<Flag>(FindObjectsInactive.Include);
        for (int i = 0; i < found.Length; i++)
        {
            found[i].PickedUp += OnFlagPickedUp;
            flags.Add(found[i]);
        }

        RefreshLabel();
    }

    void OnDestroy()
    {
        if (match != null)
        {
            match.CharacterCaptured -= OnCaptured;
            match.CharacterRescued -= OnRescued;
            match.PlayerWon -= OnVictory;
            match.PlayerLost -= OnDefeat;
        }

        for (int i = 0; i < flags.Count; i++)
        {
            if (flags[i] != null) flags[i].PickedUp -= OnFlagPickedUp;
        }

        if (Instance == this) Instance = null;
    }

    // ------------------------------------------------------------------ the pause button

    /// <summary>
    /// Flips sound on/off and remembers it. Wired to the pause screen's SOUND button, which is why
    /// it takes no arguments: it is also callable from a UnityEvent with none.
    /// </summary>
    public void ToggleSound()
    {
        bool on = !SoundOn;
        PlayerPrefs.SetInt(SoundPrefKey, on ? 1 : 0);
        PlayerPrefs.Save();

        RefreshLabel();
        Debug.Log("[Feedback] audio feedback " + (on ? "ON" : "OFF") + " (saved as " +
                  SoundPrefKey + "=" + PlayerPrefs.GetInt(SoundPrefKey, 1) + ")");
    }

    /// <summary>Puts the current state on the button, so it reads SOUND: ON or SOUND: OFF.</summary>
    public void RefreshLabel()
    {
        if (soundButtonLabel == null) return;
        soundButtonLabel.text = SoundOn ? "SOUND: ON" : "SOUND: OFF";
    }

    // ------------------------------------------------------------------ the five one-shots

    void OnCaptured(CharacterStatus caught)
    {
        string who = caught != null ? caught.name + " (" + caught.team + ")" : "someone";
        Play(catchClip, "catch - " + who + " is locked up");

        if (!hapticsEnabled || !Application.isMobilePlatform) return;

        // Handheld only exists in mobile player builds. Without this guard the project does not
        // compile for Windows at all - the runtime check above cannot help a compiler that cannot
        // see the type (DEF-16-01).
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
        Debug.Log("[Feedback] vibrate - catch");
#endif
    }

    void OnRescued(CharacterStatus freed)
    {
        string who = freed != null ? freed.name + " (" + freed.team + ")" : "someone";
        Play(rescueClip, "rescue - " + who + " is free");
    }

    void OnFlagPickedUp(Transform carrier)
    {
        string who = carrier != null ? carrier.name : "someone";
        Play(flagPickupClip, "flag picked up by " + who);
    }

    void OnVictory()
    {
        Play(victoryClip, "victory - your flag reached home");
    }

    void OnDefeat()
    {
        Play(defeatClip, "defeat - your flag was carried home");
    }

    // ------------------------------------------------------------------ playing

    /// <summary>
    /// Plays one one-shot and says so. The log line is written even when sound is off, because the
    /// evidence for this feature is the log: a muted build still has to show that the event fired.
    /// </summary>
    void Play(AudioClip clip, string what)
    {
        if (clip == null)
        {
            Debug.LogWarning("[Feedback] " + what + " - no clip assigned");
            return;
        }

        if (SoundOn && source != null) source.PlayOneShot(clip, volume);

        Debug.Log("[Feedback] " + what + " -> " + clip.name + (SoundOn ? "" : " (sound is off)"));
    }
}
