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
    [Tooltip("Which team this character belongs to. Set on the prefab.")]
    public Team team = Team.Blue;

    [Tooltip("This character's own home base. Assigned per scene instance.")]
    public BaseZone homeBase;

    [Header("State - read only")]
    [Tooltip("Seconds spent outside the home base. 0 means fully fresh and safe.")]
    public float fieldTime;

    [Tooltip("True while this character is locked inside the enemy prison.")]
    public bool isCaptured;

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
        CharacterMotor motor = GetComponent<CharacterMotor>();
        motor.TeleportTo(prisonPosition);
        motor.SetFrozen(true);
    }

    /// <summary>
    /// Frees a captured character and puts it back in its home base.
    /// Not used by the current rules (a capture is permanent) - it exists so a future
    /// "rescue" feature has one obvious place to call.
    /// </summary>
    public void Release()
    {
        if (!isCaptured) return;

        isCaptured = false;
        fieldTime = 0f;
        CharacterMotor motor = GetComponent<CharacterMotor>();
        motor.SetFrozen(false);
        if (homeBase != null) motor.TeleportTo(homeBase.transform.position);
    }
}
