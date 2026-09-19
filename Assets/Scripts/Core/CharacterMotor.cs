using UnityEngine;

/// <summary>
/// The ONLY place that physically moves a character.
/// It wraps Unity's CharacterController so that PlayerController and EnemyAI can both
/// simply say "move in this direction". It also owns prison teleporting and the frozen flag.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class CharacterMotor : MonoBehaviour
{
    [Tooltip("Movement speed in metres per second. Kept identical for player and AI so a chase is a fair race.")]
    public float moveSpeed = 6f;

    [Tooltip("Small constant downward speed that keeps the character glued to the ground.")]
    public float groundStickSpeed = -2f;

    CharacterController controller;
    bool isFrozen;

    /// <summary>True while this character is not allowed to move (captured in prison).</summary>
    public bool IsFrozen
    {
        get { return isFrozen; }
    }

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
        if (isFrozen || controller == null) return;

        // Work out this frame's motion: sideways from the input, plus a small downward push.
        Vector3 motion = new Vector3(input.x, 0f, input.y) * moveSpeed;
        motion.y = groundStickSpeed;

        controller.Move(motion * Time.deltaTime);

        // Turn to face the way we are walking. Only the Y axis matters in a top-down game.
        if (input.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(new Vector3(input.x, 0f, input.y));
        }
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
    }
}
