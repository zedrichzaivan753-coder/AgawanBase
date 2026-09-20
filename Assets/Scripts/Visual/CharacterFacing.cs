using UnityEngine;

/// <summary>
/// Turns a character's VISUAL MODEL so it faces the way it is walking.
///
/// Purely cosmetic. It rotates the "Visual" CHILD, never the root, and it never writes anything
/// the game reads. That matters for three reasons:
///   - the CharacterController, the colliders and every gameplay script stay on an untouched,
///     axis-aligned root, so movement, tag distances and AI paths are unaffected;
///   - the CharacterIndicator (ring, icon and arrow) hangs on the ROOT as a sibling of Visual,
///     so the ground ring keeps pointing the same way in the world instead of spinning with
///     the body - which is exactly what makes the facing readable;
///   - there is only one place in the project that turns a character, so nothing can fight it.
///
/// HOW IT KNOWS WHERE TO TURN: it reads CharacterMotor.MoveDirection, which the motor fills in
/// for the player (from the joystick) and for the AI (from the direction its brain chose) alike.
/// It never reads the root transform, so the player and the AI turn by one identical rule.
///
/// There is NO NavMeshAgent in this project - both sides walk through CharacterMotor - so there
/// is no second rotation authority to disagree with. If one is ever added, set
/// agent.updateRotation = false (leave updatePosition ON) or the agent will overwrite this.
/// </summary>
[RequireComponent(typeof(CharacterMotor))]
public class CharacterFacing : MonoBehaviour
{
    [Tooltip("The child holding the low-poly body. Left empty, the child named 'Visual' is found " +
             "at startup.")]
    public Transform visual;

    [Tooltip("Degrees per second the model turns. 720 turns a full about-face in about a quarter " +
             "of a second - fast enough to feel immediate, slow enough to read as a turn.")]
    public float turnSpeedDegreesPerSecond = 720f;

    [Tooltip("Movement shorter than this counts as standing still, so the model HOLDS its last " +
             "facing instead of snapping to a direction that barely exists. This is also what " +
             "stops it jittering on the spot.")]
    public float minMoveMagnitude = 0.1f;

    [Tooltip("Draw a line from the character along the way the MODEL is pointing, in the Scene view.")]
    public bool drawFacingGizmo;

    [Tooltip("Warn once if no 'Visual' child can be found. Turn off for the final build.")]
    public bool logWarnings = true;

    CharacterMotor motor;
    CharacterStatus status;

    void Awake()
    {
        motor = GetComponent<CharacterMotor>();
        status = GetComponent<CharacterStatus>();

        // The rig builder creates the child under this exact name, so no prefab wiring is needed.
        if (visual == null) visual = transform.Find("Visual");

        if (visual == null && logWarnings)
        {
            Debug.LogWarning("CharacterFacing: '" + name + "' has no child called 'Visual', so its " +
                             "model cannot turn. Assign the 'visual' field by hand.");
        }
    }

    void LateUpdate()
    {
        if (visual == null || motor == null) return;

        // A prisoner is frozen where it was teleported, and must stay exactly as it was placed.
        if (status != null && status.isCaptured) return;

        Vector2 direction = motor.MoveDirection;

        // Standing still (or barely moving): keep the last facing. Returning here is what stops
        // the model from creeping round or twitching when the joystick is at rest.
        if (direction.sqrMagnitude < minMoveMagnitude * minMoveMagnitude) return;

        // LateUpdate, not Update: the motor has already moved the root this frame, so the model
        // is turned towards the movement that has actually happened rather than towards the
        // movement that was about to happen. Pausing costs nothing - Time.timeScale = 0 gives a
        // deltaTime of 0, so no rotation is applied at all while the pause menu is up.
        Quaternion wanted = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.y));

        // RotateTowards turns at a CONSTANT rate and lands exactly on the wanted rotation, so a
        // 180 degree about-face takes the short way round and never oscillates at the end.
        visual.localRotation = Quaternion.RotateTowards(visual.localRotation, wanted,
                                                        turnSpeedDegreesPerSecond * Time.deltaTime);
    }

    /// <summary>
    /// Shows which way the model is pointing. The camera's yaw is 0, so -Z on screen is "up the
    /// pitch": the yellow line and the yellow arrow in the Scene view agree.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (!drawFacingGizmo) return;

        Transform v = visual != null ? visual : transform.Find("Visual");
        if (v == null) return;

        Gizmos.color = Color.yellow;
        Vector3 from = transform.position + Vector3.up * 1.6f;
        Gizmos.DrawLine(from, from + v.forward * 1.5f);
    }
}
