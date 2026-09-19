using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Owns the game states and the four screens. It is the only thing that switches the
/// rules on and off (by setting MatchManager.matchActive) and the only thing that
/// pauses time. It never decides who wins a touch - that is MatchManager's job.
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Title,      // title screen, nobody can move
        Playing,    // the match is live
        Paused,     // frozen, pause menu up
        Victory,    // player took the red flag
        GameOver    // player was captured, or an enemy took the blue flag
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
    public GameObject panelHud;
    public GameObject panelPause;
    public GameObject panelEnd;

    [Tooltip("Big VICTORY / GAME OVER label on the end screen.")]
    public Text resultText;

    [Header("Settings")]
    [Tooltip("Hard limit for the mobile build.")]
    public int targetFrameRate = 30;

    [Tooltip("Name of the gameplay scene, used by Restart and Title.")]
    public string sceneName = "Game";

    /// <summary>Current screen/state.</summary>
    public GameState State { get; private set; }

    /// <summary>Raised every time the state changes.</summary>
    public event Action<GameState> StateChanged;

    // ---- helpers the HUD reads (read-only mirrors of the rules) ----

    public int RedsCaptured { get { return match != null ? match.redsCaptured : 0; } }
    public int TotalReds { get { return match != null ? match.TotalEnemies : 0; } }

    void Awake()
    {
        // Performance settings belong to the app, so they are applied once at startup.
        Application.targetFrameRate = targetFrameRate;
        QualitySettings.vSyncCount = 0;
        QualitySettings.shadows = ShadowQuality.Disable;
        Time.timeScale = 1f;
    }

    void Start()
    {
        if (match != null)
        {
            match.PlayerWon += HandlePlayerWon;
            match.PlayerLost += HandlePlayerLost;
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
        }
    }

    // ------------------------------------------------- button handlers

    /// <summary>START button on the title screen.</summary>
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

    /// <summary>RESTART button. Reloading the scene is the simplest full reset.</summary>
    public void Restart()
    {
        startPlayingOnLoad = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>TITLE button. Also a full reset, but stops on the title screen.</summary>
    public void GoToTitle()
    {
        startPlayingOnLoad = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    // ------------------------------------------------- rules callbacks

    void HandlePlayerWon() { SetState(GameState.Victory); }
    void HandlePlayerLost() { SetState(GameState.GameOver); }

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

        // Screens
        if (panelTitle != null) panelTitle.SetActive(next == GameState.Title);
        if (panelHud != null) panelHud.SetActive(next == GameState.Playing || next == GameState.Paused);
        if (panelPause != null) panelPause.SetActive(next == GameState.Paused);
        if (panelEnd != null) panelEnd.SetActive(next == GameState.Victory || next == GameState.GameOver);

        // The joystick is only usable while playing, and must never stay stuck on.
        if (joystick != null)
        {
            if (next != GameState.Playing) joystick.ResetStick();
            joystick.gameObject.SetActive(next == GameState.Playing);
        }

        if (resultText != null)
        {
            if (next == GameState.Victory) resultText.text = "VICTORY!\nYou reached the red flag.";
            else if (next == GameState.GameOver) resultText.text = "GAME OVER\nYou were captured.";
        }

        if (StateChanged != null) StateChanged(next);
    }
}
