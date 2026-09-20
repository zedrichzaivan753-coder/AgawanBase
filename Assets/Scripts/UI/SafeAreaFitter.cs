using UnityEngine;

/// <summary>
/// Keeps UI out from under a notch, a punch-hole or a rounded corner.
///
/// Screen.safeArea is the rectangle the operating system promises is actually visible, expressed in
/// screen pixels. This turns it into canvas units and applies it in one of two ways, depending on
/// what the rect it sits on is for:
///
///   StretchAnchors - for a panel that covers the whole screen (Panel_Hud). Its anchors are pulled
///                   in to the safe rectangle, so every child anchored to its edges - the freshness
///                   bar top-left, the pause button top-right, the sprint button bottom-right -
///                   moves inward with it. On a phone with no cutout the safe area equals the whole
///                   screen, so this is a no-op.
///
///   CornerOffset  - for something pinned to one corner (VirtualJoystick). Changing its anchors
///                   would break its placement, so it is simply nudged in by the safe area's own
///                   inset. The offset is always measured from the position recorded in Awake, so
///                   it can never accumulate or drift.
///
/// COST: nothing per frame. It recalculates only when the resolution or the safe area changes,
/// which is once at startup and again if the phone is rotated. It touches no gameplay value.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    /// <summary>How the safe area is applied to this rect.</summary>
    public enum Mode
    {
        /// <summary>Pull the anchors in. Use on a panel that stretches the whole screen.</summary>
        StretchAnchors,

        /// <summary>Nudge the rect in from its corner. Use on an element pinned to a corner.</summary>
        CornerOffset
    }

    [Tooltip("StretchAnchors for a full-screen panel, CornerOffset for a corner-pinned element.")]
    public Mode mode = Mode.StretchAnchors;

    [Tooltip("Keep watching for a change of resolution or safe area (rotation, split screen). " +
             "Costs one Rect comparison per frame and nothing else.")]
    public bool watchForChanges = true;

    [Tooltip("Log the applied safe area once, the first time it is not the whole screen.")]
    public bool logSafeArea;

    RectTransform rect;
    Canvas canvas;

    Vector2 baseAnchoredPosition;
    Rect lastSafeArea;
    int lastWidth, lastHeight;
    bool loggedAnInset;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        // The corner the designer placed this at. The offset is added to THIS, never to the
        // running value, so repeated application cannot walk the element across the screen.
        baseAnchoredPosition = rect.anchoredPosition;

        Apply();
    }

    void Update()
    {
        if (!watchForChanges) return;

        if (Screen.width == lastWidth && Screen.height == lastHeight &&
            Screen.safeArea == lastSafeArea) return;

        Apply();
    }

    /// <summary>Applies the current safe area now. Public so a test or a scene script can force it.</summary>
    public void Apply()
    {
        lastSafeArea = Screen.safeArea;
        lastWidth = Screen.width;
        lastHeight = Screen.height;

        if (rect == null || Screen.width <= 0 || Screen.height <= 0) return;

        Rect safe = lastSafeArea;

        bool inset = safe.xMin > 0f || safe.yMin > 0f ||
                     safe.xMax < Screen.width || safe.yMax < Screen.height;

        if (mode == Mode.StretchAnchors)
        {
            rect.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            rect.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);

            // A stretched rect must not also carry a size offset, or the inset would be doubled.
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        else
        {
            // Screen.safeArea is in screen pixels; anchoredPosition is in canvas units, so the
            // inset has to be divided by the canvas scale factor (0.55 on a 1920x1080 reference).
            float scale = (canvas != null && canvas.scaleFactor > 0f) ? canvas.scaleFactor : 1f;

            rect.anchoredPosition = baseAnchoredPosition + new Vector2(safe.xMin / scale, safe.yMin / scale);
        }

        if (logSafeArea && inset && !loggedAnInset)
        {
            loggedAnInset = true;
            Debug.Log("[SafeAreaFitter] " + name + " inset to the safe area " +
                      safe.width.ToString("F0") + " x " + safe.height.ToString("F0") +
                      " at (" + safe.xMin.ToString("F0") + ", " + safe.yMin.ToString("F0") +
                      ") of a " + Screen.width + " x " + Screen.height + " screen, mode " + mode + ".");
        }
    }
}
