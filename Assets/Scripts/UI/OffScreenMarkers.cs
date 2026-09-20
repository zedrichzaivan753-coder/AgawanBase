using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Three small arrows that ride the edge of the screen and point at the things you cannot see.
///
/// Zooming the camera in makes the arena read clearly, but it also means more of the pitch is off
/// screen. Rather than widening the view again, this keeps the player oriented with three cheap
/// edge markers:
///
///   ENEMY FLAG   - the flag you are allowed to steal, whenever you are not the one carrying it.
///   YOUR FLAG    - only while an ENEMY is carrying it, which is the moment it matters most.
///   TEAM-MATE    - the nearest Blue team-mate locked in the enemy prison.
///
/// Each arrow sits on the edge of the screen nearest its target, points at it, and hides itself the
/// moment the target is genuinely visible - so the screen stays clean. Nothing here reads or changes
/// a rule: it is a read-only view of MatchManager's flags and TeamManager's roster, exactly like the
/// HUD and the tag indicators.
///
/// COST: three WorldToViewportPoint calls and a few float comparisons, TEN times a second. It builds
/// its own three Images in Awake from one generated triangle sprite, so there is no prefab to wire,
/// no texture to import, no allocation per frame, no raycast and no scene search in Update. The
/// arrows are not raycast targets, so they can never steal a touch from the joystick.
/// </summary>
public class OffScreenMarkers : MonoBehaviour
{
    [Header("Scene references")]
    [Tooltip("The rules. Read for the two flags and the human player - never written to.")]
    public MatchManager match;

    [Header("What to show")]
    [Tooltip("Arrow pointing at the enemy flag while you are not carrying it.")]
    public bool showEnemyFlag = true;

    [Tooltip("Arrow pointing at your own flag while an enemy is carrying it.")]
    public bool showStolenFlag = true;

    [Tooltip("Arrow pointing at the nearest captured team-mate.")]
    public bool showCapturedMate = true;

    [Header("Look")]
    [Tooltip("Size of each arrow, in canvas pixels at the 1920 x 1080 reference resolution.")]
    public float arrowSize = 54f;

    [Tooltip("How far the arrows sit in from the true edge of the screen, as a fraction of the " +
             "half width / half height. 0.06 keeps them clear of the rounded corners.")]
    [Range(0f, 0.25f)] public float edgeInset = 0.06f;

    public Color enemyFlagColour = new Color(0.85f, 0.15f, 0.15f, 0.9f);
    public Color stolenFlagColour = new Color(0.20f, 0.45f, 0.95f, 0.9f);
    public Color capturedMateColour = new Color(0.75f, 0.78f, 0.85f, 0.9f);

    [Header("Timing")]
    [Tooltip("How many times a second the arrows are repositioned. 10 is plenty: the target has to " +
             "move a long way for the angle to change visibly, and this keeps the cost to nothing.")]
    public float refreshInterval = 0.1f;

    [Header("Debug")]
    [Tooltip("Log only when an arrow appears or disappears - never per refresh.")]
    public bool logMarkers;

    /// <summary>One arrow. A plain class, allocated three times in Awake and never again.</summary>
    class Marker
    {
        public RectTransform rect;
        public Image image;
        public bool visible;
    }

    Marker enemyFlagMarker;
    Marker stolenFlagMarker;
    Marker mateMarker;

    Sprite arrowSprite;

    Camera cam;
    RectTransform panel;
    float timer;

    void Awake()
    {
        panel = GetComponent<RectTransform>();
        arrowSprite = CreateArrowSprite(32);

        enemyFlagMarker = BuildMarker("EnemyFlag", enemyFlagColour);
        stolenFlagMarker = BuildMarker("StolenFlag", stolenFlagColour);
        mateMarker = BuildMarker("CapturedMate", capturedMateColour);

        // A first pass now, so nothing is left over from a previous match on the first frame.
        timer = 0f;
    }

    void Update()
    {
        // Nothing is drawn outside a live match. The panel is switched off then anyway; this is the
        // belt-and-braces guard that also covers the moment the match ends.
        if (!TeamManager.MatchActive)
        {
            SetVisible(enemyFlagMarker, false);
            SetVisible(stolenFlagMarker, false);
            SetVisible(mateMarker, false);
            return;
        }

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = Mathf.Max(0.02f, refreshInterval);

        Refresh();
    }

    // ------------------------------------------------------------------- the three arrows

    void Refresh()
    {
        // One-off lookups: after the first successful pass these are plain field reads.
        if (match == null) match = Object.FindAnyObjectByType<MatchManager>();
        if (cam == null) cam = Camera.main;

        if (match == null || cam == null || panel == null)
        {
            SetVisible(enemyFlagMarker, false);
            SetVisible(stolenFlagMarker, false);
            SetVisible(mateMarker, false);
            return;
        }

        Rect area = panel.rect;
        if (area.width <= 1f || area.height <= 1f) return;

        CharacterStatus human = match.player;
        Team myTeam = human != null ? human.team : Team.Blue;

        // ---- 1. the enemy flag: the one you are allowed to steal ------------------------------
        Flag enemyFlag = match.FlagOf(myTeam.Opponent());

        bool carryingItMyself = enemyFlag != null && enemyFlag.IsCarried &&
                                human != null && enemyFlag.carrier == human.transform;

        bool wantEnemyFlag = showEnemyFlag && enemyFlag != null && !carryingItMyself;
        SetVisible(enemyFlagMarker,
                   wantEnemyFlag && Place(enemyFlagMarker, enemyFlag.transform.position, area));

        // ---- 2. our own flag, but only while an ENEMY has it ----------------------------------
        Flag ownFlag = match.FlagOf(myTeam);

        bool enemyHasOurs = ownFlag != null && ownFlag.IsCarried && ownFlag.carrier != null &&
                            (human == null || ownFlag.carrier != human.transform);

        SetVisible(stolenFlagMarker,
                   showStolenFlag && enemyHasOurs && Place(stolenFlagMarker, ownFlag.transform.position, area));

        // ---- 3. the nearest captured team-mate ------------------------------------------------
        CharacterStatus mate = NearestCapturedMate(myTeam, human);

        SetVisible(mateMarker,
                   showCapturedMate && mate != null && Place(mateMarker, mate.transform.position, area));
    }

    /// <summary>
    /// The closest imprisoned team-mate, read straight off the registry - no scene search. The human
    /// is skipped: being captured yourself is already announced by the centre alert banner.
    /// </summary>
    CharacterStatus NearestCapturedMate(Team team, CharacterStatus human)
    {
        System.Collections.Generic.List<CharacterStatus> members = TeamManager.Members(team);

        Vector3 from = human != null ? human.transform.position : transform.position;

        CharacterStatus nearest = null;
        float nearestSqr = float.MaxValue;

        for (int i = 0; i < members.Count; i++)
        {
            CharacterStatus member = members[i];
            if (member == null || member == human || !member.isCaptured) continue;

            Vector3 p = member.transform.position;
            float dx = p.x - from.x;
            float dz = p.z - from.z;
            float sqr = dx * dx + dz * dz;

            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = member;
            }
        }

        return nearest;
    }

    // ------------------------------------------------------------------- placing one arrow

    /// <summary>
    /// Puts one arrow on the screen edge nearest its target and turns it to point that way.
    /// Returns FALSE when the target is close enough to the middle of the screen to be seen, which
    /// is what hides the arrow again.
    /// </summary>
    bool Place(Marker marker, Vector3 worldPoint, Rect area)
    {
        Vector3 viewport = cam.WorldToViewportPoint(worldPoint);

        // Anything behind the camera projects oddly; mirroring it keeps the arrow on the correct
        // side. (With this top-down camera nothing should ever land here, but it is one branch.)
        if (viewport.z < 0f)
        {
            viewport.x = 1f - viewport.x;
            viewport.y = 1f - viewport.y;
        }

        // Viewport (0..1) -> offset from the centre of the panel, in canvas units.
        float x = (viewport.x - 0.5f) * area.width;
        float y = (viewport.y - 0.5f) * area.height;

        float halfWidth = area.width * 0.5f;
        float halfHeight = area.height * 0.5f;

        float limitX = halfWidth * (1f - Mathf.Clamp01(edgeInset));
        float limitY = halfHeight * (1f - Mathf.Clamp01(edgeInset));

        // Already on screen: no arrow is wanted.
        if (Mathf.Abs(x) <= limitX && Mathf.Abs(y) <= limitY) return false;

        // Scale the direction until it just touches the inset rectangle, which puts the arrow on
        // the edge that lies in the direction of the target rather than in a corner every time.
        float scale = Mathf.Min(limitX / Mathf.Max(0.0001f, Mathf.Abs(x)),
                                limitY / Mathf.Max(0.0001f, Mathf.Abs(y)));

        Vector2 position = new Vector2(x, y) * scale;

        marker.rect.anchoredPosition = position;

        // The sprite points along +x, so its Z angle IS the direction to the target. Atan2 takes
        // screen Y as the Y component, which is what a Z rotation wants in this overlay canvas.
        marker.rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(position.y, position.x) * Mathf.Rad2Deg);

        return true;
    }

    void SetVisible(Marker marker, bool value)
    {
        if (marker == null || marker.visible == value) return;

        marker.visible = value;
        marker.rect.gameObject.SetActive(value);

        if (logMarkers)
        {
            Debug.Log("[OffScreenMarkers] " + marker.rect.name + (value ? " shown" : " hidden"));
        }
    }

    // ------------------------------------------------------------------- construction

    /// <summary>Creates one arrow Image. Never a raycast target, never masked, never a collider.</summary>
    Marker BuildMarker(string markerName, Color colour)
    {
        GameObject go = new GameObject("Marker_" + markerName, typeof(RectTransform));
        go.transform.SetParent(transform, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(arrowSize, arrowSize);
        rect.anchoredPosition = Vector2.zero;

        Image image = go.AddComponent<Image>();
        image.sprite = arrowSprite;
        image.color = colour;
        image.type = Image.Type.Simple;
        image.maskable = false;

        // Critically important: an arrow must never eat a touch meant for the joystick or a button.
        image.raycastTarget = false;

        go.SetActive(false);

        Marker marker = new Marker();
        marker.rect = rect;
        marker.image = image;
        marker.visible = false;
        return marker;
    }

    /// <summary>
    /// Builds the one white triangle every arrow shares, pointing along +x, with a transparent
    /// background so each arrow can be tinted with its own Image colour. Generated in code, so the
    /// project imports no new asset - one texture, one sprite, created once.
    /// </summary>
    static Sprite CreateArrowSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "OffScreenArrow";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color32 fill = new Color32(255, 255, 255, 255);
        Color32 clear = new Color32(255, 255, 255, 0);
        Color32[] pixels = new Color32[size * size];

        float centre = size * 0.5f;
        float half = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // t runs 0 at the wide left edge to 1 at the tip on the right; the half-height
                // tapers away with it. The 0.95 keeps the point a couple of pixels blunt so it
                // stays visible when the arrow is small on a phone screen.
                float t = (x + 0.5f) / size;
                float halfHeight = (1f - t) * half * 0.95f;
                float dy = Mathf.Abs((y + 0.5f) - centre);

                pixels[y * size + x] = dy <= halfHeight ? fill : clear;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);

        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
