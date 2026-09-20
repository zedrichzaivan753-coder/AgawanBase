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
        MatchSetup, // choose the team size and the AI skill
        Playing,    // the match is live
        Paused,     // frozen, pause menu up
        Victory,    // the player's team won: flag home, enemies all locked up, or the clock
        GameOver,   // the player's team lost, or every one of them ended up in prison
        Draw        // the time limit expired with the two teams genuinely level
    }

    // Survives a scene reload so the Restart button can jump straight back into the match.
    static bool startPlayingOnLoad;

    [Header("Scene references")]
    [Tooltip("The rules component.")]
    public MatchManager match;

    [Tooltip("Disabled while the match is not being played, so the player cannot move on menus.")]
    public PlayerController playerController;

    [Tooltip("Hidden on every screen except Playing.")]
    public VirtualJoystick joystick;

    [Header("Screens")]
    public GameObject panelTitle;
    public GameObject panelMatchSetup;
    public GameObject panelHud;
    public GameObject panelPause;
    public GameObject panelEnd;

    [Tooltip("Big VICTORY / GAME OVER / DRAW label on the end screen.")]
    public Text resultText;

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

        // A fresh launch shows the title; a Restart jumps straight into the match.
        SetState(startPlayingOnLoad ? GameState.Playing : GameState.Title);
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

    /// <summary>START button on the TITLE screen: go and choose a match.</summary>
    public void ShowMatchSetup()
    {
        SetState(GameState.MatchSetup);
    }

    /// <summary>BACK button on the match setup screen: return to the title.</summary>
    public void ShowTitle()
    {
        startPlayingOnLoad = false;
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

    void HandlePlayerWon() { SetState(GameState.Victory); }
    void HandlePlayerLost() { SetState(GameState.GameOver); }
    void HandleMatchDrawn() { SetState(GameState.Draw); }

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
        if (panelMatchSetup != null) panelMatchSetup.SetActive(next == GameState.MatchSetup);
        if (panelHud != null) panelHud.SetActive(next == GameState.Playing || next == GameState.Paused);
        if (panelPause != null) panelPause.SetActive(next == GameState.Paused);
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
}
