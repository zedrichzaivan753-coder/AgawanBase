using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// A hold-to-sprint button for the touch HUD.
/// Put this on a round Image in the bottom-right corner. It simply reports whether a finger
/// is currently held on it through <see cref="IsHeld"/>; PlayerController does the rest.
///
/// It is built the same way as VirtualJoystick - a plain Image with pointer handlers - rather
/// than a UnityEngine.UI.Button, because a Button fires once on click and is awkward to hold.
/// </summary>
[RequireComponent(typeof(Image))]
public class SprintButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Tooltip("Colour while nothing is pressed. Same see-through white as the joystick.")]
    public Color normalColor = new Color(1f, 1f, 1f, 0.30f);

    [Tooltip("Colour while the button is held down (amber, so it is obvious sprint is on).")]
    public Color heldColor = new Color(0.95f, 0.72f, 0.25f, 0.85f);

    Image image;
    bool isHeld;

    /// <summary>True while a finger is down on this button.</summary>
    public bool IsHeld
    {
        get { return isHeld; }
    }

    void Awake()
    {
        image = GetComponent<Image>();
        Paint(normalColor);
    }

    // --- Unity UI pointer events ---

    public void OnPointerDown(PointerEventData eventData)
    {
        isHeld = true;
        Paint(heldColor);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Release();
    }

    /// <summary>Lets go of the button. Called on release and whenever the HUD is switched off.</summary>
    public void Release()
    {
        isHeld = false;
        Paint(normalColor);
    }

    void Paint(Color colour)
    {
        if (image != null) image.color = colour;
    }

    // If the HUD is switched off (paused, victory, game over) the button must not stay held,
    // or the player would start sprinting the instant the match resumes.
    void OnDisable()
    {
        Release();
    }
}
