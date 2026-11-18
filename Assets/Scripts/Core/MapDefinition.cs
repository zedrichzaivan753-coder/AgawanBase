using UnityEngine;

/// <summary>
/// One playable map, described once, as an ASSET.
///
/// WHY AN ASSET AND NOT A C# CONSTANT
/// ----------------------------------
/// Map-specific numbers (scene name, framing, the card's preview image) must never be written
/// into PlayerController, EnemyAI or MatchManager. They live here, and the map select screen
/// simply reads the list it was given. Adding a fourth map is then one new asset and one more
/// entry on that list - no gameplay code changes at all.
///
/// The coordinates of the bases, flags and prisons are deliberately NOT here: they are real scene
/// objects, built by the map's builder and wired into MatchManager by name. Duplicating them in an
/// asset would create two sources of truth for the same position.
/// </summary>
[CreateAssetMenu(fileName = "Map_New", menuName = "Agawan Base/Map Definition", order = 10)]
public class MapDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Stable id. Written to PlayerPrefs, so it must never change once a build ships.")]
    public string mapId = "classic";

    [Tooltip("Name shown on the selection card.")]
    public string displayName = "Classic Open Field";

    [Tooltip("One or two lines shown under the name on the selection card.")]
    [TextArea(2, 4)]
    public string description = "Experience the original open-field Agawan Base gameplay.";

    [Header("Loading")]
    [Tooltip("The scene this map lives in. Must be in the build settings.")]
    public string sceneName = "Game";

    [Tooltip("The card's preview image. Leave empty and the card falls back to a flat colour.")]
    public Sprite preview;

    [Tooltip("Card accent colour, used for the selected highlight and the preview fallback.")]
    public Color accentColor = new Color(0.42f, 0.72f, 0.95f, 1f);

    [Header("Camera framing - read by FollowCamera on load")]
    [Tooltip("Centre of the playing rectangle on the ground plane (x, z).")]
    public Vector2 arenaCenter = new Vector2(0f, -1f);

    [Tooltip("Width and depth of the playing rectangle, in metres.")]
    public Vector2 arenaSize = new Vector2(34.5f, 19f);

    [Range(0.2f, 1.2f)]
    [Tooltip("How much of the arena WIDTH is visible. Smaller = closer in = the characters look " +
             "bigger. 0.35 keeps a 64 m map reading at the same scale as the 35 m original.")]
    public float visibleWidthPercent = 0.65f;

    /// <summary>True when every field a map needs to load is filled in.</summary>
    public bool IsValid
    {
        get { return !string.IsNullOrEmpty(mapId) && !string.IsNullOrEmpty(sceneName); }
    }
}
