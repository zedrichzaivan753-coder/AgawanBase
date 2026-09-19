using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// A simple on-screen joystick for touch screens.
/// Put this on the round background. The player drags anywhere on the background and the
/// script reports a clamped direction in <see cref="Value"/> (-1..1 on each axis).
/// </summary>
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Tooltip("The small circle that follows the finger. Usually a child Image of this object.")]
    public RectTransform handle;

    [Tooltip("How far the handle can travel from the centre, measured in canvas pixels.")]
    public float handleRange = 80f;

    [Tooltip("Directions shorter than this are treated as no input, which keeps the player still.")]
    public float deadZone = 0.12f;

    RectTransform background;

    Vector2 value;

    /// <summary>Current stick direction, x = world X, y = world Z. Length is 0..1.</summary>
    public Vector2 Value
    {
        get { return value; }
    }

    void Awake()
    {
        background = GetComponent<RectTransform>();
    }

    // --- Unity UI drag events ---

    public void OnPointerDown(PointerEventData eventData)
    {
        UpdateFromPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateFromPointer(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ResetStick();
    }

    /// <summary>Returns the stick to the centre. Called on release and on respawn/pause.</summary>
    public void ResetStick()
    {
        value = Vector2.zero;
        if (handle != null) handle.anchoredPosition = Vector2.zero;
    }

    void UpdateFromPointer(PointerEventData eventData)
    {
        if (background == null) return;

        // Where did the finger land, relative to the centre of the background circle?
        Vector2 localPoint;
        bool ok = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, eventData.position, eventData.pressEventCamera, out localPoint);
        if (!ok) return;

        // Turn pixels into a -1..1 offset, then clamp it to a circle.
        Vector2 offset = localPoint / Mathf.Max(1f, handleRange);

        if (offset.magnitude < deadZone) value = Vector2.zero;
        else value = Vector2.ClampMagnitude(offset, 1f);

        if (handle != null) handle.anchoredPosition = value * handleRange;
    }

    // If the object is switched off (paused, game over) the stick must not stay stuck on.
    void OnDisable()
    {
        ResetStick();
    }
}
