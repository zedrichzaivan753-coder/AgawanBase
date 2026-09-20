using UnityEngine;

/// <summary>
/// Animates the low-poly kid rig that hangs under a character.
///
/// This script is purely cosmetic. It NEVER moves the character: it only rotates and
/// nudges the child parts of the "Visual" object, so CharacterMotor stays the only thing
/// that decides where a character is. Nothing it touches has a collider.
///
/// It reads three things, all of which already existed:
///   CharacterMotor.IsMoving    - true when the character was asked to walk this frame
///   CharacterStatus.isCaptured - true while frozen in prison
///   flag.carrier               - who is carrying the flag (optional reference)
///
/// States it shows:
///   walking   - legs swing, arms swing opposite, torso bobs
///   standing  - everything settles back to rest
///   captured  - jersey turns grey, legs stop, torso slumps forward
///   carrying  - the right arm is held up so "who has the flag" is readable at a glance
/// </summary>
public class CharacterRigAnimator : MonoBehaviour
{
    [Header("Parts of the rig (filled in by the rig builder)")]
    [Tooltip("The child that holds the whole low-poly body.")]
    public Transform visual;

    [Tooltip("Left and right leg cubes.")]
    public Transform legLeft;
    public Transform legRight;

    [Tooltip("Left and right arm cubes.")]
    public Transform armLeft;
    public Transform armRight;

    [Tooltip("The capsule torso. It bobs while walking and slumps while captured.")]
    public Transform torso;

    [Tooltip("Both team-coloured pieces: the torso capsule and the jersey hem. " +
             "They swap material while this character is captured.")]
    public Renderer jerseyA;
    public Renderer jerseyB;

    [Header("Look")]
    [Tooltip("Team jersey material. Swapped back automatically when the character is freed.")]
    public Material teamJersey;

    [Tooltip("Grey material used while captured, so a prisoner never reads as a player.")]
    public Material capturedJersey;

    [Header("Walk - all tunable")]
    [Tooltip("Degrees the legs swing each way while walking.")]
    public float legSwingDegrees = 25f;

    [Tooltip("Walk cycles per second at full speed.")]
    public float stepRate = 2.2f;

    [Tooltip("Degrees the arms swing each way while walking.")]
    public float armSwingDegrees = 18f;

    [Tooltip("Metres the torso rises and falls while walking.")]
    public float bobHeight = 0.04f;

    [Tooltip("How quickly the pose moves towards the wanted one. Higher = snappier.")]
    public float poseLerpSpeed = 12f;

    [Header("Prison pose")]
    [Tooltip("Degrees the torso leans forward while captured.")]
    public float capturedSlumpDegrees = 18f;

    [Header("Flag carry pose")]
    [Tooltip("Degrees the right arm is raised by while carrying the flag.")]
    public float carryArmDegrees = -150f;

    [Header("Flag (optional)")]
    [Tooltip("This team's flag. Left empty, the carry pose is simply never shown.")]
    public Flag flag;

    [Header("State - read only")]
    [Tooltip("True while the carry pose is being shown.")]
    public bool isCarrying;

    CharacterMotor motor;
    CharacterStatus status;

    float swingPhase;
    float legAngle;
    Vector3 torsoRestPosition;
    Quaternion legLeftRest, legRightRest, armLeftRest, armRightRest, torsoRest;

    void Awake()
    {
        motor = GetComponent<CharacterMotor>();
        status = GetComponent<CharacterStatus>();

        if (legLeft != null) legLeftRest = legLeft.localRotation;
        if (legRight != null) legRightRest = legRight.localRotation;
        if (armLeft != null) armLeftRest = armLeft.localRotation;
        if (armRight != null) armRightRest = armRight.localRotation;
        if (torso != null)
        {
            torsoRest = torso.localRotation;
            torsoRestPosition = torso.localPosition;
        }
    }

    void Start()
    {
        // The flag lives on a SCENE object, so a prefab asset cannot hold a reference to it.
        // If the field was left empty, find this character's ENEMY flag: you can only ever
        // carry the other team's flag, so that is unambiguous. One-off cost in Start only.
        if (flag != null) return;

        Team myTeam = status != null ? status.team : Team.Blue;
        Flag[] all = Object.FindObjectsByType<Flag>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].ownerTeam != myTeam)
            {
                flag = all[i];
                return;
            }
        }
    }

    void Update()
    {
        // Unity's paused state uses timeScale 0, so this freezes for free while paused.
        float dt = Time.deltaTime;

        bool captured = status != null && status.isCaptured;
        bool moving = !captured && motor != null && motor.IsMoving;

        isCarrying = !captured && flag != null && flag.IsCarried &&
                     flag.carrier != null && flag.carrier == transform;

        // ---- walk cycle ----
        if (moving)
        {
            swingPhase += dt * stepRate * Mathf.PI * 2f;
            if (swingPhase > Mathf.PI * 2f) swingPhase -= Mathf.PI * 2f;
        }
        else
        {
            // Settle the swing back to zero so a stopped character stands still.
            swingPhase = Mathf.MoveTowards(swingPhase, 0f, dt * stepRate * Mathf.PI * 2f);
        }

        float targetLegAngle = moving ? Mathf.Sin(swingPhase) * legSwingDegrees : 0f;
        float targetArmAngle = moving ? -Mathf.Sin(swingPhase) * armSwingDegrees : 0f;

        legAngle = Mathf.Lerp(legAngle, targetLegAngle, dt * poseLerpSpeed);

        if (legLeft != null)
            legLeft.localRotation = legLeftRest * Quaternion.Euler(legAngle, 0f, 0f);
        if (legRight != null)
            legRight.localRotation = legRightRest * Quaternion.Euler(-legAngle, 0f, 0f);

        // ---- arms ----
        float armLeftAngle = targetArmAngle;
        float armRightAngle = isCarrying ? carryArmDegrees : -targetArmAngle;

        if (armLeft != null)
            armLeft.localRotation = armLeftRest * Quaternion.Euler(armLeftAngle, 0f, 0f);
        if (armRight != null)
            armRight.localRotation = armRightRest * Quaternion.Euler(armRightAngle, 0f, 0f);

        // ---- torso bob and slump ----
        if (torso != null)
        {
            float wantedBob = moving ? Mathf.Abs(Mathf.Sin(swingPhase)) * bobHeight : 0f;
            Vector3 wantedPos = torsoRestPosition + new Vector3(0f, wantedBob, 0f);
            torso.localPosition = Vector3.Lerp(torso.localPosition, wantedPos, dt * poseLerpSpeed);

            float wantedPitch = captured ? capturedSlumpDegrees : 0f;
            Quaternion wantedRot = torsoRest * Quaternion.Euler(wantedPitch, 0f, 0f);
            torso.localRotation = Quaternion.Slerp(torso.localRotation, wantedRot, dt * poseLerpSpeed);
        }

        // ---- captured colour ----
        // Only assigned when it actually changes: setting sharedMaterial every frame would
        // invalidate batching and allocate nothing but still cost a state change.
        if (!captured && jerseyA != null && jerseyB != null)
        {
            if (jerseyA.sharedMaterial != teamJersey) jerseyA.sharedMaterial = teamJersey;
            if (jerseyB.sharedMaterial != teamJersey) jerseyB.sharedMaterial = teamJersey;
        }
    }

    void LateUpdate()
    {
        // The colour swap lives here rather than in Update so a capture that happens in
        // MatchManager.Update is shown in the very same frame.
        bool captured = status != null && status.isCaptured;
        if (captured && capturedJersey != null)
        {
            if (jerseyA != null && jerseyA.sharedMaterial != capturedJersey)
                jerseyA.sharedMaterial = capturedJersey;
            if (jerseyB != null && jerseyB.sharedMaterial != capturedJersey)
                jerseyB.sharedMaterial = capturedJersey;
        }
    }
}
