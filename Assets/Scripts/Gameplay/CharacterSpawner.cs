using UnityEngine;

/// <summary>
/// Builds the roster at scene load: (TeamSize - 1) Blue AI plus TeamSize Red AI, every one of them
/// from the SAME enemy prefab.
///
/// Both teams therefore run literally the same brain - only the team, the home base, the two flags,
/// the jersey colour and the guard spot differ. That is the fairness rule in code: there is no way
/// for one side to end up with sharper team-mates than the other.
///
/// It spawns in Awake, so the bodies exist for the whole scene, exactly like the hand-placed enemies
/// they replace. They do not walk around before the match starts: EnemyAI gates itself on
/// TeamManager.MatchActive.
///
/// The human player is NOT spawned. BluePlayer is placed in the scene by hand, so the joystick
/// always has exactly one owner.
/// </summary>
public class CharacterSpawner : MonoBehaviour
{
    [Header("What to spawn")]
    [Tooltip("The AI character prefab. One prefab serves both teams.")]
    public GameObject aiPrefab;

    [Tooltip("Where spawned characters are parented, purely to keep the hierarchy tidy.")]
    public Transform rosterRoot;

    [Header("Team colours")]
    [Tooltip("Jersey material for a Blue AI.")]
    public Material blueJersey;

    [Tooltip("Jersey material for a Red AI.")]
    public Material redJersey;

    [Header("Scene references")]
    public BaseZone baseBlue;
    public BaseZone baseRed;
    public Flag flagBlue;
    public Flag flagRed;

    [Header("Formation")]
    [Tooltip("How far from the home base centre each spawn slot sits, in metres. The four slots " +
             "are a 2 x 2 square, so 1.1 puts team-mates 2.2 m apart - clear of a 1 m body.")]
    public float slotOffset = 1.1f;

    [Tooltip("How far from the flag each guard post sits. Kept inside the base, so a defender " +
             "patrolling around its post stays fresh.")]
    public float guardOffset = 1.4f;

    [Header("Debug")]
    [Tooltip("Log the roster that was built. Turn off for the final build.")]
    public bool logRoster = true;

    /// <summary>The four spawn slots of the 2 x 2 formation, in the order they are handed out.</summary>
    static readonly Vector2[] Slots =
    {
        new Vector2(-1f, -1f),
        new Vector2( 1f, -1f),
        new Vector2(-1f,  1f),
        new Vector2( 1f,  1f)
    };

    /// <summary>Hero guard posts, spread around the flag so four defenders never share one spot.</summary>
    static readonly Vector2[] GuardSpots =
    {
        new Vector2( 0f,  1f),
        new Vector2( 1f,  0f),
        new Vector2( 0f, -1f),
        new Vector2(-1f,  0f)
    };

    /// <summary>
    /// The choice the CURRENT roster was built with. The setup screen compares against these, so
    /// picking a different size or skill reloads the scene (the only way to rebuild a roster) while
    /// pressing START on the choice already on the field goes straight into the match.
    /// Static, so the values survive that reload.
    /// </summary>
    public static int SpawnedTeamSize { get; private set; }
    public static MatchDifficulty SpawnedDifficulty { get; private set; }

    /// <summary>Has anything been spawned yet? False until the first Awake, which is how the setup
    /// screen knows it must reload rather than trust a stale static.</summary>
    public static bool HasSpawned { get; private set; }

    void Awake()
    {
        // Read the remembered choice HERE rather than relying on GameManager having run first:
        // the order of two Awake methods is not defined, and spawning the wrong team size on the
        // first frame of a reloaded scene would be a real bug. Load() is a few PlayerPrefs reads.
        MatchSettings.Load();

        SpawnedTeamSize = MatchSettings.TeamSize;
        SpawnedDifficulty = MatchSettings.Difficulty;
        HasSpawned = true;

        if (aiPrefab == null)
        {
            Debug.LogError("CharacterSpawner: no AI prefab assigned, so the match has no AI at all.");
            return;
        }

        // Blue gets one fewer AI than Red, because the human already counts as one Blue body.
        // Indices continue from 1 for Blue (0 is the human), and start at 0 for Red.
        //
        // Two different flags are wired per body on purpose:
        //   EnemyAI.ourFlag   - this team's OWN flag, the one it defends and returns.
        //   EnemyAI.enemyFlag - the flag it is allowed to steal, which is what the STEAL and
        //                       CARRY states work on. It is also the one MatchManager will
        //                       actually hand it, because a character can only pick up the
        //                       OTHER team's flag.
        //   rig.flag          - the same enemy flag, which is what makes the carry pose appear
        //                       in the right hand.
        SpawnTeam(Team.Blue, MatchSettings.BlueAiCount, baseBlue, flagBlue, flagRed, blueJersey, 1);
        SpawnTeam(Team.Red, MatchSettings.RedAiCount, baseRed, flagRed, flagBlue, redJersey, 0);

        if (logRoster)
        {
            Debug.Log("[Spawn] " + MatchSettings.SizeName + " at " + MatchSettings.Difficulty +
                      " -> 1 human + " + MatchSettings.BlueAiCount + " Blue AI + " +
                      MatchSettings.RedAiCount + " Red AI = " + MatchSettings.TotalCharacters +
                      " characters.");
        }
    }

    /// <summary>
    /// Spawns one team's AI on the 2 x 2 formation around its own base and configures each body.
    /// ownFlag is the flag it guards; carryFlag is the enemy flag it is allowed to pick up.
    /// </summary>
    void SpawnTeam(Team team, int count, BaseZone homeBase, Flag ownFlag, Flag carryFlag,
                   Material jersey, int firstIndex)
    {
        if (homeBase == null)
        {
            Debug.LogError("CharacterSpawner: " + team + " has no home base assigned, so its AI " +
                           "cannot be spawned where they belong.");
            return;
        }

        Vector3 centre = homeBase.transform.position;

        for (int i = 0; i < count; i++)
        {
            int index = firstIndex + i;

            Vector2 slot = Slots[index % Slots.Length];
            Vector3 position = new Vector3(centre.x + slot.x * slotOffset, 0f,
                                           centre.z + slot.y * slotOffset);

            GameObject go = Instantiate(aiPrefab, position, Quaternion.identity, rosterRoot);
            go.name = (team == Team.Blue ? "Blue_AI_" : "Red_AI_") + (i + 1);

            CharacterStatus status = go.GetComponent<CharacterStatus>();
            CharacterRigAnimator rig = go.GetComponent<CharacterRigAnimator>();
            EnemyAI ai = go.GetComponent<EnemyAI>();

            if (status != null)
            {
                status.spawnIndex = index;
                status.homeBase = homeBase;
            }

            if (rig != null)
            {
                if (jersey != null) rig.teamJersey = jersey;

                // The rig only shows the carry pose for the flag whose carrier is this character,
                // and a character can only ever pick up the OTHER team's flag.
                rig.flag = carryFlag;
            }

            if (ai != null)
            {
                ai.ourFlag = ownFlag;      // defend this one
                ai.enemyFlag = carryFlag;  // steal this one

                Vector2 guard = GuardSpots[index % GuardSpots.Length];
                ai.guardOffset = new Vector2(guard.x * guardOffset, guard.y * guardOffset);

                // Team-mates must not all decide on the same frame: index / count of one decision
                // interval keeps them spread out, so the cost never spikes on a single frame.
                // Read AFTER the components are configured, because the difficulty preset in
                // EnemyAI.Awake is what decides how long that interval actually is.
                ai.StaggerFirstDecision(index * (ai.decisionInterval / Mathf.Max(1, count)));
            }

            // SetTeam runs LAST, because OnEnable has already filed this body under the prefab's
            // own default team by the time Instantiate returns.
            if (status != null) status.SetTeam(team);
        }
    }
}
