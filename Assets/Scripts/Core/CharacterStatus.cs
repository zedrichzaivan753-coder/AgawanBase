using UnityEngine;

/// <summary>
/// Tracks one character's "field time": how long it has been outside its own home base.
///
/// Rule of the game: outside your base your timer counts UP; inside it, your timer snaps to 0.
/// When two opponents touch, the LOWER fieldTime wins. So a character standing in its own
/// base always has 0 and can never lose a touch.
/// </summary>
[RequireComponent(typeof(CharacterMotor))]
public class CharacterStatus : MonoBehaviour
{
    /// <summary>Reference value for the HUD freshness bar (how stale counts as "empty").</summary>
    public const float MaxFieldTimeSeconds = 20f;

    [Header("Identity")]
    [Tooltip("Which team this character belongs to. Change it through SetTeam, never by hand, " +
             "so the TeamManager registry stays correct.")]
    public Team team = Team.Blue;

    [Tooltip("This character's own home base. Assigned per scene instance.")]
    public BaseZone homeBase;

    [Tooltip("This character's position in its team's roster (0 = first). The AI uses it to " +
             "stagger its decisions so two team-mates never think on the same frame.")]
    public int spawnIndex;

    [Header("State - read only")]
    [Tooltip("Seconds spent outside the home base. 0 means fully fresh and safe.")]
    public float fieldTime;

    [Tooltip("True while this character is locked inside the enemy prison.")]
    public bool isCaptured;

    [Tooltip("Seconds of rescue immunity left. While this is above 0 the tag rules skip this " +
             "character entirely, which is what covers the walk out of the prison.")]
    public float immunityTimer;

    [Tooltip("Seconds left before this character can be freed again. Set the moment it is " +
             "rescued, so the same prisoner cannot be freed over and over in a chain.")]
    public float rescueCooldownTimer;

    /// <summary>True while this character must not be tagged. Only ever set by a rescue.</summary>
    public bool IsImmune
    {
        get { return immunityTimer > 0f; }
    }

    /// <summary>True when the character is currently standing inside its own base.</summary>
    public bool IsInHomeBase
    {
        get { return homeBase != null && homeBase.Contains(transform.position); }
    }

    /// <summary>Freshness in 0..1, for the HUD bar. 1 = just reset, 0 = very stale.</summary>
    public float Freshness01
    {
        get { return Mathf.Clamp01(1f - (fieldTime / MaxFieldTimeSeconds)); }
    }

    void Update()
    {
        // Both timers tick whether the character is free or imprisoned. The rescue cooldown is a
        // property of the CHARACTER, not of the prison, so it always decays in real time - which
        // is what makes "3 s" mean the same thing no matter what happens in between.
        if (immunityTimer > 0f) immunityTimer -= Time.deltaTime;
        if (rescueCooldownTimer > 0f) rescueCooldownTimer -= Time.deltaTime;

        // A captured character is frozen in the enemy prison, so it never gets fresher.
        if (isCaptured) return;

        if (IsInHomeBase)
        {
            fieldTime = 0f;              // safe at home: fully fresh
        }
        else
        {
            fieldTime += Time.deltaTime; // in the field: getting staler every second
        }
    }

    /// <summary>
    /// Freezes this character inside the enemy prison.
    /// Called by MatchManager when this character loses a touch.
    /// </summary>
    public void Capture(Vector3 prisonPosition)
    {
        if (isCaptured) return;

        isCaptured = true;

        // A prisoner is never immune. The tag rules already refuse to capture an immune
        // character, so this only guards against an immunity left over from something else.
        immunityTimer = 0f;

        CharacterMotor motor = GetComponent<CharacterMotor>();
        motor.TeleportTo(prisonPosition);
        motor.SetFrozen(true);
    }

    /// <summary>
    /// Frees a captured character and stands it at the prison exit a rescuer has earned.
    ///
    /// WHERE it is placed is passed in rather than worked out here, because prison geometry is
    /// MatchManager's business - this script only knows about one character's state.
    ///
    /// fieldTime goes to 0 on purpose. It is what a release has always done, it is exactly what
    /// the rescuer's run across the map bought, and it makes the freed character FRESHER than
    /// the enemy camping the prison - so a rescue is a comeback rather than a re-capture loop.
    ///
    /// The immunity covers the walk out of the cage. It is NOT given to the rescuer: the rescuer
    /// is out in the field, its own fieldTime is climbing, and a free enemy may still tag it
    /// under the normal rule. That risk is the whole point of the rescue.
    /// </summary>
    public void Release(Vector3 exitPosition, float immunitySeconds, float rescueCooldownSeconds)
    {
        if (!isCaptured) return;

        isCaptured = false;
        fieldTime = 0f;

        immunityTimer = Mathf.Max(immunityTimer, immunitySeconds);
        rescueCooldownTimer = Mathf.Max(rescueCooldownTimer, rescueCooldownSeconds);

        CharacterMotor motor = GetComponent<CharacterMotor>();
        motor.SetFrozen(false);
        motor.TeleportTo(exitPosition);
    }

    // ---------------------------------------------------------------- team registry

    void OnEnable()
    {
        // A character files itself in the roster the moment it is switched on. Nothing in the
        // game ever has to search the scene for the members of a team.
        TeamManager.Register(this);
    }

    void OnDisable()
    {
        TeamManager.Unregister(this);
    }

    /// <summary>
    /// Moves this character to another team and re-files it in the registry.
    /// The spawner calls this right after instantiating, because OnEnable has already run by
    /// then with the prefab's own default team.
    /// </summary>
    public void SetTeam(Team next)
    {
        if (team == next) return;

        TeamManager.Unregister(this);
        team = next;
        TeamManager.Register(this);
    }
}
