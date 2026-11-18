using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Week 16: the repeatable scenario the capture runs against.
///
/// The module asks for a scenario that lasts at least 60 seconds and can be repeated under the same
/// conditions. This one is deliberately boring and fixed: the biggest roster the game allows (4v4),
/// the hardest AI preset, a seeded random stream, and the human walking a fixed four-waypoint loop
/// around the two flags at walking speed. Everything that can be pinned down is pinned down; the
/// remaining variance is the AI's own decisions, which is why every reported figure comes from three
/// runs and not one.
///
/// Two things the first smoke run proved it has to do, because both would silently poison a capture:
///
/// * Resume a paused match. Week 15 added OnApplicationFocus/OnApplicationPause auto-pause, and an
///   automated run whose window is not focused sits at Time.timeScale = 0 for its whole length -
///   measuring a still image. Every resume is counted and logged rather than hidden.
/// * Report the match ending. A 4v4 HARD match can be won inside twenty seconds, and a game-over
///   screen is not a frame cost; the harness restarts the match and stamps a new segment.
///
/// Sprint is NOT exercised: the sprint button lives on the HUD and driving it would add touch-input
/// simulation on top of movement for a component that only multiplies speed. The documents say so
/// rather than implying full input coverage.
/// </summary>
public class PerfScenario : MonoBehaviour
{
    // ---------------------------------------------------------------- the fixed conditions

    /// <summary>Largest roster the game supports: 1 human + 3 Blue AI against 4 Red AI = 8 bodies.</summary>
    public const int TeamSize = 4;

    /// <summary>Hardest preset, so the AI thinks and moves as much as it ever will.</summary>
    public const MatchDifficulty Difficulty = MatchDifficulty.Hard;

    /// <summary>Fixed seed for UnityEngine.Random, which EnemyAI uses for aggression and jitter.</summary>
    public const int Seed = 20261118;

    const float ArriveRadius = 1.8f;

    // ---------------------------------------------------------------- state

    GameManager game;
    MatchManager match;
    CharacterStatus player;
    CharacterMotor motor;
    VirtualJoystick joystick;
    FieldInfo stickField;

    Vector3[] loop;
    int loopIndex;
    int waypointsReached;
    int autoResumes;
    float drivenSeconds;
    bool driving;
    bool joystickReachable;

    // ---------------------------------------------------------------- setup, before the reload

    public void ApplySettings()
    {
        MatchSettings.SetTeamSize(TeamSize);
        MatchSettings.SetDifficulty(Difficulty);
        MatchSettings.Save();
        Random.InitState(Seed);
    }

    public string SettingsSummary()
    {
        return MatchSettings.SizeName + " at " + MatchSettings.DifficultyName(MatchSettings.Difficulty) +
               " (1 human + " + MatchSettings.BlueAiCount + " Blue AI vs " + MatchSettings.RedAiCount +
               " Red AI = " + MatchSettings.TotalCharacters + " characters), AI random seed " + Seed + ".";
    }

    // ---------------------------------------------------------------- run

    public void Begin(MatchManager manager, GameManager session)
    {
        match = manager;
        game = session;
        player = match != null ? match.player : null;
        driving = false;

        if (player == null)
        {
            Debug.LogError("[Perf] the match has no player - the scenario cannot drive the human.");
            return;
        }

        motor = player.GetComponent<CharacterMotor>();
        ResolveJoystick();
        BuildLoop();

        driving = true;
        Debug.Log("[Perf] scenario running: " + Describe());
    }

    public void End()
    {
        driving = false;
        SetStick(Vector2.zero);
        Debug.Log("[Perf] scenario segment finished: " + waypointsReached + " waypoint(s) reached in " +
                  drivenSeconds.ToString("F1", CultureInfo.InvariantCulture) + " s of driving, " +
                  autoResumes + " auto-resume(s) of a paused match.");
    }

    /// <summary>True once the match has stopped being playable, so the capture can cut the segment.</summary>
    public bool MatchOver
    {
        get
        {
            if (game == null) return true;

            return game.State == GameManager.GameState.Victory ||
                   game.State == GameManager.GameState.GameOver ||
                   game.State == GameManager.GameState.Draw;
        }
    }

    public string EndReason
    {
        get
        {
            if (game == null) return "no GameManager";
            return "game state " + game.State + ", match outcome " +
                   (match != null ? match.outcome.ToString() : "no match") +
                   ", driven " + drivenSeconds.ToString("F1", CultureInfo.InvariantCulture) + " s";
        }
    }

    /// <summary>
    /// The joystick is the real input path - PlayerController reads nothing else - so the scenario
    /// writes into it instead of calling CharacterMotor itself. Its Value property has no setter, so
    /// the backing field is set by reflection. This is test instrumentation only: no gameplay script
    /// is changed to make the measurement possible.
    /// </summary>
    void ResolveJoystick()
    {
        joystick = FindAnyObjectByType<VirtualJoystick>(FindObjectsInactive.Include);
        if (joystick == null)
        {
            Debug.LogWarning("[Perf] no VirtualJoystick in the scene, so the human will stand still. " +
                             "The AI load is still real; the input sequence is not.");
            return;
        }

        stickField = typeof(VirtualJoystick).GetField("value", BindingFlags.Instance | BindingFlags.NonPublic);
        joystickReachable = stickField != null;

        if (!joystickReachable)
        {
            Debug.LogWarning("[Perf] VirtualJoystick.value could not be reached by reflection - the " +
                             "human will stand still for this capture.");
        }
    }

    /// <summary>
    /// Four waypoints, built from the two flags the scene already has rather than from hard-coded
    /// map coordinates, so the same scenario runs on Classic, Palengke and School Campus.
    /// </summary>
    void BuildLoop()
    {
        Vector3 blue = match.blueFlag != null ? match.blueFlag.transform.position : Vector3.zero;
        Vector3 red = match.redFlag != null ? match.redFlag.transform.position : Vector3.forward * 40f;

        Vector3 centre = (blue + red) * 0.5f;
        Vector3 toRed = red - blue;
        toRed.y = 0f;
        if (toRed.sqrMagnitude < 0.01f) toRed = Vector3.forward * 40f;

        Vector3 side = Vector3.Cross(toRed.normalized, Vector3.up) * 6f;

        loop = new Vector3[4];
        loop[0] = Flat(centre + side);
        loop[1] = Flat(red - toRed.normalized * 4f);
        loop[2] = Flat(centre - side);
        loop[3] = Flat(blue + toRed.normalized * 4f);
        loopIndex = 0;
    }

    static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v;
    }

    void Update()
    {
        if (!driving) return;

        // A match that paused itself (focus loss, HOME press) is not a match being measured.
        if (game != null && game.State == GameManager.GameState.Paused)
        {
            autoResumes++;
            game.Resume();
            if (autoResumes <= 3 || autoResumes % 20 == 0)
                Debug.Log("[Perf] the match was paused mid-capture (focus loss); resumed it so the " +
                          "segment keeps measuring a live match. Count so far: " + autoResumes + ".");
        }

        if (player == null) return;

        drivenSeconds += Time.deltaTime;

        // Captured characters are frozen by the game itself; pushing the stick would only fight it.
        if (player.isCaptured || loop == null)
        {
            SetStick(Vector2.zero);
            return;
        }

        Vector3 here = player.transform.position;
        Vector3 target = loop[loopIndex];

        Vector3 to = target - here;
        to.y = 0f;

        if (to.magnitude <= ArriveRadius)
        {
            loopIndex = (loopIndex + 1) % loop.Length;
            waypointsReached++;
            target = loop[loopIndex];
            to = target - here;
            to.y = 0f;
        }

        // CharacterMotor.Move takes x = world X and y = world Z, which is exactly what the joystick
        // reports, so the direction goes straight through.
        Vector2 direction = new Vector2(to.x, to.z);
        SetStick(Vector2.ClampMagnitude(direction.normalized, 1f));
    }

    void SetStick(Vector2 value)
    {
        if (joystick == null || stickField == null || motor == null) return;
        stickField.SetValue(joystick, value);
    }

    // ---------------------------------------------------------------- description for the CSV

    public string Describe()
    {
        string scene = SceneManager.GetActiveScene().name;

        string start = player != null
            ? player.transform.position.x.ToString("F1", CultureInfo.InvariantCulture) + "," +
              player.transform.position.z.ToString("F1", CultureInfo.InvariantCulture)
            : "unknown";

        return "scene " + scene + "; " + MatchSettings.SizeName + " at " +
               MatchSettings.DifficultyName(MatchSettings.Difficulty) +
               " (1 human + " + MatchSettings.BlueAiCount + " Blue AI vs " + MatchSettings.RedAiCount + " Red AI); " +
               "AI random seed " + Seed + "; blue base spawn at (" + start + "); " +
               "human input = fixed 4-waypoint loop between the two flags, walking speed, no sprint; " +
               (joystickReachable ? "joystick driven by reflection" : "joystick NOT driven") +
               "; AI fully active; paused matches auto-resumed; a finished match restarts as a new segment.";
    }
}
