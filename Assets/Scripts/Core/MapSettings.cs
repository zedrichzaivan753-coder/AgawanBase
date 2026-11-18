using UnityEngine;

/// <summary>
/// The MAP the player chose, and the one place the choice is remembered.
///
/// It deliberately mirrors MatchSettings: a static class rather than a ScriptableObject, because
/// every map is a SCENE and a scene load wipes everything in it. A static field survives the
/// load, PlayerPrefs survives the app being closed, and no asset needs to be found at runtime.
///
/// Only two strings are remembered - the map id and its scene name - so Restart and Title can
/// reload the right map without needing the map select screen (or its list of assets) to exist.
/// </summary>
public static class MapSettings
{
    /// <summary>The map used when nothing has been chosen yet. It is the original arena.</summary>
    public const string DefaultMapId = "classic";

    /// <summary>The scene the default map lives in.</summary>
    public const string DefaultSceneName = "Game";

    const string MapIdKey = "map.selected";
    const string SceneKey = "map.scene";
    const string NameKey = "map.name";

    static string mapId;
    static string sceneName;
    static string displayName;
    static bool loaded;

    /// <summary>The chosen map's stable id.</summary>
    public static string MapId
    {
        get { EnsureLoaded(); return mapId; }
    }

    /// <summary>The scene that has to be loaded to play the chosen map.</summary>
    public static string SceneName
    {
        get { EnsureLoaded(); return sceneName; }
    }

    /// <summary>The chosen map's on-screen name, for the setup summary line.</summary>
    public static string DisplayName
    {
        get { EnsureLoaded(); return displayName; }
    }

    /// <summary>Reads the remembered choice. Called automatically the first time anything asks.</summary>
    public static void Load()
    {
        mapId = PlayerPrefs.GetString(MapIdKey, DefaultMapId);
        sceneName = PlayerPrefs.GetString(SceneKey, DefaultSceneName);
        displayName = PlayerPrefs.GetString(NameKey, "Classic Open Field");

        // A saved id with no scene would load nothing at all. Fall back rather than break.
        if (string.IsNullOrEmpty(sceneName))
        {
            mapId = DefaultMapId;
            sceneName = DefaultSceneName;
            displayName = "Classic Open Field";
        }

        loaded = true;

        Debug.Log("[Map] loaded selection: " + displayName + " (id=" + mapId +
                  ", scene=" + sceneName + ")");
    }

    /// <summary>
    /// Chooses a map and remembers it. The definition supplies everything, so no map-specific
    /// string is ever written into the callers.
    /// </summary>
    public static void Select(MapDefinition map)
    {
        EnsureLoaded();

        if (map == null || !map.IsValid)
        {
            Debug.LogError("[Map] Select() was given an invalid map definition. Keeping '" + mapId + "'.");
            return;
        }

        mapId = map.mapId;
        sceneName = map.sceneName;
        displayName = map.displayName;

        PlayerPrefs.SetString(MapIdKey, mapId);
        PlayerPrefs.SetString(SceneKey, sceneName);
        PlayerPrefs.SetString(NameKey, displayName);
        PlayerPrefs.Save();

        Debug.Log("[Map] selected " + displayName + " (id=" + mapId + ", scene=" + sceneName + ")");
    }

    /// <summary>Resets the choice to the original arena.</summary>
    public static void ResetToDefault()
    {
        loaded = true;
        mapId = DefaultMapId;
        sceneName = DefaultSceneName;
        displayName = "Classic Open Field";

        PlayerPrefs.SetString(MapIdKey, mapId);
        PlayerPrefs.SetString(SceneKey, sceneName);
        PlayerPrefs.SetString(NameKey, displayName);
        PlayerPrefs.Save();

        Debug.Log("[Map] selection reset to the default: " + displayName);
    }

    static void EnsureLoaded()
    {
        if (!loaded) Load();
    }
}
