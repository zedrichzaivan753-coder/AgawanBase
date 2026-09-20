using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The job an AI character has been given for the next little while.
///
/// A role is a PREFERENCE, never a permission. It adds weight to the actions that job is
/// supposed to care about, but it can never forbid an action outright: a Defender with nobody
/// left at home will still run across the map to free a team-mate, because that is more urgent
/// than standing on an empty flag. That is what keeps the roles from breaking the match at the
/// small team sizes, where there are fewer AI than there are jobs.
///
/// The human is never given a real job - see TeamManager.IsHuman. The human is the one player
/// whose behaviour nobody can script, so counting it as a FLEX is what lets a team of
/// "1 human + N AI" hold the same set of jobs as a team of N+1 AI.
/// </summary>
public enum AiRole
{
    /// <summary>Stays home: guards the flag, blocks a carrier heading for our base, fetches a dropped flag.</summary>
    Defender,

    /// <summary>Pushes up the field: takes the enemy flag and runs it home.</summary>
    Raider,

    /// <summary>Goes for an imprisoned team-mate. Only ever one at a time.</summary>
    Rescuer,

    /// <summary>No fixed job - follows whatever is most urgent on the field. Also the human's role.</summary>
    Flex
}

/// <summary>
/// The one place in the game that knows WHO IS ON WHICH TEAM, and which job each of them has.
///
/// Why it exists
/// -------------
/// The prototype started as one human Blue against two Red AI, and the code said so out loud:
/// EnemyAI literally looked up the player. That stops working the moment a team can have any
/// number of members. The AI, the tag rules, the flags and the prisons must not care WHICH
/// character is the human - they only care about the Team value. This registry is what makes
/// that possible.
///
/// It is a registry and nothing more. Characters add themselves when they are switched on and
/// remove themselves when they are destroyed, so gameplay code can simply ask "who is on
/// Blue?" and get an already-built list. That is why nothing in the project needs
/// FindObjectsOfType or FindGameObjectsWithTag inside Update - both are far too slow to run
/// every frame on Android.
///
/// The lists are STATIC, so registration works no matter which script's OnEnable happens to
/// run first. The component on Game/Systems only carries the serialized tuning values.
///
/// ROLES
/// -----
/// The roles are re-derived from the state of the field - not from a rotation - so they move
/// on their own when something changes: a prisoner is taken, our flag is dropped, a team-mate
/// picks up the enemy flag. Whoever is nearest the thing that needs doing gets the job of
/// doing it. Every team is reviewed every roleReviewInterval, and the two teams are staggered
/// so both reviews never land on the same frame.
/// </summary>
public class TeamManager : MonoBehaviour
{
    [Header("Perception - read by EnemyAI")]
    [Tooltip("How far an AI can notice an opponent, in metres, before difficulty scaling. " +
             "Used when EnemyAI is NOT reading the Easy / Normal / Hard preset.")]
    public float visionRadius = 12f;

    [Tooltip("How long an AI remembers where it last saw an opponent, in seconds, before " +
             "difficulty scaling. Used when EnemyAI is NOT reading the preset.")]
    public float memorySeconds = 3f;

    [Header("Roles")]
    [Tooltip("The rules authority. Roles need to know where the flags and prisons are, and which " +
             "character is the human. All of that is read from here rather than duplicated, so " +
             "there is still exactly one place that holds those references.")]
    public MatchManager matchManager;

    [Tooltip("Seconds between role reviews, for each team.")]
    public float roleReviewInterval = 1.5f;

    [Tooltip("Extra delay given to the second team's review, so the two teams never re-derive " +
             "their roles on the same frame.")]
    public float roleStagger = 0.025f;

    [Header("Debug")]
    [Tooltip("Log every character joining a team. Handy while the spawner is new.")]
    public bool logRoster = true;

    [Tooltip("Log every role change. Off by default: at 4v4 the roles move often enough that " +
             "the log becomes noise. The On-Screen AIDebugLabel shows the same thing live.")]
    public bool logRoles;

    /// <summary>The scene instance, if one was placed. Gameplay never needs it: the registry
    /// below is static, so ordering can never break it.</summary>
    public static TeamManager Instance { get; private set; }

    static readonly List<CharacterStatus> blueMembers = new List<CharacterStatus>();
    static readonly List<CharacterStatus> redMembers = new List<CharacterStatus>();

    /// <summary>Which job each AI currently holds. Keyed by character, so a role survives a
    /// character being moved around the roster until the next review overwrites it.</summary>
    static readonly Dictionary<CharacterStatus, AiRole> roles =
        new Dictionary<CharacterStatus, AiRole>(16);

    /// <summary>Scratch list: who has already been given a job during the current review.
    /// Reused every review, so assigning roles allocates nothing.</summary>
    static readonly List<CharacterStatus> assigned = new List<CharacterStatus>(8);

    /// <summary>One countdown per team (Blue, Red). Reused, never re-created.</summary>
    static readonly float[] roleTimer = new float[2];

    /// <summary>The rules authority, cached. Set from the serialized field in Awake so the
    /// static helpers below can reach it without a scene search.</summary>
    static MatchManager matchRef;

    /// <summary>Bumped by every registration and unregistration. A change means the roster was
    /// rebuilt - a reload, a restart - so the roles must be re-derived at once rather than
    /// waiting for the next review tick.</summary>
    static int rosterVersion;

    static int reviewedVersion = -1;
    static bool wasActive;

    // ------------------------------------------------------------------ the role sets
    //
    // Which jobs a team holds is decided by how many AI it has and whether one of its members
    // is the human. The human flexes by definition, so a team containing the human needs one
    // fewer AI job to reach the same four jobs.
    //
    //   team with the human (Blue)      team of AI only (Red)
    //   0 AI -> -                       0 AI -> -
    //   1 AI -> Defender                1 AI -> -
    //   2 AI -> Defender, Raider        2 AI -> Defender, Raider
    //   3 AI -> Defender, Raider,       3 AI -> Defender, Raider
    //           Rescuer                        (3rd stays Flex)
    //   4 AI -> Defender, Raider,       4 AI -> Defender, Raider, Rescuer
    //           Rescuer, Flex                  (4th stays Flex)
    //
    // At 4v4 that gives 1 Defender / 1 Raider / 1 Rescuer / 1 Flex per team: on Blue the human
    // is the Flex. Each role appears at MOST once, which is what stops two AI defending the
    // same flag or two of them chasing one prisoner.
    static readonly AiRole[] noRoles = { };

    static readonly AiRole[] withHuman1 = { AiRole.Defender };
    static readonly AiRole[] withHuman2 = { AiRole.Defender, AiRole.Raider };
    static readonly AiRole[] withHuman3 = { AiRole.Defender, AiRole.Raider, AiRole.Rescuer };

    static readonly AiRole[] aiOnly2 = { AiRole.Defender, AiRole.Raider };
    static readonly AiRole[] aiOnly3 = { AiRole.Defender, AiRole.Raider };
    static readonly AiRole[] aiOnly4 = { AiRole.Defender, AiRole.Raider, AiRole.Rescuer };

    void Awake()
    {
        Instance = this;

        // Prefer the Inspector reference, but do NOT depend on it: with no link here the
        // roles cannot find the flags, the prisons or the human, and nothing on screen says
        // so. Awake is not Update - one lookup at load can never run per frame.
        matchRef = matchManager != null ? matchManager : Object.FindAnyObjectByType<MatchManager>();

        // The static role table outlives a scene reload, so it is cleared here rather than
        // trusted: a restart must never inherit the previous match's prisoners or carriers.
        roles.Clear();
        assigned.Clear();

        roleTimer[0] = roleReviewInterval;
        roleTimer[1] = roleReviewInterval + roleStagger;

        rosterVersion++;
        reviewedVersion = -1;
        wasActive = false;
    }

    void Start()
    {
        // Log the FINAL roster once the whole scene has woken up. Doing it here instead of
        // inside Register() means the report never depends on which object's Awake happened to
        // run first - and it shows both teams together, which is what you actually want to read.
// Roles are derived BEFORE the first Update, so no AI can think without a job, and so
        // the roster below reads as the jobs that were handed out rather than all FLEX.
        ReviewRoles();
        ResetRoleTimers();

        if (logRoster) LogRoster();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (matchRef == null) matchRef = matchManager;

        // A rebuilt roster (scene load, restart) invalidates every role. Re-derive immediately
        // instead of waiting up to roleReviewInterval, so no AI ever thinks with a stale job.
        if (reviewedVersion != rosterVersion)
        {
            reviewedVersion = rosterVersion;
            ReviewRoles();
            ResetRoleTimers();
            return;
        }

        if (!MatchActive)
        {
            wasActive = false;
            return;
        }

        // First frame of a live match: the flags and rosters are final by now, so take one
        // fresh reading before any AI gets to think.
        if (!wasActive)
        {
            wasActive = true;
            ReviewRoles();
            ResetRoleTimers();
        }

        for (int t = 0; t < TeamUtil.All.Length; t++)
        {
            roleTimer[t] -= Time.deltaTime;
            if (roleTimer[t] > 0f) continue;

            roleTimer[t] = roleReviewInterval;
            AssignRoles(TeamUtil.All[t]);
        }
    }

    /// <summary>Restarts both reviews, keeping the second team a roleStagger behind the first.</summary>
    void ResetRoleTimers()
    {
        roleTimer[0] = roleReviewInterval;
        roleTimer[1] = roleReviewInterval + roleStagger;
    }

    /// <summary>
    /// True while a match is actually being played. EnemyAI refuses to think while this is
    /// false, so nobody walks around on the Title, Match Setup or Ready screens.
    /// MatchManager mirrors its own matchActive flag into here every frame.
    /// </summary>
    public static bool MatchActive;

    /// <summary>Prints both teams in one line each, with each AI's current job.
    /// Used by Start() and by the editor tools.</summary>
    public static void LogRoster()
    {
        for (int t = 0; t < TeamUtil.All.Length; t++)
        {
            Team team = TeamUtil.All[t];
            List<CharacterStatus> list = ListFor(team);

            string names = "";
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null) continue;
                names += (names.Length > 0 ? ", " : "") + list[i].name +
                         " [" + RoleName(RoleOf(list[i])) + "]" +
                         (list[i].isCaptured ? " (prison)" : "");
            }

            Debug.Log("[Team] " + team + ": " + list.Count + " member(s) - " +
                      (names.Length > 0 ? names : "none"));
        }
    }

    // ================================================================ registration

    /// <summary>Adds a character to its team's list. Safe to call more than once.</summary>
    public static void Register(CharacterStatus character)
    {
        if (character == null) return;

        List<CharacterStatus> list = ListFor(character.team);
        Prune(list);

        if (list.Contains(character)) return;   // no duplicates

        list.Add(character);
        rosterVersion++;
    }

    /// <summary>
    /// Removes a character from the registry. Both lists are checked, because a character can
    /// be removed after its team value has already changed.
    /// </summary>
    public static void Unregister(CharacterStatus character)
    {
        if (character == null) return;

        blueMembers.Remove(character);
        redMembers.Remove(character);

        // The dictionary key would otherwise keep a reference to a destroyed character and
        // quietly grow for the lifetime of the session.
        roles.Remove(character);

        rosterVersion++;
    }

    // ===================================================================== queries

    /// <summary>The members of one team. Never null; may be empty.</summary>
    public static List<CharacterStatus> Members(Team team)
    {
        return ListFor(team);
    }

    /// <summary>How many characters are on this team (captured ones included).</summary>
    public static int Count(Team team)
    {
        return ListFor(team).Count;
    }

    /// <summary>How many characters on this team are still free to move.</summary>
    public static int FreeCount(Team team)
    {
        List<CharacterStatus> list = ListFor(team);
        int count = 0;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null && !list[i].isCaptured) count++;
        }
        return count;
    }

    /// <summary>How many characters on this team are locked in the enemy prison.</summary>
    public static int CapturedCount(Team team)
    {
        List<CharacterStatus> list = ListFor(team);
        int count = 0;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null && list[i].isCaptured) count++;
        }
        return count;
    }

    /// <summary>True when this team has members and every single one of them is imprisoned.</summary>
    public static bool AllImprisoned(Team team)
    {
        return Count(team) > 0 && FreeCount(team) == 0;
    }

    /// <summary>
    /// The nearest free member of the opposing team, or null if there is none.
    /// Cheap on purpose: a plain index loop over at most four entries, no LINQ, no allocation.
    /// </summary>
    public static CharacterStatus NearestFreeEnemy(Team myTeam, Vector3 from)
    {
        List<CharacterStatus> foes = ListFor(myTeam.Opponent());
        CharacterStatus nearest = null;
        float nearestSqr = float.MaxValue;

        for (int i = 0; i < foes.Count; i++)
        {
            CharacterStatus foe = foes[i];
            if (foe == null || foe.isCaptured) continue;

            Vector3 p = foe.transform.position;
            float dx = p.x - from.x;
            float dz = p.z - from.z;
            float sqr = dx * dx + dz * dz;

            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = foe;
            }
        }

        return nearest;
    }

    /// <summary>
    /// The nearest imprisoned team-mate of this team, or null if nobody is imprisoned.
    /// The rescuer roles use this; the touch itself is still judged by MatchManager.
    /// </summary>
    public static CharacterStatus NearestCapturedTeammate(Team myTeam, Vector3 from)
    {
        List<CharacterStatus> mates = ListFor(myTeam);
        CharacterStatus nearest = null;
        float nearestSqr = float.MaxValue;

        for (int i = 0; i < mates.Count; i++)
        {
            CharacterStatus mate = mates[i];
            if (mate == null || !mate.isCaptured) continue;

            Vector3 p = mate.transform.position;
            float dx = p.x - from.x;
            float dz = p.z - from.z;
            float sqr = dx * dx + dz * dz;

            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = mate;
            }
        }

        return nearest;
    }

    // ======================================================================= roles

    /// <summary>The job this character is currently holding. FLEX when it has none.</summary>
    public static AiRole RoleOf(CharacterStatus character)
    {
        AiRole role;
        if (character != null && roles.TryGetValue(character, out role)) return role;
        return AiRole.Flex;
    }

    /// <summary>
    /// True for the one character on Blue that a person is driving. It is read from
    /// MatchManager rather than guessed from a name or a tag, so there is still only one place
    /// in the project that knows who the human is.
    /// </summary>
    public static bool IsHuman(CharacterStatus character)
    {
        return matchRef != null && character != null && matchRef.player == character;
    }

    /// <summary>Short, fixed-width name for the on-screen debug label and the roster log.</summary>
    public static string RoleName(AiRole role)
    {
        if (role == AiRole.Defender) return "DEFENDER";
        if (role == AiRole.Raider) return "RAIDER";
        if (role == AiRole.Rescuer) return "RESCUER";
        return "FLEX";
    }

    /// <summary>Re-derives the roles of both teams right now. Used at match start and whenever
    /// the roster is rebuilt; in play the Update timer calls it on its own.</summary>
    public static void ReviewRoles()
    {
        for (int t = 0; t < TeamUtil.All.Length; t++)
        {
            AssignRoles(TeamUtil.All[t]);
        }
    }

    /// <summary>
    /// Gives one team its jobs for this review.
    ///
    /// Everyone starts as FLEX, then each job the team holds is handed to the best free AI for
    /// it - the one nearest the flag it must defend, the flag it must take, or the prison it
    /// must reach. Because a member is struck off the candidate list once it has a job, one
    /// character can never hold two, and the roles therefore stay distinct.
    ///
    /// Prisoners are skipped, so a captured Rescuer hands its job to somebody else on the very
    /// next review - which is the whole reason the roles are re-derived rather than rotated.
    /// </summary>
    static void AssignRoles(Team team)
    {
        List<CharacterStatus> members = ListFor(team);
        Prune(members);

        bool hasHuman = false;
        int aiCount = 0;

        for (int i = 0; i < members.Count; i++)
        {
            CharacterStatus member = members[i];
            if (member == null) continue;

            if (IsHuman(member)) hasHuman = true;
            else aiCount++;

            // The default for everybody, so a character that loses its job this review simply
            // becomes a free agent rather than keeping a job it can no longer do.
            roles[member] = AiRole.Flex;
        }

        AiRole[] set = RoleSetFor(aiCount, hasHuman);
        if (set.Length == 0) return;

        // With no rules authority wired there is nothing to measure distance against, so the
        // jobs are handed out in roster order instead. The AI still works; it is just not
        // organised around the flags.
        assigned.Clear();

        for (int s = 0; s < set.Length; s++)
        {
            AiRole role = set[s];

            CharacterStatus pick = matchRef != null
                ? NearestFreeAiTo(team, AnchorFor(team, role))
                : NextFreeAi(team);

            if (pick == null) continue;   // fewer free AI than jobs: the rest stay FLEX

            if (Instance != null && Instance.logRoles && roles[pick] != role)
            {
                Debug.Log("[Role] " + pick.name + " (" + team + ") -> " + RoleName(role));
            }

            roles[pick] = role;
            assigned.Add(pick);
        }
    }

    /// <summary>The jobs a team holds, given how many AI it has and whether the human is on it.</summary>
    static AiRole[] RoleSetFor(int aiCount, bool hasHuman)
    {
        if (aiCount <= 0) return noRoles;

        if (hasHuman)
        {
            if (aiCount == 1) return withHuman1;
            if (aiCount == 2) return withHuman2;
            return withHuman3;
        }

        if (aiCount == 1) return noRoles;      // a lone AI has no job but FLEX: it must switch
        if (aiCount == 2) return aiOnly2;
        if (aiCount == 3) return aiOnly3;
        return aiOnly4;                        // 4th AI stays FLEX, as the plan requires
    }

    /// <summary>
    /// The place a job is measured from: the flag to defend, the flag to steal, or the prison
    /// to break into. A role's owner is simply the free AI standing nearest its own work.
    /// </summary>
    static Transform AnchorFor(Team team, AiRole role)
    {
        if (matchRef == null) return null;

        if (role == AiRole.Defender)
        {
            Flag own = matchRef.FlagOf(team);
            return own != null ? own.transform : null;
        }

        if (role == AiRole.Raider)
        {
            Flag enemy = matchRef.FlagOf(team.Opponent());
            return enemy != null ? enemy.transform : null;
        }

        if (role == AiRole.Rescuer) return matchRef.PrisonOf(team);

        return null;
    }

    /// <summary>
    /// The free AI nearest an anchor that has not already been given a job this review.
    /// Plain index loop over at most four entries: no LINQ, no allocation.
    /// </summary>
    static CharacterStatus NearestFreeAiTo(Team team, Transform anchor)
    {
        if (anchor == null) return null;

        List<CharacterStatus> members = ListFor(team);
        Vector3 from = anchor.position;

        CharacterStatus best = null;
        float bestSqr = float.MaxValue;

        for (int i = 0; i < members.Count; i++)
        {
            CharacterStatus member = members[i];
            if (!IsAvailable(member)) continue;

            Vector3 p = member.transform.position;
            float dx = p.x - from.x;
            float dz = p.z - from.z;
            float sqr = dx * dx + dz * dz;

            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = member;
            }
        }

        return best;
    }

    /// <summary>The next free AI in roster order that has not been given a job yet.</summary>
    static CharacterStatus NextFreeAi(Team team)
    {
        List<CharacterStatus> members = ListFor(team);

        for (int i = 0; i < members.Count; i++)
        {
            if (IsAvailable(members[i])) return members[i];
        }

        return null;
    }

    /// <summary>A character can be given a job only if it is free, not the human, and idle so far.</summary>
    static bool IsAvailable(CharacterStatus character)
    {
        if (character == null) return false;
        if (character.isCaptured) return false;      // a prisoner can do no job
        if (IsHuman(character)) return false;        // the human answers to nobody
        if (assigned.Contains(character)) return false;   // one job each
        return true;
    }

    // ==================================================================== internals

    static List<CharacterStatus> ListFor(Team team)
    {
        return team == Team.Blue ? blueMembers : redMembers;
    }

    /// <summary>Drops entries whose object was destroyed while the list was alive.</summary>
    static void Prune(List<CharacterStatus> list)
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i] == null) list.RemoveAt(i);
        }
    }
}
