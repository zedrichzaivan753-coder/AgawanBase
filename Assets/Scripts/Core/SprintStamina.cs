using UnityEngine;

/// <summary>
/// THE one sprint + stamina component. The Blue player and both Red enemies all use this
/// same script - it is shared, not copied, so the sprint rules can never drift apart.
///
/// It does three jobs and nothing else:
///   1. Drains stamina while the character is actually sprinting.
///   2. Refills stamina after a short delay once sprinting stops.
///   3. Tells CharacterMotor how much faster to move (speedMultiplier).
///
/// Whoever owns the character (PlayerController or EnemyAI) only has to set
/// <see cref="WantsSprint"/>. Everything else - the lock-out, the delay, the speed - is
/// handled here, so the player and the AI are guaranteed to sprint by the same rules.
/// </summary>
[RequireComponent(typeof(CharacterMotor))]
public class SprintStamina : MonoBehaviour
{
    [Header("Stamina")]
    [Tooltip("Full stamina value. Stamina is stored as 0..maxStamina, not as 0..1.")]
    public float maxStamina = 2f;

    [Tooltip("How much stamina is used up per second of sprinting.")]
    public float drainPerSecond = 1f;

    [Tooltip("How much stamina comes back per second, once regeneration has started.")]
    public float regenPerSecond = 0.7f;

    [Tooltip("Seconds to wait after sprinting stops before stamina starts coming back.")]
    public float regenDelay = 0.6f;

    [Tooltip("Stamina needed before a sprint may START again after running out. " +
             "This is the lock-out that stops the player tapping sprint over and over.")]
    public float restartThreshold = 0.4f;

    [Header("Sprint")]
    [Tooltip("Movement speed multiplier while sprinting. 1.6 = 60% faster than walking.")]
    public float sprintSpeedMultiplier = 1.6f;

    [Header("Carrying a flag")]
    [Tooltip("Set by Flag when this character takes an enemy flag, and cleared when he lets it go. " +
             "Do not set this by hand.")]
    public bool IsCarryingFlag { get; set; }

    [Tooltip("Movement speed multiplier while carrying a flag. 1 = carrying costs no speed at all. " +
             "Deliberately left at 1: the chaser and the carrier share the same top speed, so any " +
             "speed penalty would mean the carrier is always caught and the run home could never " +
             "be won. Running out of stamina is what makes the trip hard, not slow legs.")]
    public float carrySpeedMultiplier = 1f;

    [Tooltip("Stamina regeneration multiplier while carrying a flag. 0.5 = the bar refills half as " +
             "fast, so a long run out to the enemy flag leaves you too winded to sprint all the way " +
             "home. This is the carry penalty. Set it to 1 for no penalty at all.")]
    public float carryRegenMultiplier = 0.5f;

    [Header("Debug")]
    [Tooltip("Logs every sprint start and stop, with the stamina value. Turn off for the final build.")]
    public bool logSprint;

    CharacterMotor motor;
    float stamina;

    // Counts down after a sprint ends. While it is above 0 nothing regenerates.
    float regenDelayTimer;

    // The lock-out flag: set when stamina hits 0, cleared only once stamina has
    // refilled past restartThreshold. That is what makes sprinting impossible to spam.
    bool lockedOut;

    /// <summary>Set by the owner: "I would like to sprint right now". Read every frame.</summary>
    public bool WantsSprint { get; set; }

    /// <summary>True while this character is actually sprinting (wanted AND allowed).</summary>
    public bool IsSprinting { get; private set; }

    /// <summary>
    /// True when a sprint is allowed to start right now: there is stamina left and the
    /// lock-out has been cleared. The AI uses this to decide whether it can chase at all.
    /// </summary>
    public bool CanSprint
    {
        get { return !lockedOut && stamina > 0f; }
    }

    /// <summary>Stamina as 0..1, for the HUD bar and for the AI's thresholds.</summary>
    public float Stamina01
    {
        get { return maxStamina > 0f ? Mathf.Clamp01(stamina / maxStamina) : 0f; }
    }

    void Awake()
    {
        motor = GetComponent<CharacterMotor>();
        stamina = maxStamina;
        motor.speedMultiplier = 1f;

        // Guard against a tuning mistake that would permanently disable sprinting.
        if (restartThreshold >= maxStamina)
        {
            Debug.LogWarning(name + ": SprintStamina.restartThreshold (" + restartThreshold +
                             ") is not below maxStamina (" + maxStamina +
                             "), so sprinting could never start again. Lower restartThreshold.");
        }
    }

    void Update()
    {
        // A sprint needs three things: the owner wants it, the character is not frozen in
        // prison, and the character is actually trying to walk somewhere. That last check is
        // what stops stamina draining while standing still with a finger on the SPRINT button.
        bool wants = WantsSprint && !motor.IsFrozen && motor.IsMoving;

        bool wasSprinting = IsSprinting;
        IsSprinting = wants && CanSprint;

        if (IsSprinting)
        {
            stamina -= drainPerSecond * Time.deltaTime;

            if (stamina <= 0f)
            {
                stamina = 0f;
                lockedOut = true;   // must refill past restartThreshold before sprinting again
            }

            regenDelayTimer = regenDelay;   // keep the delay topped up while sprinting
        }
        else
        {
            // Not sprinting: wait out the delay, then refill. Carrying a flag slows the refill
            // down, and that slower refill IS the carry penalty.
            if (regenDelayTimer > 0f) regenDelayTimer -= Time.deltaTime;
            else stamina += regenPerSecond * (IsCarryingFlag ? carryRegenMultiplier : 1f) * Time.deltaTime;
        }

        stamina = Mathf.Clamp(stamina, 0f, maxStamina);

        // The lock-out only lifts once the stamina bar has refilled far enough.
        if (lockedOut && stamina >= restartThreshold) lockedOut = false;

        // Hand the speed change to the motor. 1 means normal walking speed. The carry multiplier
        // applies to both walking and sprinting, and is 1 unless a designer lowers it.
        float carrySpeed = IsCarryingFlag ? carrySpeedMultiplier : 1f;
        motor.speedMultiplier = (IsSprinting ? sprintSpeedMultiplier : 1f) * carrySpeed;

        if (logSprint && IsSprinting != wasSprinting)
        {
            Debug.Log("[Sprint] " + name + (IsSprinting ? " SPRINT ON" : " sprint off") +
                      "  stamina=" + stamina.ToString("F2") + "/" + maxStamina.ToString("F2") +
                      "  lockedOut=" + lockedOut);
        }
    }

    void OnDisable()
    {
        // If this component is switched off mid-sprint, do not leave the motor sped up.
        IsSprinting = false;
        WantsSprint = false;
        if (motor != null) motor.speedMultiplier = 1f;
    }
}
