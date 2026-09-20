using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Turns the on-screen joystick into movement for the Blue player.
/// It does nothing else: the actual moving is handled by CharacterMotor.
/// </summary>
[RequireComponent(typeof(CharacterMotor))]
[RequireComponent(typeof(CharacterStatus))]
public class PlayerController : MonoBehaviour
{
    [Tooltip("The on-screen joystick. Assigned on the scene instance.")]
    public VirtualJoystick joystick;

    [Tooltip("The hold-to-sprint button. Assigned on the scene instance.")]
    public SprintButton sprintButton;

    [Tooltip("Editor/desktop only: also allow WASD and the arrow keys so you can test without a touch screen.")]
    public bool allowKeyboardFallback = true;

    [Tooltip("Editor/desktop only: allow Left/Right Shift to sprint, so sprinting is testable on a PC.")]
    public bool allowSprintKeyboardFallback = true;

    CharacterMotor motor;
    CharacterStatus status;
    SprintStamina sprint;

    void Awake()
    {
        motor = GetComponent<CharacterMotor>();
        status = GetComponent<CharacterStatus>();
        sprint = GetComponent<SprintStamina>();

        // The SPRINT button lives on the HUD, and the HUD is switched OFF while the title
        // screen is showing, so the search has to include inactive objects.
        // Assigning the field by hand in the Inspector still wins if you prefer.
        if (sprintButton == null)
        {
            sprintButton = Object.FindAnyObjectByType<SprintButton>(FindObjectsInactive.Include);
            if (sprintButton == null)
            {
                Debug.LogWarning("PlayerController: no SprintButton found in the scene, " +
                                 "so the player will never be able to sprint.");
            }
        }
    }

    void Update()
    {
        // A captured player is frozen in prison and must not move at all.
        if (status.isCaptured)
        {
            SetSprint(false);
            motor.Move(Vector2.zero);
            return;
        }

        Vector2 input = joystick != null ? joystick.Value : Vector2.zero;

        // No touch input? Fall back to the keyboard so the game is testable on a PC.
        if (allowKeyboardFallback && input.sqrMagnitude < 0.01f)
        {
            input = ReadKeyboard();
        }

        // Tell the shared stamina component whether we are holding SPRINT. It decides whether
        // that is allowed, drains the stamina, and speeds the motor up. This script never
        // changes the speed itself, so the player and the AI sprint by exactly the same rules.
        SetSprint(IsSprintHeld());

        motor.Move(Vector2.ClampMagnitude(input, 1f));
    }

    /// <summary>True while the SPRINT button (or Shift, in the editor) is being held down.</summary>
    bool IsSprintHeld()
    {
        if (sprintButton != null && sprintButton.IsHeld) return true;

        if (allowSprintKeyboardFallback)
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed)) return true;
        }

        return false;
    }

    void SetSprint(bool wants)
    {
        if (sprint != null) sprint.WantsSprint = wants;
    }

    void OnDisable()
    {
        // Pausing, winning, losing or restarting switches this component off. Letting go here
        // means the player can never come back from a menu already sprinting.
        SetSprint(false);
    }

    /// <summary>WASD / arrow keys as a Vector2, using the Input System package.</summary>
    Vector2 ReadKeyboard()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return Vector2.zero;

        float x = 0f;
        float y = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) y -= 1f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) y += 1f;

        return new Vector2(x, y);
    }
}
