using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Owns the game states and every screen. It is the only thing that switches the
/// rules on and off (by setting MatchManager.matchActive) and the only thing that
/// pauses time. It never decides who wins a touch - that is MatchManager's job.
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Title,      // title screen, nobody can move
        Profile,    // the player's own record: name, wins/losses, fastest victories
        MapSelect,  // choose which map to play on
        MatchSetup, // choose the team size and the AI skill
        Playing,    // the match is live
        Paused,     // frozen, pause menu up
        Victory,    // the player's team won: flag home, enemies all locked up, or the clock
        GameOver,   // the player's team lost, or every one of them ended up in prison
        Draw        // the time limit expired with the two teams genuinely level
    }

    // Survives a scene reload so the Restart button can jump straight back into the match.
    static bool startPlayingOnLoad;

    // Survives the reload that happens when a DIFFERENT map is chosen: after that load the player
    // lands on the match setup screen, not back on the title.
    static bool startOnMatchSetup;

    // The id of the match this session has already banked. -1 means nothing yet. It is an
    // instance field, so a scene reload starts a brand new match with a fresh guard.
    int recordedMatchId = -1;

    /// <summary>
    /// Clears the two flags above every time a play session begins.
    ///
    /// This project runs with Enter Play Mode Options domain reload switched OFF, so a STATIC keeps
    /// its value from the previous play session. Without this reset, a session that ended in a
    /// match leaves startPlayingOnLoad at true and the next Play jumps straight past the title
    /// screen - which is exactly what was happening. In a built player the fields are already
    /// false, so this only ever helps.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetPlaySessionFlags()
    {
        startPlayingOnLoad = false;
        startOnMatchSetup = false;
    }

    [Header("Scene references")]
    [Tooltip("The rules component.")]
    public MatchManager match;

    [Tooltip("Disabled while the match is not being played, so the player cannot move on menus.")]
    public PlayerController playerController;

    [Tooltip("Hidden on every screen except Playing.")]
    public VirtualJoystick joystick;

    [Header("Screens")]
    public GameObject panelTitle;

    [Tooltip("Map select: one card per playable map.")]
    public GameObject panelMapSelect;

    public GameObject panelMatchSetup;

    [Tooltip("The player's profile: display name, record and fastest victories.")]
    public GameObject panelProfile;

    [Tooltip("Optional. Redrawn every time the profile panel opens.")]
    public ProfileUI profileUI;
    public GameObject panelHud;
    public GameObject panelPause;
    public GameObject panelEnd;

    [Tooltip("Big VICTORY / GAME OVER / DRAW label on the end screen.")]
    public Text resultText;

    [Header("Tag hints")]
    [Tooltip("Caption on the pause screen's TAG HINTS button, so it can read ON or OFF.")]
    public Text tagHintsLabel;

    [Tooltip("Asked to redraw the rings the instant the toggle is pressed. Optional.")]
    public IndicatorController indicatorController;

    [Header("Flags")]
    [Tooltip("Both flags. They are sent home whenever the match is not live, so no screen can ever " +
             "leave a flag stuck to a character or abandoned in the middle of the field.")]
    public Flag redFlag;
    public Flag blueFlag;

    [Header("Settings")]
    [Tooltip("Hard limit for the mobile build.")]
    public int targetFrameRate = 30;

    [Tooltip("Name of the gameplay scene, used by Restart and Title.")]
    public string sceneName = "Game";

    /// <summary>Current screen/state.</summary>
    public GameState State { get; private set; }

    /// <summary>Raised every time the state changes.</summary>
    public event Action<GameState> StateChanged;

    /// <summary>
    /// How the match ended. A read-only mirror of the rules, so anything that has to EXPLAIN the
    /// result can, instead of having to guess from the score.
    /// </summary>
    public MatchManager.MatchOutcome Outcome
    {
        get { return match != null ? match.outcome : MatchManager.MatchOutcome.None; }
    }

    void Awake()
    {
        // Performance settings belong to the app, so they are applied once at startup.
        Application.targetFrameRate = targetFrameRate;
        QualitySettings.vSyncCount = 0;
        QualitySettings.shadows = ShadowQuality.Disable;
        Time.timeScale = 1f;

        // Read the remembered match choice once, before any screen can ask for it.
        MatchSettings.Load();

        // Same for the chosen map: Restart and Title reload by scene name, so it must be known
        // before the first screen appears.
        MapSettings.Load();

        // The profile is a file, not a preference, but it is read at the same moment for the same
        // reason: the title screen can open it, and it must already be true when it does.
        PlayerProfile.Load();
    }

    void Start()
    {
        if (match != null)
        {
            match.PlayerWon += HandlePlayerWon;
            match.PlayerLost += HandlePlayerLost;
            match.MatchDrawn += HandleMatchDrawn;
        }
        else Debug.LogError("GameManager: 'match' is not assigned.");

        // A fresh launch shows the title; a Restart jumps straight into the match; and arriving
        // from the map select screen with a newly loaded map stops on match setup.
        bool onMatchSetup = startOnMatchSetup;
        bool playImmediately = startPlayingOnLoad;

        // Both flags are one-shot. Clearing them here means nothing can leak into the next screen.
        startOnMatchSetup = false;
        startPlayingOnLoad = false;

        if (playImmediately)
        {
            Debug.Log("[Title] restart requested - going straight into the match.");
            SetState(GameState.Playing);
        }
        else if (onMatchSetup)
        {
            Debug.Log("[Title] map chosen - showing the match setup screen.");
            SetState(GameState.MatchSetup);
        }
        else
        {
            Debug.Log("[Title] starting on the title screen.");
            SetState(GameState.Title);
        }
    }

    void OnDestroy()
    {
        if (match != null)
        {
            match.PlayerWon -= HandlePlayerWon;
            match.PlayerLost -= HandlePlayerLost;
            match.MatchDrawn -= HandleMatchDrawn;
        }
    }

    // ------------------------------------------------- button handlers

    /// <summary>START button on the TITLE screen: go and choose a map.</summary>
    public void ShowMapSelect()
    {
        SetState(GameState.MapSelect);
    }

    /// <summary>PROFILE button on the title screen: the player's own record.</summary>
    public void ShowProfile()
    {
        SetState(GameState.Profile);
    }

    /// <summary>BACK button on the profile panel: return to the title.</summary>
    public void CloseProfile()
    {
        SetState(GameState.Title);
    }

    /// <summary>START button on the match setup screen: go and choose a map.</summary>
    public void ShowMatchSetup()
    {
        SetState(GameState.MatchSetup);
    }

    /// <summary>
    /// Loads the map the player chose on the map select screen.
    ///
    /// Every map is its own SCENE, so choosing a different one means a scene load - and after that
    /// load the player must land on match setup, not back on the title. Choosing the map that is
    /// ALREADY loaded skips the load entirely and just moves the screen on, which is why pressing
    /// START MATCH on the map you are standing in is instant.
    /// </summary>
    public void LoadMap(MapDefinition map)
    {
        if (map == null || !map.IsValid)
        {
            Debug.LogError("GameManager: LoadMap() was given an invalid map definition.");
            return;
        }

        if (map.sceneName == SceneManager.GetActiveScene().name)
        {
            SetState(GameState.MatchSetup);
            return;
        }

        startPlayingOnLoad = false;
        startOnMatchSetup = true;
        Time.timeScale = 1f;

        Debug.Log("[Map] loading scene '" + map.sceneName + "' for " + map.displayName);
        SceneManager.LoadScene(map.sceneName);
    }

    /// <summary>BACK button on the match setup screen: return to the title.</summary>
    public void ShowTitle()
    {
        startPlayingOnLoad = false;
        startOnMatchSetup = false;
        SetState(GameState.Title);
    }

    /// <summary>START button on the match setup screen. Also what Restart ends up doing.</summary>
    public void StartGame()
    {
        startPlayingOnLoad = true;
        SetState(GameState.Playing);
    }

    /// <summary>Pause button on the HUD.</summary>
    public void Pause()
    {
        if (State == GameState.Playing) SetState(GameState.Paused);
    }

    /// <summary>RESUME button on the pause screen.</summary>
    public void Resume()
    {
        if (State == GameState.Paused) SetState(GameState.Playing);
    }

    /// <summary>
    /// TAG HINTS button on the pause screen.
    ///
    /// This is a DISPLAY preference and nothing else. Switching it off hides the tag-status
    /// rings on the opponents and changes no rule - MatchManager still decides every capture
    /// the same way. The marker on the character the player controls stays on regardless, so
    /// the player is never left without a "this one is me" cue.
    /// </summary>
    public void ToggleTagHints()
    {
        MatchSettings.SetTagHints(!MatchSettings.TagHints);

        // Redraw NOW rather than waiting for the controller's next tick. While this screen is
        // up Time.timeScale is 0, so Time.deltaTime is 0 and the controller's refresh timer
        // never reaches its interval - the rings would not change until well after Resume.
        if (indicatorController != null) indicatorController.ForceRefresh();

        RefreshTagHintsLabel();
    }

    /// <summary>Keeps the button's caption in step with the remembered choice.</summary>
    void RefreshTagHintsLabel()
    {
        if (tagHintsLabel != null)
            tagHintsLabel.text = MatchSettings.TagHints ? "TAG HINTS: ON" : "TAG HINTS: OFF";
    }

    /// <summary>
    /// START button on the match setup screen, when the choice differs from what is on the field.
    /// The roster is spawned as the scene loads, so a reload is the only way to build a different
    /// team size - and it is also the cheapest, because it resets everything else too.
    /// startPlayingOnLoad then brings the brand new match straight up as Playing.
    /// </summary>
    public void StartMatchAndReload()
    {
        startPlayingOnLoad = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>RESTART button. Reloading the scene is the simplest full reset.</summary>
    public void Restart()
    {
        StartMatchAndReload();
    }

    /// <summary>TITLE button. Also a full reset, but stops on the title screen.</summary>
    public void GoToTitle()
    {
        startPlayingOnLoad = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    // ------------------------------------------------- rules callbacks

    // The human's team is always Blue, so the two legacy events are still exactly what the end
    // screen needs. A TIME-LIMIT or ELIMINATION ending arrives through the same two events.

    void HandlePlayerWon()
    {
        RecordResult(MatchResult.Win);
        SetState(GameState.Victory);
    }

    void HandlePlayerLost()
    {
        RecordResult(MatchResult.Loss);
        SetState(GameState.GameOver);
    }

    void HandleMatchDrawn()
    {
        RecordResult(MatchResult.Draw);
        SetState(GameState.Draw);
    }

    /// <summary>
    /// Writes one finished match into the player's profile. Called from the three end-of-match
    /// callbacks above, and nowhere else, so a match is banked exactly once.
    ///
    /// The guard is belt and braces: PlayerProfile refuses an id it has already seen, and the
    /// field below stops this class from even asking twice if a callback were ever to fire again.
    /// </summary>
    void RecordResult(MatchResult result)
    {
        int matchId = PlayerProfile.NextMatchId;
        if (matchId == recordedMatchId) return;
        recordedMatchId = matchId;

        string outcome = match != null ? match.outcome.ToString().ToUpperInvariant() : "UNKNOWN";
        float seconds = match != null ? match.MatchClock : 0f;

        PlayerProfile.RecordMatch(matchId, result, outcome, seconds, MapSettings.MapId,
                                  MatchSettings.TeamSize,
                                  MatchSettings.DifficultyName(MatchSettings.Difficulty));
    }

    // ------------------------------------------------- the end-screen words

    /// <summary>
    /// What to write on the end screen when the player WON. All three are victories, but they are
    /// not the same story, and an end screen that tells the wrong one reads as a bug.
    /// </summary>
    string VictoryText()
    {
        if (Outcome == MatchManager.MatchOutcome.Elimination)
        {
            // At 1v1 there are no team-mates, so "every enemy was locked in prison" is a strange
            // way to describe catching the one opponent. The step's bar is that a 1v1 ending still
            // READS like the game it replaces.
            if (TeamManager.Count(Team.Red) <= 1)
            {
                return "VICTORY!\nYou captured the enemy.";
            }

            return "VICTORY!\nEvery enemy was locked in prison.";
        }

        if (Outcome == MatchManager.MatchOutcome.TimeLimit)
        {
            return "VICTORY!\nTime ran out - your team was ahead.";
        }

        return "VICTORY!\nYou carried the red flag home.";
    }

    /// <summary>What to write on the end screen when the player LOST.</summary>
    string DefeatText()
    {
        if (Outcome == MatchManager.MatchOutcome.Elimination)
        {
            // 1v1: the human IS the whole team, so being caught is simply being captured.
            if (TeamManager.Count(Team.Blue) <= 1)
            {
                return "GAME OVER\nYou were captured.";
            }

            return "GAME OVER\nEvery one of your team was locked in prison.";
        }

        if (Outcome == MatchManager.MatchOutcome.TimeLimit)
        {
            return "GAME OVER\nTime ran out - the enemy team was ahead.";
        }

        return "GAME OVER\nThe enemy carried your flag home.";
    }

    // ------------------------------------------------- the state machine

    void SetState(GameState next)
    {
        State = next;

        // The rules only run while the match is actually live.
        if (match != null) match.matchActive = (next == GameState.Playing);

        // Pausing freezes everything that uses Time.deltaTime.
        Time.timeScale = (next == GameState.Paused) ? 0f : 1f;

        // The player must not be able to walk around on menus.
        if (playerController != null) playerController.enabled = (next == GameState.Playing);

        // Flags: any screen that is NOT the live match leaves them in a clean, predictable state.
        // Restart and Title reload the scene anyway; Victory, Game Over and Draw do not, so reset
        // here. Paused is deliberately excluded - pausing mid-carry must freeze the flag on the
        // carrier, not teleport it home, or resuming would silently undo the player's steal.
        if (next != GameState.Playing && next != GameState.Paused)
        {
            if (redFlag != null) redFlag.ReturnHome();
            if (blueFlag != null) blueFlag.ReturnHome();
        }

        // Screens
        if (panelTitle != null) panelTitle.SetActive(next == GameState.Title);
        if (panelMapSelect != null) panelMapSelect.SetActive(next == GameState.MapSelect);
        if (panelMatchSetup != null) panelMatchSetup.SetActive(next == GameState.MatchSetup);

        // The profile panel is redrawn from the stored profile every time it opens, so it can
        // never show a record that the save file no longer holds.
        if (panelProfile != null) panelProfile.SetActive(next == GameState.Profile);
        if (next == GameState.Profile && profileUI != null) profileUI.Open();
        if (panelHud != null) panelHud.SetActive(next == GameState.Playing || next == GameState.Paused);
        if (panelPause != null) panelPause.SetActive(next == GameState.Paused);

        // The caption must already be correct when the screen appears, not one click later.
        if (next == GameState.Paused) RefreshTagHintsLabel();
        if (panelEnd != null)
        {
            panelEnd.SetActive(next == GameState.Victory || next == GameState.GameOver ||
                               next == GameState.Draw);
        }

        // The joystick is only usable while playing, and must never stay stuck on.
        if (joystick != null)
        {
            if (next != GameState.Playing) joystick.ResetStick();
            joystick.gameObject.SetActive(next == GameState.Playing);
        }

        if (resultText != null)
        {
            if (next == GameState.Victory) resultText.text = VictoryText();
            else if (next == GameState.GameOver) resultText.text = DefeatText();
            else if (next == GameState.Draw) resultText.text = "DRAW\nTime ran out with the two teams level.";
        }

        if (StateChanged != null) StateChanged(next);
    }

    // ------------------------------------------------- backgrounding

    /// <summary>
    /// Sending the app to the background must not leave the match running. Android keeps ticking
    /// the app for a while after HOME, and if the OS then reclaims it the player returns to a match
    /// they lost while they could not see it. Nothing else in the project is told when the app
    /// leaves, so this is the only place that can prevent it.
    ///
    /// Coming back does NOT auto-resume: the pause menu is already on screen with its RESUME
    /// button, so an interrupted player resumes on purpose instead of being dropped into a live
    /// chase with a finger nowhere near the stick.
    /// </summary>
    void OnApplicationPause(bool paused)
    {
        if (!paused) return;
        if (State != GameState.Playing) return;

        Debug.Log("[Pause] the app went to the background - pausing the match.");
        Pause();
    }

    /// <summary>
    /// Some Android builds deliver focus loss without a pause event, so the same guard runs here.
    /// Idempotent by construction: the State check means a second call while paused does nothing.
    /// </summary>
    void OnApplicationFocus(bool focused)
    {
        if (focused) return;
        if (State != GameState.Playing) return;

        Pause();
    }
}
