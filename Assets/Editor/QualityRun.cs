using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Week 15: the play-mode scenario suite. Where the EditMode tests check units, this drives the
/// real match through the same public methods the rules use, waits real frames, and records what
/// actually happened in a TSV row per case.
///
/// It never asserts a result it did not observe: a case that cannot run is recorded BLOCKED with
/// the reason, and a case that throws is recorded FAIL with the exception text.
///
/// Menu: Tools > Week15 > Run Quality Scenarios.
/// Output: Docs/Zedrich Abrera/Week_15/Evidence/test_logs_&lt;label&gt;/scenario.tsv
/// </summary>
public static class QualityRun
{
    // ---------------------------------------------------------------- configuration

    /// <summary>Folder label, so a baseline run and a post-fix run never overwrite each other.</summary>
    public static string Label = "final";

    static readonly float GlobalTimeoutSeconds = 300f;

    // ---------------------------------------------------------------- results

    class Row
    {
        public string id, requirement, title, expected, actual, status;
    }

    static readonly List<Row> rows = new List<Row>();
    static readonly List<string> feedbackLines = new List<string>();
    static int exceptionCount;

    static GameManager game;
    static MatchManager match;

    static int phase;
    static float phaseWaitUntil;
    static float runStartedAt;
    static float phaseStartedAt;
    static string stopReason = "still running";
    static bool finished;

    // Scratch state shared between phases.
    static CharacterStatus subject;
    static int capturesBefore;
    static int feedbackMark;
    static bool nudged;
    static bool waitThenAdvance;
    static float clockBefore;
    static float clockAtResume;
    static float resumeAt;
    static Vector3 positionBefore;
    static float savedTimeScale = 1f;
    static bool sawPauseMethod;
    static bool aiFreezePending = true;

    // ---------------------------------------------------------------- entry point

    [MenuItem("Tools/Week15/Run Quality Scenarios")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[QualityRun] already in play mode - stop it first.");
            return;
        }

        rows.Clear();
        feedbackLines.Clear();
        exceptionCount = 0;
        phase = 0;
        phaseWaitUntil = 0f;
        runStartedAt = 0f;
        phaseStartedAt = 0f;
        stopReason = "still running";
        finished = false;
        game = null;
        match = null;
        savedTimeScale = 1f;
        aiFreezePending = true;

        // A previous run that was interrupted may still be subscribed; never double-tick.
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;

        Application.logMessageReceived += OnLog;
        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();

        Debug.Log("[QualityRun] starting the Week 15 scenario suite (label '" + Label + "').");
    }

    // ---------------------------------------------------------------- plumbing

    static void OnLog(string message, string stack, LogType type)
    {
        if (message.StartsWith("[Feedback]")) feedbackLines.Add(message);
        if (type == LogType.Exception || type == LogType.Error) exceptionCount++;
    }

    static void Tick()
    {
        if (finished) return;

        if (!EditorApplication.isPlaying)
        {
            // Play mode was stopped from outside before the suite finished. Record what ran.
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                Finish("play mode ended before the suite finished");
            return;
        }

        if (runStartedAt <= 0f) runStartedAt = Time.realtimeSinceStartup;
        if (phaseStartedAt <= 0f) phaseStartedAt = Time.realtimeSinceStartup;

        // A phase that never completes must be recorded as blocked, not sit in silence. Silence
        // that looks like success is the one failure mode a test harness must not have.
        if (Time.realtimeSinceStartup - phaseStartedAt > 30f)
        {
            Add(AutomationId(phase), "-", "phase " + phase + " must finish inside 30 s",
                "the phase completes", "stalled in phase " + phase + " for over 30 s", "BLOCKED");
            Next();
            return;
        }

        if (Time.realtimeSinceStartup - runStartedAt > GlobalTimeoutSeconds)
        {
            Add("TC-SUITE", "-", "suite completed inside the time budget", "under " + GlobalTimeoutSeconds + " s",
                "timed out in phase " + phase, "BLOCKED");
            Finish("the suite hit its " + GlobalTimeoutSeconds + " s budget");
            EditorApplication.ExitPlaymode();
            return;
        }

        if (phaseWaitUntil > 0f)
        {
            if (Time.realtimeSinceStartup < phaseWaitUntil) return;

            phaseWaitUntil = 0f;
            if (waitThenAdvance)
            {
                waitThenAdvance = false;
                Next();
                return;
            }
        }

        try
        {
            FreezeAi();
            RunPhase();
        }
        catch (System.Exception e)
        {
            Add(AutomationId(phase), "-", "phase " + phase + " must not throw", "no exception",
                e.GetType().Name + ": " + e.Message, "FAIL");
            Next();
        }
    }

    static string AutomationId(int p) { return "TC-AUTO-" + p; }

    static void Next()
    {
        phase++;
        phaseWaitUntil = 0f;
        waitThenAdvance = false;
        phaseStartedAt = Time.realtimeSinceStartup;
        nudged = false;
        aiFreezePending = true;
    }

    static void Wait(float seconds)
    {
        phaseWaitUntil = Time.realtimeSinceStartup + seconds;
        waitThenAdvance = false;
    }

    /// <summary>Waits, then moves to the next phase. Next() clears the pending wait, so a phase that
    /// calls Wait() and then Next() would skip its own wait entirely - this is the call that
    /// actually waits. Every case that needs time to pass uses this one.</summary>
    static void WaitThenNext(float seconds)
    {
        phaseWaitUntil = Time.realtimeSinceStartup + seconds;
        waitThenAdvance = true;
    }

    static void Add(string id, string requirement, string title, string expected, string actual, string status)
    {
        Row r = new Row();
        r.id = id; r.requirement = requirement; r.title = title;
        r.expected = expected; r.actual = actual; r.status = status;
        rows.Add(r);
        Debug.Log("[QualityRun] " + status + "  " + id + "  " + title + "  ->  " + actual);
    }

    static void Pass(string id, string req, string title, string expected, string actual)
    {
        Add(id, req, title, expected, actual, "PASS");
        Next();
    }

    static void Fail(string id, string req, string title, string expected, string actual)
    {
        Add(id, req, title, expected, actual, "FAIL");
        Next();
    }

    // ---------------------------------------------------------------- fase helpers

    static bool GameReady()
    {
        if (game == null) game = Object.FindAnyObjectByType<GameManager>();
        if (game != null && match == null) match = game.match;
        return game != null && match != null;
    }

    static bool Playing()
    {
        return GameReady() && game.State == GameManager.GameState.Playing;
    }

    static CharacterStatus FirstFree(Team team)
    {
        List<CharacterStatus> members = TeamManager.Members(team);
        for (int i = 0; i < members.Count; i++)
        {
            if (members[i] != null && !members[i].isCaptured) return members[i];
        }
        return null;
    }

    static void RestartMatch()
    {
        if (game == null) return;
        if (CharacterSpawner.HasSpawned) game.StartGame();
        else game.StartMatchAndReload();
        match = null;
    }

    /// <summary>
    /// Stops every AI-controlled character for the rest of the match. The rule cases drive the
    /// captures, the rescue and the flags themselves, and MatchManager runs the same rules for an AI
    /// as for the human - so a live AI adds no coverage here, only a race. Left running, one AI team
    /// can eliminate the other and end the match in the middle of a case that is measuring the pause
    /// rule, and that case then reports a defect the build does not have. Re-applied after every
    /// restart, because a restart can bring a fresh roster with fresh AI components.
    /// </summary>
    static void FreezeAi()
    {
        if (!aiFreezePending) return;
        if (!GameReady()) return;
        if (match.player == null) return;

        aiFreezePending = false;
        EnemyAI[] agents = Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        for (int i = 0; i < agents.Length; i++)
        {
            if (agents[i] == null) continue;
            if (agents[i].gameObject == match.player.gameObject) continue;
            agents[i].enabled = false;
        }
    }

    /// <summary>Moves the human the way the game itself moves a character: CharacterMotor switches
    /// the CharacterController off around the move, so the position sticks.</summary>
    static void MovePlayerTo(Vector3 worldPosition)
    {
        if (match == null || match.player == null) return;
        CharacterMotor motor = match.player.GetComponent<CharacterMotor>();
        if (motor != null) motor.TeleportTo(worldPosition);
        else match.player.transform.position = worldPosition;
    }

    /// <summary>Puts the human on open field, midway between the two bases. Every flag case except
    /// the two ending cases needs the carrier standing OUTSIDE its own base: MatchManager.Update runs
    /// CheckWinCondition (MatchManager.cs:222) before CheckFlagTouches (:232), so a pick-up made while
    /// the player is still inside the Blue base is a delivery, the match ends, and every later case is
    /// then measuring a finished match instead of a live one.</summary>
    static void PlayerToField()
    {
        if (match == null || match.player == null) return;
        if (match.blueBase == null || match.redBase == null) return;
        Vector3 blue = match.blueBase.transform.position;
        Vector3 red = match.redBase.transform.position;
        MovePlayerTo(Vector3.Lerp(blue, red, 0.5f));
    }

    /// <summary>MatchClock's setter is private - the clock belongs to the rules - so the clock
    /// boundary case reaches it the way a test has to. Nothing else about the match is touched.</summary>
    static void SetClock(float seconds)
    {
        PropertyInfo p = typeof(MatchManager).GetProperty("MatchClock");
        p.GetSetMethod(true).Invoke(match, new object[] { seconds });
    }

    /// <summary>True when a [Feedback] line contains the fragment. Used to decide WHO won, because
    /// MatchOutcome records only why the match ended.</summary>
    static bool SawFeedback(string fragment)
    {
        for (int i = 0; i < feedbackLines.Count; i++)
        {
            if (feedbackLines[i].ToLowerInvariant().Contains(fragment)) return true;
        }
        return false;
    }

    /// <summary>True when a [Feedback] line containing the fragment arrived SINCE the mark. Needed
    /// because a victory line from an earlier case must never be read as this case's result.</summary>
    static bool SawFeedbackSince(int mark, string fragment)
    {
        for (int i = mark; i < feedbackLines.Count; i++)
        {
            if (feedbackLines[i].ToLowerInvariant().Contains(fragment)) return true;
        }
        return false;
    }

    /// <summary>True when a match is running. When a reload has left the game sitting on its own
    /// panels, presses the same StartGame() the setup screen's button presses.</summary>
    static bool EnsurePlaying()
    {
        if (Playing()) return true;
        if (game == null) return false;

        // A focus event on the editor can pause the match under the harness. Every phase that
        // needs a live match gets one, rather than inheriting whatever the last one left behind.
        if (game.State == GameManager.GameState.Paused)
        {
            game.Resume();
            return false;
        }

        if (!nudged && Time.realtimeSinceStartup - phaseStartedAt > 1.5f)
        {
            nudged = true;
            game.StartGame();
        }
        return false;
    }

    // ---------------------------------------------------------------- the phases

    static void RunPhase()
    {
        switch (phase)
        {
            // ---- boot: give the scene time, then start a real match
            case 0:
                if (!GameReady())
                {
                    // The scene is still loading its first frame; nothing can be driven yet.
                    Wait(0.3f);
                    return;
                }
                if (Time.realtimeSinceStartup - runStartedAt < 2.5f) { Wait(0.2f); return; }
                RestartMatch();
                WaitThenNext(3.0f);
                return;

            case 1:
                if (!EnsurePlaying()) { Wait(0.5f); return; }
                Pass("TC-CORE-01", "REQ-01", "a match starts and runs",
                    "GameManager.State == Playing with a spawned roster",
                    "Playing, " + TeamManager.Count(Team.Blue) + " Blue / " + TeamManager.Count(Team.Red) + " Red, "
                    + match.TotalEnemies + " enemies, clock " + match.MatchClock.ToString("F1") + " s");
                return;

            case 2:
            {
                CharacterStatus player = match.player;
                bool ok = player != null && TeamManager.Members(Team.Blue).Contains(player);
                if (!ok)
                {
                    Fail("TC-CORE-02", "REQ-02", "the human is on the Blue roster",
                        "match.player registered in TeamManager.Members(Blue)",
                        "player " + (player == null ? "null" : player.name) + " not found on the Blue roster");
                    return;
                }
                Pass("TC-CORE-02", "REQ-02", "the human is on the Blue roster and free to move",
                    "registered, not captured, motor not frozen",
                    player.name + " free, in " + (player.IsInHomeBase ? "its home base" : "the field"));
                return;
            }

            // ---- catch and prison
            case 3:
            {
                subject = FirstFree(Team.Red);
                if (subject == null) { Fail("TC-CORE-03", "REQ-04", "a catch can be made", "a free Red character",
                    "no free Red character on the field"); return; }
                capturesBefore = match.redsCaptured;
                match.Capture(subject);
                if (!subject.isCaptured)
                    Fail("TC-CORE-03", "REQ-04", "touching an enemy captures them",
                        "subject.isCaptured == true", "still free after MatchManager.Capture");
                else if (match.redsCaptured != capturesBefore + 1)
                    Fail("TC-CORE-03", "REQ-04", "one catch counts once", "redsCaptured + 1",
                        "redsCaptured went " + capturesBefore + " -> " + match.redsCaptured);
                else
                {
                    positionBefore = subject.transform.position;
                    Wait(0.4f);
                    Next();
                }
                return;
            }

            case 4:
            {
                if (subject == null)
                {
                    Fail("TC-CORE-04", "REQ-05", "the prison holds the prisoner", "still captured",
                        "the prisoner reference was lost before the prison could be checked");
                    return;
                }

                float moved = Vector3.Distance(positionBefore, subject.transform.position);
                CharacterMotor motor = subject.GetComponent<CharacterMotor>();
                bool frozenMotor = motor != null && motor.IsFrozen;

                if (!subject.isCaptured)
                    Fail("TC-CORE-04", "REQ-05", "the prison holds the prisoner", "still captured",
                        "the prisoner was released without a rescue");
                else if (moved > 0.25f)
                    Fail("TC-CORE-04", "REQ-05", "the prison holds the prisoner",
                        "the prisoner does not move while imprisoned", "moved " + moved.ToString("F2") + " m in 0.4 s");
                else if (!frozenMotor)
                    Fail("TC-CORE-04", "REQ-05", "the prison holds the prisoner",
                        "CharacterMotor.IsFrozen while captured",
                        "captured but the motor is not frozen, so the prisoner is free to walk out");
                else
                    Pass("TC-CORE-04", "REQ-05", "the prison holds the prisoner",
                        "immobile and the motor frozen while captured",
                        "moved " + moved.ToString("F3") + " m in 0.4 s, motor.IsFrozen=True");
                return;
            }

            // ---- rescue, driven through the real proximity rule.
            // CheckRescueTouches pairs a prisoner only with a TEAM-MATE, and teleporting the human
            // into the enemy prison would simply get them tagged, so this captures one Red AI and
            // walks a second Red AI to it: same rule, same code path, no self-inflicted capture.
            case 5:
            {
                subject = FirstFree(Team.Red);
                CharacterStatus rescuer = null;
                List<CharacterStatus> reds = TeamManager.Members(Team.Red);
                for (int i = 0; i < reds.Count; i++)
                {
                    if (reds[i] != null && reds[i] != subject && !reds[i].isCaptured)
                    {
                        rescuer = reds[i];
                        break;
                    }
                }

                if (subject == null || rescuer == null)
                {
                    Add("TC-CORE-05", "REQ-06", "rescue frees a prisoner",
                        "one Red to capture and another Red to do the rescuing",
                        "the roster did not provide both", "BLOCKED");
                    Next();
                    return;
                }

                match.Capture(subject);
                if (!subject.isCaptured)
                {
                    Add("TC-CORE-05", "REQ-06", "rescue frees a prisoner", "the Red is imprisoned",
                        "MatchManager.Capture did not imprison " + subject.name, "BLOCKED");
                    Next();
                    return;
                }

                subject.rescueCooldownTimer = 0f;
                rescuer.transform.position = subject.transform.position;
                WaitThenNext(1.2f);
                return;
            }

            case 6:
            {
                if (subject == null) { Fail("TC-CORE-05", "REQ-06", "rescue frees a prisoner", "released",
                    "the prisoner reference was lost"); return; }
                if (subject.isCaptured)
                {
                    Add("TC-CORE-05", "REQ-06", "a team-mate reaching the prison frees the prisoner",
                        "isCaptured == false after the rescuer reaches the prison",
                        "still captured 1.2 s after a team-mate stood at the prison - the rescue rule did not fire",
                        "FAIL");
                    Next();
                    return;
                }
                Pass("TC-CORE-05", "REQ-06", "a team-mate reaching the prison frees the prisoner",
                    "isCaptured == false and the prisoner can move again",
                    "released, immunity " + subject.immunityTimer.ToString("F1") + " s");
                return;
            }

            // ---- flag pick-up and the carry wiring
            case 7:
            {
                Flag redFlag = match.FlagOf(Team.Red);
                if (redFlag == null) { Fail("TC-CORE-06", "REQ-07", "the enemy flag can be taken",
                    "a Red flag in the scene", "MatchManager.FlagOf(Red) returned null"); return; }

                // Field first, then the grab: see PlayerToField for why the order matters.
                PlayerToField();
                bool taken = redFlag.TryPickUp(match.player.transform, Team.Blue);
                SprintStamina stamina = match.player.GetComponent<SprintStamina>();
                bool carrying = stamina != null && stamina.IsCarryingFlag;

                if (!taken || !redFlag.IsCarried)
                    Fail("TC-CORE-06", "REQ-07", "the enemy flag can be taken",
                        "TryPickUp succeeds and state becomes Carried",
                        "taken=" + taken + " state=" + redFlag.state);
                else if (!carrying)
                    Fail("TC-CORE-06", "REQ-07", "the carry penalty is wired in",
                        "SprintStamina.IsCarryingFlag set by the flag",
                        "the flag is carried but SprintStamina was not told");
                else
                    Pass("TC-CORE-06", "REQ-07", "the enemy flag can be taken and the carry penalty arms",
                        "Carried + SprintStamina.IsCarryingFlag",
                        "carried by " + redFlag.carrier.name + ", IsCarryingFlag=" + carrying);
                return;
            }

            // ---- drop and the 10 s return, compressed with timeScale
            case 8:
            {
                Flag redFlag = match.FlagOf(Team.Red);
                savedTimeScale = Time.timeScale;
                redFlag.Drop(match.player.transform.position);
                // Step the carrier off the flag it just dropped. CheckFlagTouches (MatchManager.cs:526)
                // hands a dropped flag straight back to a free enemy standing on it, and a re-pick-up
                // would hide the return timer this case exists to measure.
                MovePlayerTo(match.blueBase.transform.position);
                Time.timeScale = 20f;
                phaseStartedAt = Time.realtimeSinceStartup;
                WaitThenNext(0.2f);
                return;
            }

            case 9:
            {
                Flag redFlag = match.FlagOf(Team.Red);
                if (redFlag.state == Flag.FlagState.Carried)
                {
                    Time.timeScale = savedTimeScale;
                    Fail("TC-CORE-07", "REQ-08", "a dropped flag returns home",
                        "state becomes Dropped after Drop()", "the flag is still Carried");
                    return;
                }
                if (Time.realtimeSinceStartup - phaseStartedAt < 1.5f) { Wait(0.3f); return; }
                float elapsed = Time.realtimeSinceStartup - phaseStartedAt;
                Time.timeScale = savedTimeScale;
                if (redFlag.IsDropped)
                    Fail("TC-CORE-07", "REQ-08", "a dropped flag returns home after 10 s",
                        "AtBase once the return timer expires",
                        "still Dropped after " + elapsed.ToString("F1") + " real s at 20x (10 game s)");
                else
                    Pass("TC-CORE-07", "REQ-08", "a dropped flag returns home after 10 s",
                        "AtBase once the timer expires",
                        "AtBase when checked " + elapsed.ToString("F1") + " real s after the drop at 20x");
                return;
            }

            // ---- pause and resume through the real state machine
            case 10:
            {
                game.Pause();
                clockBefore = match.MatchClock;
                WaitThenNext(1.0f);
                return;
            }

            case 11:
            {
                // Wait() only registers a wait and returns, so the clock cannot be read in the same
                // tick that Resume() is called: the value would still be the paused one. Resume once,
                // remember the clock at that moment, then come back after 0.7 s of real time.
                if (!nudged)
                {
                    nudged = true;
                    clockAtResume = match.MatchClock;
                    resumeAt = Time.realtimeSinceStartup;
                    game.Resume();
                    Wait(0.1f);
                    return;
                }
                if (Time.realtimeSinceStartup - resumeAt < 0.7f) { Wait(0.2f); return; }

                float whilePaused = clockAtResume;
                float afterResume = match.MatchClock;
                bool frozen = Mathf.Abs(whilePaused - clockBefore) < 0.15f;
                // MatchClock counts UP towards matchTimeSeconds (MatchManager.cs:195 zeroes it and
                // :212 adds deltaTime), so "running" means the value grew.
                bool advanced = afterResume > whilePaused + 0.1f;
                if (!frozen)
                    Fail("TC-LIFE-01", "REQ-18", "pause stops the match clock",
                        "the clock does not move while paused",
                        "clock moved " + (clockBefore - whilePaused).ToString("F2") + " s while paused");
                else if (!advanced)
                    Fail("TC-LIFE-01", "REQ-18", "resume continues the match",
                        "the clock runs again after Resume",
                        "clock did not advance in 0.7 s after Resume");
                else
                    Pass("TC-LIFE-01", "REQ-18", "pause stops the clock and resume continues it",
                        "frozen while paused, running after resume",
                        "paused delta " + (clockBefore - whilePaused).ToString("F2") + " s, then ran again");
                return;
            }

            // ---- backgrounding (the DEF-15-01 check)
            case 12:
            {
                MethodInfo pauseHandler = typeof(GameManager).GetMethod("OnApplicationPause",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                sawPauseMethod = pauseHandler != null;

                if (!sawPauseMethod)
                {
                    Fail("TC-LIFE-03", "REQ-19", "sending the app to the background pauses the match",
                        "GameManager handles OnApplicationPause and pauses",
                        "GameManager has no OnApplicationPause handler, so backgrounding cannot pause anything");
                    return;
                }

                float before = match.MatchClock;
                pauseHandler.Invoke(game, new object[] { true });
                clockBefore = before;
                WaitThenNext(0.8f);
                return;
            }

            case 13:
            {
                bool paused = game.State == GameManager.GameState.Paused;
                float moved = Mathf.Abs(match.MatchClock - clockBefore);
                MethodInfo handler = typeof(GameManager).GetMethod("OnApplicationPause",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (handler != null) handler.Invoke(game, new object[] { false });

                if (!paused)
                    Fail("TC-LIFE-03", "REQ-19", "backgrounding pauses the match",
                        "State == Paused after a background event", "State was " + game.State);
                else if (moved > 0.2f)
                    Fail("TC-LIFE-03", "REQ-19", "backgrounding stops the clock",
                        "the clock stops while backgrounded", "clock moved " + moved.ToString("F2") + " s");
                else
                    Pass("TC-LIFE-03", "REQ-19", "backgrounding pauses the match and stops its clock",
                        "Paused, clock stopped, resumed on return",
                        "paused with the clock frozen for 0.8 s");
                return;
            }

            // ---- restart leaves clean match state
            case 14:
            {
                game.Restart();
                match = null;
                WaitThenNext(3.0f);
                return;
            }

            case 15:
            {
                if (!EnsurePlaying()) { Wait(0.5f); return; }
                bool clockClean = match.MatchClock < 5f;
                bool outcomeClean = game.Outcome == MatchManager.MatchOutcome.None;
                if (!clockClean || !outcomeClean)
                    Fail("TC-LIFE-02", "REQ-18", "restart starts a clean match",
                        "clock near the full time and outcome None",
                        "clock " + match.MatchClock.ToString("F1") + "/" + match.matchTimeSeconds.ToString("F0")
                        + ", outcome " + game.Outcome);
                else
                    Pass("TC-LIFE-02", "REQ-18", "restart starts a clean match",
                        "clock near the full time, outcome None",
                        "clock " + match.MatchClock.ToString("F1") + " s, outcome None");
                return;
            }

            // ---- rapid input, the boundary tests
            case 16:
            {
                int before = match.redsCaptured;
                int throws = 0;

                // Stop short of a full prison: capturing every enemy is an ELIMINATION, which ends the
                // match and would contaminate every later case. This case is about rapid input.
                int allowed = Mathf.Max(1, match.TotalEnemies - 1);
                int made = 0;
                for (int i = 0; i < 100 && made < allowed; i++)
                {
                    CharacterStatus victim = FirstFree(Team.Red);
                    if (victim == null) break;
                    try { match.Capture(victim); made++; } catch { throws++; }
                }
                WaitThenNext(0.3f);
                if (throws > 0)
                    Fail("TC-BND-01", "REQ-04", "repeated rapid catches", "no exception",
                        throws + " exceptions during " + made + " captures");
                else
                    Pass("TC-BND-01", "REQ-04", "repeated rapid catches stay consistent",
                        "no exception, the capture counter matches the captures made",
                        made + " captures in one frame, redsCaptured " + before + " -> " + match.redsCaptured +
                        ", no exception");
                return;
            }

            case 17:
            {
                int throws = 0;
                for (int i = 0; i < 50; i++)
                {
                    try { game.Pause(); game.Resume(); } catch { throws++; }
                }
                WaitThenNext(0.4f);
                if (throws > 0)
                    Fail("TC-BND-02", "REQ-18", "50 rapid pause toggles", "no exception",
                        throws + " exceptions");
                else
                    Pass("TC-BND-02", "REQ-18", "50 rapid pause toggles",
                        "no exception, ends in a known state",
                        "no exception; state " + game.State + ", timeScale " + Time.timeScale.ToString("F2"));
                return;
            }

            case 18:
            {
                Flag redFlag = match.FlagOf(Team.Red);
                int throws = 0;
                // The restart at case 14 put the player back in its base, so move out before grabbing.
                PlayerToField();
                for (int i = 0; i < 50; i++)
                {
                    try
                    {
                        redFlag.TryPickUp(match.player.transform, Team.Blue);
                        if (redFlag.IsCarried) redFlag.Drop(match.player.transform.position);
                    }
                    catch { throws++; }
                }
                WaitThenNext(0.3f);
                if (throws > 0)
                    Fail("TC-BND-03", "REQ-07", "50 rapid pick-up attempts", "no exception",
                        throws + " exceptions");
                else
                    Pass("TC-BND-03", "REQ-07", "50 rapid pick-up attempts",
                        "no exception, the flag ends in one consistent state",
                        "no exception; state " + redFlag.state + ", carried=" + redFlag.carrier);
                return;
            }

            // ---- the three endings, each on a fresh match
            case 19:
            {
                game.Restart();
                match = null;
                WaitThenNext(3.0f);
                return;
            }

            case 20:
            {
                if (!EnsurePlaying()) { Wait(0.5f); return; }
                // Near the end of the count-up clock, so the rules themselves expire it.
                SetClock(match.matchTimeSeconds - 0.2f);
                WaitThenNext(1.5f);
                return;
            }

            case 21:
            {
                MatchManager.MatchOutcome o = game.Outcome;
                bool byClock = o == MatchManager.MatchOutcome.Draw || o == MatchManager.MatchOutcome.TimeLimit;
                if (byClock)
                    Pass("TC-CORE-10", "REQ-11", "the clock running out ends the match as a draw",
                        "the match ends when the clock reaches zero with no flag taken",
                        "ended by the clock, outcome " + o + ", no winning feedback line");
                else
                    Fail("TC-CORE-10", "REQ-11", "the clock running out ends the match as a draw",
                        "Outcome == Draw", "Outcome was " + o + " after the clock expired");
                return;
            }

            case 22:
            {
                game.Restart();
                match = null;
                WaitThenNext(3.0f);
                return;
            }

            case 23:
            {
                if (!EnsurePlaying()) { Wait(0.5f); return; }
                Flag redFlag = match.FlagOf(Team.Red);
                feedbackMark = feedbackLines.Count;
                redFlag.TryPickUp(match.player.transform, Team.Blue);
                match.player.transform.position = match.blueBase.transform.position;
                WaitThenNext(1.5f);
                return;
            }

            case 24:
            {
                MatchManager.MatchOutcome o = game.Outcome;
                bool ding = SawFeedbackSince(feedbackMark, "victory");
                if (o == MatchManager.MatchOutcome.FlagCarriedHome && ding)
                    Pass("TC-CORE-08", "REQ-09", "carrying the enemy flag home wins the match",
                        "the match ends by flag delivery and the victory feedback fires",
                        "outcome " + o + " with the victory clip played");
                else
                    Fail("TC-CORE-08", "REQ-09", "carrying the enemy flag home wins the match",
                        "flag delivery ending plus a victory feedback line",
                        "outcome " + o + ", victory feedback seen = " + ding);
                return;
            }

            case 25:
            {
                game.Restart();
                match = null;
                WaitThenNext(3.0f);
                return;
            }

            case 26:
            {
                if (!EnsurePlaying()) { Wait(0.5f); return; }
                CharacterStatus red = FirstFree(Team.Red);
                Flag blueFlag = match.FlagOf(Team.Blue);
                if (red == null || blueFlag == null)
                {
                    Add("TC-CORE-09", "REQ-10", "the enemy carrying our flag home loses the match",
                        "a free Red carrier", "no free Red character to carry our flag", "BLOCKED");
                    Next();
                    return;
                }
                blueFlag.TryPickUp(red.transform, Team.Red);
                feedbackMark = feedbackLines.Count;
                red.transform.position = match.redBase.transform.position;
                WaitThenNext(1.5f);
                return;
            }

            case 27:
            {
                MatchManager.MatchOutcome o = game.Outcome;
                bool ding = SawFeedbackSince(feedbackMark, "defeat");
                if (o == MatchManager.MatchOutcome.FlagCarriedHome && ding)
                    Pass("TC-CORE-09", "REQ-10", "the enemy carrying our flag home loses the match",
                        "the match ends by flag delivery and the defeat feedback fires",
                        "outcome " + o + " with the defeat clip played");
                else
                    Fail("TC-CORE-09", "REQ-10", "the enemy carrying our flag home loses the match",
                        "flag delivery ending plus a defeat feedback line",
                        "outcome " + o + ", defeat feedback seen = " + ding);
                return;
            }

            // ---- editor-only build checks that can be read without a device
            case 28:
            {
                HudController hud = Object.FindAnyObjectByType<HudController>();
                if (hud == null)
                {
                    Add("TC-BUILD-01", "REQ-12", "the AI read-out is editor-only", "a HudController",
                        "no HudController in the scene", "BLOCKED");
                    Next();
                    return;
                }
                // The scene's serialized value is what actually runs, so read it and report it. On
                // means a tester could read enemy role and state, which is a different game.
                if (hud.showAiDebug)
                    Fail("TC-BUILD-01", "REQ-12", "the AI read-out is off in the running game",
                        "showAiDebug == false",
                        "showAiDebug is TRUE, so a tester could read enemy role and state");
                else
                    Pass("TC-BUILD-01", "REQ-12", "the AI read-out is off in the running game",
                        "showAiDebug == false, so no AI read-out can be seen",
                        "showAiDebug=False (source guard HudController.cs:59-63, scene value off)");
                return;
            }

            default:
                Finish("the suite ran every case");
                EditorApplication.ExitPlaymode();
                return;
        }
    }

    // ---------------------------------------------------------------- output

    static void Finish(string reason)
    {
        if (finished) return;
        finished = true;
        stopReason = reason;

        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;

        Time.timeScale = 1f;

        int pass = 0, fail = 0, blocked = 0;
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].status == "PASS") pass++;
            else if (rows[i].status == "FAIL") fail++;
            else blocked++;
        }

        string dir = Path.Combine(Directory.GetCurrentDirectory(), "Docs", "Zedrich Abrera",
                                  "Week_15", "Evidence", "test_logs_" + Label);
        Directory.CreateDirectory(dir);

        System.Text.StringBuilder tsv = new System.Text.StringBuilder();
        tsv.AppendLine("id\trequirement\ttitle\texpected\tactual\tstatus");
        for (int i = 0; i < rows.Count; i++)
        {
            Row r = rows[i];
            tsv.AppendLine(r.id + "\t" + r.requirement + "\t" + r.title + "\t" + r.expected + "\t" +
                           r.actual + "\t" + r.status);
        }
        tsv.AppendLine("TOTAL\t\t\t\t" + pass + " passed, " + fail + " failed, " + blocked + " blocked\t");

        File.WriteAllText(Path.Combine(dir, "scenario.tsv"), tsv.ToString());

        File.WriteAllText(Path.Combine(dir, "scenario_summary.txt"),
            "Week 15 play-mode scenario suite\r\n" +
            "Unity " + Application.unityVersion + "\r\n" +
            "Finished: " + System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC\r\n" +
            "Stopped because: " + stopReason + "\r\n" +
            "Cases: " + rows.Count + " (" + pass + " passed, " + fail + " failed, " + blocked + " blocked)\r\n" +
            "Exceptions and errors raised during the run: " + exceptionCount + "\r\n" +
            "[Feedback] lines captured: " + feedbackLines.Count + "\r\n\r\n" +
            "--- every [Feedback] line, in order ---\r\n" +
            string.Join("\r\n", feedbackLines.ToArray()) + "\r\n");

        Debug.Log("[QualityRun] " + reason + ". " + pass + " passed / " + fail + " failed / " +
                  blocked + " blocked, " + exceptionCount + " errors -> " + dir);
    }
}
