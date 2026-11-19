using UnityEngine;

/// <summary>
/// The release identity of the build, in exactly one place.
///
/// Two things need to agree with the player's screen: the version the APK was built with and the
/// words on the credits panel. The version is NOT repeated here - it is read from
/// <see cref="Application.version"/>, which Unity fills from PlayerSettings.bundleVersion when the
/// player is built. That means the title label can only ever show the real version of the binary it
/// is running inside, and a documentation error (or a forgotten rebuild) shows up on screen the
/// moment the game starts instead of hiding until the defence.
///
/// BuildStamp is a compile-time constant and is deliberately dumb: the APK's own md5, recorded in
/// Week_17/Evidence/build_and_commit.txt, is what identifies the exact binary. A commit id cannot be
/// baked in, because the APK has to exist before the commit that ships it does - the same problem
/// Week 15 solved by recording the APK md5 as the identifier of what was actually tested.
/// </summary>
public static class ReleaseInfo
{
    /// <summary>Day the release candidate was built. Hand-set once, before the build.</summary>
    public const string BuildStamp = "2026-11-19";

    public const string Title = "AGAWAN BASE";
    public const string Subtitle = "The Filipino playground game, rebuilt for a phone";
    public const string Author = "Zedrich Zaivan B. Abrera";
    public const string Course = "MOBILE DEV 002 - Advanced Mobile Application Development";
    public const string Module = "Module 5 - Individual Thematic Mobile Game Development Laboratory";
    public const string Theme = "Philippine Games and Sports";
    public const string Engine = "Unity 6000.6.0f1, Universal Render Pipeline 17.6.0";

    /// <summary>
    /// What the credits panel prints under the title. Kept as one string so the panel shows one
    /// block of text the audit script can measure as a single label.
    /// </summary>
    public const string Licences =
        "Credits and licences\n" +
        "- Game design, programming, art direction and documentation: Zedrich Zaivan B. Abrera.\n" +
        "- Built with Unity 6000.6.0f1 and the Universal Render Pipeline 17.6.0, used under the " +
        "Unity Personal licence. Unity and the Unity logo are trademarks of Unity Technologies.\n" +
        "- Interface text uses Unity's built-in uGUI legacy font, redistributed with the editor " +
        "under the Unity Companion License.\n" +
        "- All geometry, textures, sounds and interface art in this build were produced for this " +
        "project. No third-party art, music or sound effects are included.\n" +
        "- AI assistance was used for documentation drafting and for part of the art and audio " +
        "generation, and is disclosed in the Week 17 documentation and the project README.\n" +
        "- Agawang Base is a traditional Filipino playground game; this adaptation claims no rights " +
        "over the game itself.";

    /// <summary>The version string Unity stamped into this binary.</summary>
    public static string Version
    {
        get { return Application.version; }
    }

    /// <summary>Version plus build day: what the title label and the credits panel print.</summary>
    public static string BuildId
    {
        get { return Version + "  -  built " + BuildStamp; }
    }
}
