using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Works out, for every character on the field, what its marker should be saying - and hands
/// that verdict to that character's CharacterIndicator.
///
/// This is the ONLY place the tag indicators ask the rules anything. It reads
/// MatchManager.WouldWinTag, which is the same method ResolveTouch uses to decide a real capture,
/// so a green ring and the capture it promises can never disagree. Nothing here re-derives the
/// fieldTime rule, and nothing here changes it.
///
/// WHAT IT DOES NOT DO: it never moves a character, never touches an AI, and never adds a
/// collider. It is a read-only view of state that already exists.
///
/// COST: one pass every refreshInterval (0.15 s). At 4v4 that is 4 opponents x 6.7 Hz = about
/// 27 verdicts a second - a handful of float comparisons each, no allocations, no raycasts, and
/// no searching the scene (TeamManager is a registry that characters fill in themselves). The
/// per-frame path does nothing but a timer.
/// </summary>
public class IndicatorController : MonoBehaviour
{
    [Header("Scene references")]
    [Tooltip("The rules. Read for tagDistance and WouldWinTag - never written to.")]
    public MatchManager match;

    [Header("Shared materials - five for the whole match, never one per character")]
    public Material youMaterial;
    public Material youDimMaterial;
    public Material canTagMaterial;
    public Material dangerMaterial;
    public Material neutralMaterial;

    [Header("When a tag ring is drawn")]
    [Tooltip("How far away an opponent starts showing a verdict, as a multiple of the game's " +
             "own tagDistance. 2 means the ring appears at 2.4 m, just before contact at 1.2 m.")]
    public float warnRangeMultiplier = 2f;

    [Tooltip("Extra fieldTime, in seconds, that must separate the two characters before a " +
             "verdict is shown. This is the anti-flicker margin: without it a ring would strobe " +
             "whenever the two fieldTimes are nearly equal.")]
    public float hysteresis = 0.15f;

    [Tooltip("How long a new verdict must survive before it is displayed.")]
    public float verdictHoldSeconds = 0.2f;

    [Tooltip("How often verdicts are recomputed. 0.15 s is inside the requested 0.1-0.2 s band.")]
    public float refreshInterval = 0.15f;

    [Header("Debug")]
    [Tooltip("Log every verdict with both fieldTimes and the distance, so the ring can be " +
             "checked against the rule by hand.")]
    public bool logVerdicts;

    CharacterStatus human;
    float timer;
    bool wasActive;

    Quaternion billboard;
    bool cameraResolved;

    void Start()
    {
        ResolveMatch();
        ResolveHuman();

        timer = refreshInterval;
        Refresh();
    }

    void Update()
    {
        bool active = TeamManager.MatchActive;

        // React the instant a match starts or ends rather than up to 0.15 s later, so nothing
        // is still drawn over the title screen or the results screen.
        if (active != wasActive)
        {
            wasActive = active;

            if (active)
            {
                // Possibly a Restart, which rebuilds every character from the prefab.
                ResolveMatch();
                ResolveHuman();
            }

            Refresh();
        }

        if (!active) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = Mathf.Max(0.02f, refreshInterval);

        Refresh();
    }

    /// <summary>Recomputes everything now. Used when the tag-hint toggle flips.</summary>
    public void ForceRefresh()
    {
        Refresh();
    }

    // ------------------------------------------------------------------- the verdicts

    void Refresh()
    {
        if (match == null) ResolveMatch();
        if (human == null) ResolveHuman();
        if (human == null) return;      // no player on the field yet

        bool live = TeamManager.MatchActive;

        // ---- the character the player controls. ALWAYS marked, toggle or not -------------
        CharacterIndicator mine = IndicatorFor(human);
        if (mine != null)
        {
            mine.SetVisible(live);

            // Dimmed while captured. The bob stops too (see CharacterIndicator.LateUpdate),
            // so "inactive" is two cues rather than one.
            mine.SetState(human.isCaptured ? IndicatorState.YouDim : IndicatorState.You);
        }

        // ---- every opponent, relative to the player --------------------------------------
        bool hints = MatchSettings.TagHints;
        List<CharacterStatus> foes = TeamManager.Members(human.team.Opponent());

        for (int i = 0; i < foes.Count; i++)
        {
            CharacterStatus foe = foes[i];
            if (foe == null) continue;

            CharacterIndicator ind = IndicatorFor(foe);
            if (ind == null) continue;

            ind.SetVisible(live && hints);
            if (!live || !hints) continue;

            IndicatorState verdict = Verdict(human, foe);

            // RequestState, not SetState: a verdict that does not last verdictHoldSeconds is
            // thrown away, which is the second line of defence against a flickering ring.
            ind.RequestState(verdict, refreshInterval, verdictHoldSeconds);

            if (logVerdicts)
            {
                Debug.Log("[Indicator] " + foe.name + " -> " + verdict +
                          "   me.ft=" + human.fieldTime.ToString("F2") +
                          "  foe.ft=" + foe.fieldTime.ToString("F2") +
                          "  dist=" + Flat(human.transform.position, foe.transform.position).ToString("F2") +
                          "  canTag=" + match.WouldWinTag(human, foe) +
                          "  wouldLose=" + match.WouldWinTag(foe, human));
            }
        }

        // ---- team-mates: no ring -----------------------------------------------------------
        // They resolve to Neutral and hideWhenNeutral keeps them off the field, which is the
        // documented default. The team marker is an optional extra, not part of this step.
        List<CharacterStatus> mates = TeamManager.Members(human.team);

        for (int i = 0; i < mates.Count; i++)
        {
            CharacterStatus mate = mates[i];
            if (mate == null || mate == human) continue;

            CharacterIndicator ind = IndicatorFor(mate);
            if (ind == null) continue;

            ind.SetVisible(live);
            ind.SetState(IndicatorState.Neutral);
        }
    }

    /// <summary>
    /// The state table, in one place. Everything here is a GATE on top of the real rule - the
    /// rule itself is MatchManager.WouldWinTag and is never copied.
    /// </summary>
    IndicatorState Verdict(CharacterStatus me, CharacterStatus foe)
    {
        if (match == null) return IndicatorState.Neutral;

        // A captured or just-rescued player can neither tag anybody nor be tagged.
        if (me.isCaptured || me.IsImmune) return IndicatorState.Neutral;

        // WouldWinTag refuses prisoners and immune characters as well, so this only saves the
        // distance check - but it is also what makes a captured opponent read as Neutral.
        if (foe.isCaptured || foe.IsImmune) return IndicatorState.Neutral;

        float distance = Flat(me.transform.position, foe.transform.position);
        if (distance > WarnRange) return IndicatorState.Neutral;

        // tieEpsilon is the game's own draw threshold; hysteresis widens it into a dead band.
        // A lead smaller than this reads Neutral rather than flickering between the two.
        float margin = match.tieEpsilon + hysteresis;
        float lead = foe.fieldTime - me.fieldTime;    // positive = the foe is staler, so I win

        if (lead > margin && match.WouldWinTag(me, foe)) return IndicatorState.CanTag;
        if (-lead > margin && match.WouldWinTag(foe, me)) return IndicatorState.Danger;

        return IndicatorState.Neutral;
    }

    /// <summary>How close an opponent must be before it is worth showing a verdict.</summary>
    float WarnRange
    {
        get
        {
            float tag = match != null ? match.tagDistance : 1.2f;
            return Mathf.Max(0.01f, tag) * Mathf.Max(1f, warnRangeMultiplier);
        }
    }

    // ------------------------------------------------------------------- lookups

    void ResolveMatch()
    {
        if (match == null) match = Object.FindAnyObjectByType<MatchManager>();
    }

    /// <summary>
    /// Finds the human once. TeamManager.IsHuman is the only thing in the project that knows who
    /// the player is, and the roster is a registry the characters fill in themselves - so this is
    /// a walk over at most four entries, not a scene search.
    /// </summary>
    void ResolveHuman()
    {
        human = FindHuman(Team.Blue);
        if (human == null) human = FindHuman(Team.Red);
    }

    static CharacterStatus FindHuman(Team team)
    {
        List<CharacterStatus> members = TeamManager.Members(team);

        for (int i = 0; i < members.Count; i++)
        {
            if (members[i] != null && TeamManager.IsHuman(members[i])) return members[i];
        }

        return null;
    }

    /// <summary>
    /// Gets a character's indicator, creating and configuring it if the prefab did not already
    /// carry one. That self-heal is what makes this safe on a prefab whose wiring was missed.
    /// </summary>
    CharacterIndicator IndicatorFor(CharacterStatus character)
    {
        if (character == null) return null;

        CharacterIndicator ind = character.GetComponent<CharacterIndicator>();
        if (ind == null) ind = character.gameObject.AddComponent<CharacterIndicator>();

        if (!ind.isConfigured)
        {
            ind.Configure(youMaterial, youDimMaterial, canTagMaterial, dangerMaterial,
                          neutralMaterial, BillboardRotation);
        }

        return ind;
    }

    /// <summary>
    /// The rotation that faces a marker at the camera. Resolved ONCE: this camera never moves,
    /// so billboarding costs nothing per frame. If it is ever animated, this just needs the
    /// camera's rotation refreshed here and the icons will follow.
    /// </summary>
    Quaternion BillboardRotation
    {
        get
        {
            if (!cameraResolved)
            {
                Camera cam = Camera.main;

                // The camera rig is pitched 55 degrees (rig 325 + camera 90). Falling back to
                // that keeps the icons readable even if MainCamera's tag is ever mislaid.
                billboard = cam != null ? cam.transform.rotation : Quaternion.Euler(55f, 0f, 0f);
                cameraResolved = true;
            }

            return billboard;
        }
    }

    static float Flat(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }
}
