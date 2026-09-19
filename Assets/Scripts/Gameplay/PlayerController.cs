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

    [Tooltip("Editor/desktop only: also allow WASD and the arrow keys so you can test without a touch screen.")]
    public bool allowKeyboardFallback = true;

    CharacterMotor motor;
    CharacterStatus status;

    void Awake()
    {
        motor = GetComponent<CharacterMotor>();
        status = GetComponent<CharacterStatus>();
    }

    void Update()
    {
        // A captured player is frozen in prison and must not move at all.
        if (status.isCaptured)
        {
            motor.Move(Vector2.zero);
            return;
        }

        Vector2 input = joystick != null ? joystick.Value : Vector2.zero;

        // No touch input? Fall back to the keyboard so the game is testable on a PC.
        if (allowKeyboardFallback && input.sqrMagnitude < 0.01f)
        {
            input = ReadKeyboard();
        }

        motor.Move(Vector2.ClampMagnitude(input, 1f));
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
