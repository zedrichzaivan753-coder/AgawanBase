using UnityEngine;

/// <summary>
/// One team's flag, and the whole of its little life: standing at home, riding above the
/// character who stole it, or lying on the ground waiting to be fetched back.
///
/// It deliberately decides NOTHING about the match. It never says who wins. MatchManager asks
/// it to pick up / drop / go home, and this script only moves the flag and remembers what it is
/// doing. That keeps the "one rule lives in one place" design the rest of the project uses.
///
///   AtBase  - standing on its home spot, waiting to be stolen.
///   Carried - riding above the character who picked it up.
///   Dropped - lying where the carrier was caught. It walks itself home after
///             returnAfterSeconds, which is the safety net that stops a match stalling.
/// </summary>
public class Flag : MonoBehaviour
{
    /// <summary>The three things a flag can be doing.</summary>
    public enum FlagState
    {
        /// <summary>Standing on its home spot, ready to be stolen.</summary>
        AtBase,

        /// <summary>Riding above the character who picked it up.</summary>
        Carried,

        /// <summary>Lying on the ground where the carrier was captured.</summary>
        Dropped
    }

    [Header("Identity")]
    [Tooltip("Which team owns this flag. Only the OTHER team can pick it up.")]
    public Team ownerTeam = Team.Red;

    [Header("Carrying")]
    [Tooltip("How high above the carrier's feet the flag rides, in metres. 1 puts the banner " +
             "above the carrier's head while the bottom of the pole looks held in his hand.")]
    public float carryHeight = 1f;

    [Header("Dropping")]
    [Tooltip("Seconds a dropped flag lies on the ground before it goes home by itself. " +
             "This is the safety net that stops a match stalling when nobody fetches it.")]
    public float returnAfterSeconds = 10f;

    [Tooltip("How far the pole tips over while the flag is lying dropped, in degrees.")]
    public float droppedTiltDegrees = 70f;

    [Tooltip("How quickly the pole tips over and straightens up again, in degrees per second.")]
    public float tiltSpeed = 240f;

    [Header("Debug")]
    [Tooltip("Log every state change with the position. Turn off for the final build.")]
    public bool logFlagEvents = true;

    /// <summary>
    /// Where this flag lives. Recorded once at startup so a carried flag can always find its way home.
    /// </summary>
    public Vector3 HomePosition { get; private set; }

    /// <summary>What the flag is doing right now.</summary>
    public FlagState state { get; private set; }

    /// <summary>Who is carrying it, or null when nobody is.</summary>
    public Transform carrier { get; private set; }

    /// <summary>Seconds left before a dropped flag returns home by itself. 0 unless it is Dropped.</summary>
    public float DropTimer { get; private set; }

    /// <summary>True while somebody is carrying this flag.</summary>
    public bool IsCarried { get { return state == FlagState.Carried; } }

    /// <summary>True while this flag is lying on the ground.</summary>
    public bool IsDropped { get { return state == FlagState.Dropped; } }

    Quaternion homeRotation;
    float tilt;          // current lean of the pole, in degrees
    float tiltTarget;    // where the lean is heading

    // The carrier's stamina, remembered when the flag is taken so the carry penalty can be
    // switched on and off. Cached once here instead of being looked up every frame.
    SprintStamina carrierStamina;

    void Awake()
    {
        HomePosition = transform.position;
        homeRotation = transform.rotation;
        state = FlagState.AtBase;
        tilt = 0f;
        tiltTarget = 0f;
    }

    void Update()
    {
        // Only a dropped flag has anything to do: count down towards going home.
        // While the game is paused Time.deltaTime is 0, so the countdown freezes on its own.
        if (state == FlagState.Dropped)
        {
            DropTimer -= Time.deltaTime;

            if (DropTimer <= 0f)
            {
                if (logFlagEvents)
                {
                    Debug.Log("[Flag] " + name + " was left on the ground too long -> it goes home");
                }

                ReturnHome();
                return;
            }
        }

        // Tip the pole over when dropped, and stand it back up once home or picked up.
        if (!Mathf.Approximately(tilt, tiltTarget))
        {
            tilt = Mathf.MoveTowards(tilt, tiltTarget, tiltSpeed * Time.deltaTime);
            transform.rotation = homeRotation * Quaternion.Euler(0f, 0f, tilt);
        }
    }

    void LateUpdate()
    {
        // LateUpdate, not Update: the carrier has already finished moving this frame, so the
        // flag cannot trail a frame behind him even while he sprints.
        if (state != FlagState.Carried || carrier == null) return;

        transform.position = carrier.position + Vector3.up * carryHeight;

        // The flag keeps its OWN rotation rather than copying the carrier's. The banner is a flat
        // panel, so turning with the player would swing it edge-on and make it invisible.
    }

    // ------------------------------------------------------------------- commands

    /// <summary>
    /// Tries to take this flag. Refuses while it is already carried, and refuses a character
    /// from the owning team - you cannot steal your own flag.
    /// </summary>
    public bool TryPickUp(Transform who, Team whoTeam)
    {
        if (who == null) return false;
        if (state == FlagState.Carried) return false;
        if (whoTeam == ownerTeam) return false;

        carrier = who;
        tiltTarget = 0f;      // stand the pole up as the flag is lifted
        DropTimer = 0f;       // no countdown is running while it is being carried

        // Switch the carry penalty on for whoever took it.
        carrierStamina = who.GetComponent<SprintStamina>();
        if (carrierStamina != null) carrierStamina.IsCarryingFlag = true;

        SetState(FlagState.Carried);
        return true;
    }

    /// <summary>
    /// Drops the flag at <paramref name="where"/>, lying on the ground.
    /// IMPORTANT: call this BEFORE teleporting the carrier to prison, or the flag lands in prison.
    /// </summary>
    public void Drop(Vector3 where)
    {
        if (state != FlagState.Carried) return;

        ForgetCarrier();

        // The ground height comes from home rather than from the carrier, who may be halfway
        // through a teleport when this is called.
        transform.position = new Vector3(where.x, HomePosition.y, where.z);

        DropTimer = returnAfterSeconds;
        tiltTarget = droppedTiltDegrees;

        SetState(FlagState.Dropped);
    }

    /// <summary>
    /// Puts the flag back on its home spot, upright and free to be stolen again.
    /// Every return path uses this, and so does GameManager to reset on Title / Victory / Game Over.
    /// </summary>
    public void ReturnHome()
    {
        ForgetCarrier();

        transform.position = HomePosition;
        transform.rotation = homeRotation;
        tilt = 0f;
        tiltTarget = 0f;
        DropTimer = 0f;

        SetState(FlagState.AtBase);
    }

    // ------------------------------------------------------------------ internals

    /// <summary>Lets go of the carrier and cancels the carry penalty.</summary>
    void ForgetCarrier()
    {
        if (carrierStamina != null) carrierStamina.IsCarryingFlag = false;
        carrierStamina = null;
        carrier = null;
    }

    void SetState(FlagState next)
    {
        if (state == next) return;

        if (logFlagEvents)
        {
            Debug.Log("[Flag] " + name + " " + state + " -> " + next +
                      "  at " + transform.position.ToString("F1"));
        }

        state = next;
    }
}
