using UnityEngine;

/// <summary>
/// The ONLY place that physically moves a character.
/// It wraps Unity's CharacterController so that PlayerController and EnemyAI can both
/// simply say "move in this direction". It also owns prison teleporting and the frozen flag.
///
/// It does NOT turn the character any more. Turning is cosmetic, so it lives in CharacterFacing,
/// which rotates only the "Visual" child. That keeps the ring, the icon and the collider
/// axis-aligned, which is what makes the direction a character is facing readable at a glance.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class CharacterMotor : MonoBehaviour
{
    [Tooltip("Movement speed in metres per second. Kept identical for player and AI so a chase is a fair race.")]
    public float moveSpeed = 6f;

    [Tooltip("Small constant downward speed that keeps the character glued to the ground.")]
    public float groundStickSpeed = -2f;

    [Tooltip("Live speed multiplier. SprintStamina raises this while a character is sprinting. " +
             "Leave it at 1 for normal walking speed.")]
    public float speedMultiplier = 1f;

    CharacterController controller;
    bool isFrozen;

    /// <summary>True while this character is not allowed to move (captured in prison).</summary>
    public bool IsFrozen
    {
        get { return isFrozen; }
    }

    /// <summary>
    /// True when the last <see cref="Move"/> call was given a real direction.
    /// SprintStamina reads this so that standing still never drains stamina.
    /// </summary>
    public bool IsMoving { get; private set; }

    /// <summary>
    /// The direction this character was last asked to walk, on the ground plane
    /// (x = world X, y = world Z). Zero while standing still or frozen.
    ///
    /// CharacterFacing reads this to turn the model, so the player and the AI turn by exactly
    /// the same rule and neither of them has to look at the root transform.
    /// </summary>
    public Vector2 MoveDirection { get; private set; }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    /// <summary>
    /// Moves the character. <paramref name="input"/> is a direction on the ground plane
    /// (x = world X, y = world Z). Keep its length at 1 or less.
    /// </summary>
    public void Move(Vector2 input)
    {
        if (isFrozen || controller == null)
        {
            IsMoving = false;
            MoveDirection = Vector2.zero;
            return;
        }

        // Remember whether we were actually asked to walk this frame. SprintStamina uses it.
        IsMoving = input.sqrMagnitude > 0.0001f;

        // The direction this frame's walk is going. CharacterFacing turns the model with it.
        MoveDirection = IsMoving ? input : Vector2.zero;

        // Work out this frame's motion: sideways from the input, plus a small downward push.
        // speedMultiplier is 1 unless SprintStamina has raised it for a sprint.
        Vector3 motion = new Vector3(input.x, 0f, input.y) * (moveSpeed * speedMultiplier);
        motion.y = groundStickSpeed;

        controller.Move(motion * Time.deltaTime);
    }

    /// <summary>
    /// Instantly places the character at worldPosition. Used for the prison teleport --
    /// the CharacterController has to be switched off for one frame or it overrides the position.
    /// </summary>
    public void TeleportTo(Vector3 worldPosition)
    {
        if (controller == null) controller = GetComponent<CharacterController>();

        controller.enabled = false;
        transform.position = worldPosition;
        controller.enabled = true;
    }

    /// <summary>Stop (true) or allow (false) movement. Captured characters are frozen.</summary>
    public void SetFrozen(bool frozen)
    {
        isFrozen = frozen;

        // A frozen character is not walking anywhere, so it must not claim a direction either.
        if (frozen)
        {
            IsMoving = false;
            MoveDirection = Vector2.zero;
        }
    }
}
