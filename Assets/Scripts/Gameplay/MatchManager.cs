using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The single authority for the rules of the game. Nothing else decides who wins a touch.
///
/// Every rule here is written against a TEAM, never against "the player" or "the enemy", so
/// the exact same code covers a 1v1 and a 4v4. The rosters come from TeamManager, which the
/// characters fill in themselves when they are switched on - this script never searches the
/// scene, and never calls FindObjectsOfType.
///
/// The rulings it makes:
///   - A touch between two FREE OPPONENTS closer than tagDistance. The character with the
///     LOWER fieldTime wins; the loser is teleported into the enemy prison and frozen.
///   - Exactly equal fieldTime is a draw, and nothing happens.
///   - CAPTURE THE FLAG: a free character may pick up the ENEMY flag, and wins only by
///     carrying it into its OWN base. Touching the flag is not a win by itself.
///   - If the carrier is captured, the flag drops exactly where it stood, and is sent home
///     either by a free member of the owning team touching it, or by its own drop timeout.
///   - RESCUE: a free character touching an imprisoned TEAM-MATE frees it. The freed character
///     goes to the prison exit with fieldTime 0 and a short immunity; the rescuer gets nothing.
///   - ELIMINATION: every member of a team imprisoned -> that team loses. The human being
///     captured while team-mates are still free is NOT an ending - the match plays on.
///   - TIME LIMIT: optional. On expiry, more free members wins, then the fresher team, then draw.
///
/// The order inside Update() is deliberate: rescues, then the win, then touches, then the clock,
/// then elimination. Rescues first and elimination last is what makes "you cannot be eliminated
/// in the same frame a team-mate frees you" true.
///
/// Note on bases: a character inside its own base always has fieldTime 0, so it can never lose
/// a touch there. That is how a base acts as a safe zone, with no extra special case needed.
/// </summary>
public class MatchManager : MonoBehaviour
{
    /// <summary>
    /// HOW the match ended. GameManager reads this to put the right words on the end screen -
    /// "you carried the flag home" and "every enemy was locked up" are both victories, but they
    /// are not the same story, and an end screen that says the wrong one looks like a bug.
    /// </summary>
    public enum MatchOutcome
    {
        /// <summary>Still playing, or never started.</summary>
        None,

        /// <summary>Somebody carried the enemy flag into their own base. The normal win.</summary>
        FlagCarriedHome,

        /// <summary>Every member of a team ended up in the enemy prison.</summary>
        Elimination,

        /// <summary>The optional matchTimeSeconds limit expired with a winner.</summary>
        TimeLimit,

        /// <summary>The time limit expired with the two teams genuinely level.</summary>
        Draw
    }

    [Tooltip("How the match ended. Read by GameManager for the end screen.")]
    public MatchOutcome outcome = MatchOutcome.None;

    [Header("Scene references")]
    [Tooltip("The Blue safe zone (Base_Blue).")]
    public BaseZone blueBase;

    [Tooltip("The Red safe zone (Base_Red).")]
    public BaseZone redBase;

    [Tooltip("Where a captured BLUE character is held (PrisonForBlue).")]
    public Transform prisonForBlue;

    [Tooltip("Where a captured RED character is held (PrisonForRed).")]
    public Transform prisonForRed;

    [Tooltip("The Blue flag. Only a free RED may carry it.")]
    public Flag blueFlag;

    [Tooltip("The Red flag. Only a free BLUE may carry it.")]
    public Flag redFlag;

    [Tooltip("The human player. No longer needed for the rules - it is used only by the " +
             "debug drop key, and the HUD reads it for the freshness and stamina bars.")]
    public CharacterStatus player;

    [Header("Rules")]
    [Tooltip("How close two opponents must be to count as a touch, in metres.")]
    public float tagDistance = 1.2f;

    [Tooltip("How close a free character must be to the enemy flag to grab it, in metres.")]
    public float flagDistance = 1.5f;

    [Tooltip("How close a free character must be to its own DROPPED flag to send it home, in metres.")]
    public float returnDistance = 1.2f;

    [Tooltip("Difference in fieldTime below which a touch counts as a draw.")]
    public float tieEpsilon = 0.01f;

    [Tooltip("ON = a touch only counts when BOTH characters are out in the field, " +
             "so no base can be attacked at all. " +
             "OFF (default) = the bases are protected purely by fieldTime being 0 inside them, " +
             "which means an enemy base can be raided by a perfectly fresh (fieldTime 0) character.")]
    public bool requiresBothOutsideBases = false;

    [Tooltip("ON = equal fieldTime means nothing happens.")]
    public bool tieIsNoCapture = true;

    [Header("Rescue")]
    [Tooltip("How close a FREE character must be to an imprisoned TEAM-MATE to free it, in metres.")]
    public float rescueDistance = 1.5f;

    [Tooltip("How far from the prison centre a freed character is placed, in metres. " +
             "2.1 = the 3 x 3 plate's 1.5 m half-width plus 0.6 m of clearance, so the freed " +
             "character never lands back inside its own cage.")]
    public float rescueExitOffset = 2.1f;

    [Tooltip("Seconds a freed character cannot be tagged. Covers the walk out of the cage, so a " +
             "rescuer cannot free somebody straight into a waiting enemy. The RESCUER gets none.")]
    public float immunitySeconds = 2f;

    [Tooltip("Seconds before the SAME prisoner may be freed again. Stops two teams trading one " +
             "prisoner back and forth in a chain.")]
    public float rescueCooldown = 3f;

    [Header("Match limit")]
    [Tooltip("Optional time limit in seconds. On expiry the team with more free members wins, " +
             "then the fresher team, then it is a draw. 0 = no limit.")]
    public float matchTimeSeconds = 180f;

    [Header("Debug")]
    [Tooltip("Editor/desktop only: press SPACE to drop the flag the player is carrying, so the " +
             "drop, return and timeout rules can be watched without ending the match. " +
             "Stripped out of a release build by the #if.")]
    public bool allowDebugDrop = true;

    [Tooltip("Log every touch, capture, rescue and match end. Turn off for the final build.")]
    public bool logRules = true;

    [Header("State - read only")]
    [Tooltip("Set by GameManager. The rules only run while this is true.")]
    public bool matchActive;

    [Tooltip("How many RED characters are currently imprisoned. The authoritative per-team counts " +
             "live in TeamManager; this is here for anything that wants a single number.")]
    public int redsCaptured;

    /// <summary>Raised with the team that won (carried the enemy flag home).</summary>
    public event Action<Team> TeamWon;

    /// <summary>Raised with the team that lost (an opponent carried their flag home).</summary>
    public event Action<Team> TeamLost;

    /// <summary>Raised when the HUMAN's team wins. The human is always on Blue.</summary>
    public event Action PlayerWon;

    /// <summary>Raised when the HUMAN's team loses. The human is always on Blue.</summary>
    public event Action PlayerLost;

    /// <summary>Raised whenever the number of captured RED characters changes.</summary>
    public event Action<int> CaptureCountChanged;

    /// <summary>Raised on every capture AND every rescue, so a HUD can repaint both teams'
    /// counters without polling. Carries no numbers: the live counts live in TeamManager.</summary>
    public event Action TeamStateChanged;

    /// <summary>Raised when the time limit runs out with nothing to separate the two teams.</summary>
    public event Action MatchDrawn;

    /// <summary>Seconds of match time played. Only counts up while the rules are running, so the
    /// Paused screen does not eat the clock.</summary>
    public float MatchClock { get; private set; }

    /// <summary>How many characters the Red team has.</summary>
    public int TotalEnemies
    {
        get { return TeamManager.Count(Team.Red); }
    }

    void Awake()
    {
        MatchClock = 0f;

        // Loud, early complaints beat a silent rules bug.
        if (redFlag == null || blueFlag == null) Debug.LogError("MatchManager: flags are not assigned.");
        if (prisonForRed == null || prisonForBlue == null) Debug.LogError("MatchManager: prisons are not assigned.");
        if (blueBase == null || redBase == null) Debug.LogError("MatchManager: bases are not assigned.");
        if (player == null) Debug.LogWarning("MatchManager: 'player' is not assigned (only the debug drop key uses it).");
    }

    void Update()
    {
        // Keep the registry in step with the rules. EnemyAI reads this and refuses to think
        // while the match is not live, so nobody wanders around on the Title or Paused screens.
        TeamManager.MatchActive = matchActive;

        if (!matchActive) return;

        MatchClock += Time.deltaTime;

        // RESCUES RUN FIRST, and that ordering is the whole reason a team can never be eliminated
        // unfairly: a rescue landing in the same frame as a capture is honoured, and the
        // elimination check at the bottom of the frame therefore sees the freed team-mate.
        CheckRescueTouches();

        // Winning is checked next. If the carrier steps into its own base in the same frame as
        // a tag, "it got home" is the result it earned, and the flag is handed back before any
        // other rule can act on it. That order is unchanged from the shipped 1v2 game.
        CheckWinCondition();
        if (!matchActive) return;

        HandleDebugDrop();

        // Touches are resolved before flag grabs: if you are tagged you are no longer "free",
        // so reaching the flag in the same instant does not save you.
        CheckCharacterTouches();
        if (!matchActive) return;

        CheckFlagTouches();
        if (!matchActive) return;

        // The optional time limit, then elimination LAST: nothing may end a match in the same
        // frame a rescue happened, and elimination is the roughest of the endings.
        CheckMatchClock();
        if (!matchActive) return;

        CheckElimination();
    }

    // ------------------------------------------------------------------- touches

    /// <summary>
    /// Every free Blue against every free Red. At 4v4 that is at most sixteen distance checks
    /// per frame, and each one is a couple of subtractions and a compare - cheap enough for a
    /// 30 FPS Android build, and it needs no colliders or triggers at all.
    /// </summary>
    void CheckCharacterTouches()
    {
        List<CharacterStatus> blues = TeamManager.Members(Team.Blue);
        List<CharacterStatus> reds = TeamManager.Members(Team.Red);

        for (int b = 0; b < blues.Count; b++)
        {
            CharacterStatus blue = blues[b];
            if (blue == null || blue.isCaptured) continue;

            for (int r = 0; r < reds.Count; r++)
            {
                CharacterStatus red = reds[r];
                if (red == null || red.isCaptured) continue;   // prisoners cannot fight

                // A just-rescued character is immune for a couple of seconds, so it cannot be
                // re-captured on the prison doorstep. The rescuer gets no such protection.
                if (blue.IsImmune || red.IsImmune) continue;

                float distance = FlatDistance(blue.transform.position, red.transform.position);
                if (distance > tagDistance) continue;

                // Optional rule: ignore every touch that happens inside a base.
                if (requiresBothOutsideBases &&
                    (IsInAnyBase(blue.transform.position) || IsInAnyBase(red.transform.position)))
                {
                    continue;
                }

                ResolveTouch(blue, red);

                if (!matchActive) return;
                if (blue.isCaptured) break;   // he is out of the match, stop pairing him up
            }
        }
    }

    /// <summary>Applies the "lower fieldTime wins" rule to one touching pair.</summary>
    void ResolveTouch(CharacterStatus blue, CharacterStatus red)
    {
        float blueTime = blue.fieldTime;
        float redTime = red.fieldTime;

        // A draw: nothing happens.
        if (tieIsNoCapture && Mathf.Abs(blueTime - redTime) <= tieEpsilon)
        {
            if (logRules)
            {
                Debug.Log("[Match] draw - " + blue.name + " and " + red.name +
                          " both have fieldTime " + blueTime.ToString("F2") + ", nobody is captured.");
            }
            return;
        }

        bool blueWins = blueTime < redTime;
        CharacterStatus loser = blueWins ? red : blue;

        if (logRules)
        {
            Debug.Log("[Match] touch: " + blue.name + " fieldTime=" + blueTime.ToString("F2") +
                      " vs " + red.name + " fieldTime=" + redTime.ToString("F2") +
                      "  -> " + (blueWins ? blue.name : red.name) + " wins (lower is fresher), " +
                      loser.name + " is captured.");
        }

        Capture(loser);
    }

    /// <summary>
    /// Freezes a character in the enemy prison and counts it. Public so the rescue rules and
    /// any future rule have exactly one way to capture somebody.
    /// </summary>
    public void Capture(CharacterStatus loser)
    {
        if (loser == null || loser.isCaptured) return;

        // He may have been carrying a flag. It has to drop FIRST: Capture teleports him to
        // prison, and dropping afterwards would leave the flag sitting inside the prison.
        Flag carried = FlagCarriedBy(loser);
        if (carried != null)
        {
            if (logRules) Debug.Log("[Match] the flag carrier " + loser.name + " was caught -> " + carried.name + " drops where he stood");
            carried.Drop(loser.transform.position);
        }

        // Which prison cell to stand in: one cell per team-mate already locked up, so two
        // prisoners never share a spot inside a 3 x 3 plate.
        int slot = TeamManager.CapturedCount(loser.team);
        loser.Capture(PrisonSpot(PrisonOf(loser.team), slot));

        if (loser.team == Team.Red)
        {
            redsCaptured = TeamManager.CapturedCount(Team.Red);
            if (CaptureCountChanged != null) CaptureCountChanged(redsCaptured);
        }

        if (logRules)
        {
            Debug.Log("[Match] captured " + loser.name + " (" + loser.team + ").  " + TeamCounts());
        }

        if (TeamStateChanged != null) TeamStateChanged();
    }

    // ------------------------------------------------------------------- rescue

    /// <summary>
    /// A free character standing next to an imprisoned TEAM-MATE frees it.
    ///
    /// Judged by distance, exactly like the tag and flag rules - no triggers, no colliders, and
    /// nothing below can duplicate the decision. At 4v4 that is at most four prisoners checked
    /// against at most four rescuers, a couple of dozen float compares per frame.
    ///
    /// Two guards keep this from becoming a ping-pong loop:
    ///   - a prisoner that was just freed ignores rescuers for `rescueCooldown` seconds;
    ///   - the freed character is immune for `immunitySeconds`, so it cannot be tagged instantly.
    /// </summary>
    void CheckRescueTouches()
    {
        for (int t = 0; t < TeamUtil.All.Length; t++)
        {
            Team team = TeamUtil.All[t];
            Transform prison = PrisonOf(team);
            if (prison == null) continue;

            List<CharacterStatus> members = TeamManager.Members(team);

            for (int p = 0; p < members.Count; p++)
            {
                CharacterStatus prisoner = members[p];
                if (prisoner == null || !prisoner.isCaptured) continue;
                if (prisoner.rescueCooldownTimer > 0f) continue;   // just freed: no chain-rescue

                for (int r = 0; r < members.Count; r++)
                {
                    CharacterStatus rescuer = members[r];
                    if (rescuer == null || rescuer == prisoner) continue;
                    if (rescuer.isCaptured) continue;              // prisoners cannot free anybody

                    float distance = FlatDistance(rescuer.transform.position, prisoner.transform.position);
                    if (distance > rescueDistance) continue;

                    Rescue(rescuer, prisoner, prison);
                    break;                                          // this prisoner is free now
                }
            }
        }
    }

    /// <summary>Frees one prisoner. The only place a rescue is applied.</summary>
    void Rescue(CharacterStatus rescuer, CharacterStatus prisoner, Transform prison)
    {
        prisoner.Release(RescueExit(prison, prisoner.team), immunitySeconds, rescueCooldown);

        if (logRules)
        {
            Debug.Log("[Match] RESCUE: " + rescuer.name + " (" + rescuer.team + ") freed " +
                      prisoner.name + " at " + prison.name + " -> fieldTime 0.00, " +
                      immunitySeconds.ToString("F1") + " s immunity, " +
                      rescueCooldown.ToString("F1") + " s before it can be freed again.  " +
                      TeamCounts() + "   rescuer fieldTime=" + rescuer.fieldTime.ToString("F2") +
                      " (no immunity for the rescuer)");
        }

        if (TeamStateChanged != null) TeamStateChanged();
    }

    /// <summary>
    /// Where a freed character is placed: just outside the prison, on the side facing its OWN
    /// base. One rule covers both teams by construction - a freed Blue is put down on the Blue
    /// side of the cage, a freed Red on the Red side - and it means the obvious next move (run
    /// home) is also the direction the character is already facing.
    /// </summary>
    Vector3 RescueExit(Transform prison, Team team)
    {
        BaseZone home = BaseOf(team);
        float towardsHome = 1f;

        if (home != null)
        {
            float delta = home.transform.position.x - prison.position.x;
            if (Mathf.Abs(delta) > 0.01f) towardsHome = Mathf.Sign(delta);
        }

        return new Vector3(prison.position.x + towardsHome * rescueExitOffset, 0f, prison.position.z);
    }

    // --------------------------------------------------------------------- flags

    /// <summary>
    /// The win, for EITHER team: be free, be carrying the ENEMY flag, and step inside your OWN
    /// base. Touching the flag on its own does nothing - that is the capture-the-flag rule.
    /// </summary>
    void CheckWinCondition()
    {
        for (int t = 0; t < TeamUtil.All.Length; t++)
        {
            Team team = TeamUtil.All[t];

            Flag enemyFlag = FlagOf(team.Opponent());
            if (enemyFlag == null || !enemyFlag.IsCarried || enemyFlag.carrier == null) continue;

            List<CharacterStatus> members = TeamManager.Members(team);
            for (int i = 0; i < members.Count; i++)
            {
                CharacterStatus member = members[i];
                if (member == null || member.isCaptured) continue;
                if (enemyFlag.carrier != member.transform) continue;   // somebody else is carrying it
                if (!member.IsInHomeBase) continue;

                if (logRules)
                {
                    Debug.Log("[Match] " + member.name + " (" + team + ") carried the " + enemyFlag.name +
                              " into its own base -> " + team + " WINS.  " + TeamCounts());
                }

                enemyFlag.ReturnHome();   // the flag goes back on its pole; the match is over anyway
                EndMatch(team, MatchOutcome.FlagCarriedHome);
                return;
            }
        }
    }

    /// <summary>
    /// Everything to do with the flags themselves: stealing one, and getting a dropped one home.
    /// Generic in both directions, so a Blue AI can steal the Red flag exactly as the human
    /// does, and a Red AI returns the Red flag exactly as before.
    /// </summary>
    void CheckFlagTouches()
    {
        for (int t = 0; t < TeamUtil.All.Length; t++)
        {
            Team owner = TeamUtil.All[t];
            Flag flag = FlagOf(owner);
            if (flag == null) continue;

            // --- a free member of the OTHER team steals it ---
            if (!flag.IsCarried)
            {
                List<CharacterStatus> raiders = TeamManager.Members(owner.Opponent());
                for (int i = 0; i < raiders.Count; i++)
                {
                    CharacterStatus raider = raiders[i];
                    if (raider == null || raider.isCaptured) continue;
                    if (FlatDistance(raider.transform.position, flag.transform.position) > flagDistance) continue;

                    // TryPickUp refuses a character from the owning team, so nobody can "steal"
                    // their own flag by standing on it.
                    if (flag.TryPickUp(raider.transform, raider.team) && logRules)
                    {
                        Debug.Log("[Match] " + raider.name + " (" + raider.team + ") picked up the " + flag.name + " - now carry it home");
                    }
                }
            }

            // --- a free member of the OWNING team sends its own dropped flag home ---
            // The AI's ReturnFlag state does the walking; the touch itself is judged here,
            // exactly like every other touch in the game, so there is still only one authority.
            if (flag.IsDropped)
            {
                List<CharacterStatus> owners = TeamManager.Members(owner);
                for (int i = 0; i < owners.Count; i++)
                {
                    CharacterStatus member = owners[i];
                    if (member == null || member.isCaptured) continue;
                    if (FlatDistance(member.transform.position, flag.transform.position) > returnDistance) continue;

                    if (logRules) Debug.Log("[Match] " + member.name + " touched its own dropped " + flag.name + " -> it goes home");
                    flag.ReturnHome();
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Testing hook. A real capture always ends the match, so there is no way to watch the
    /// drop, return and timeout rules during normal play - SPACE puts the flag on the ground
    /// instead. Stripped out of a release build by the #if, which costs nothing at runtime.
    /// </summary>
    void HandleDebugDrop()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!allowDebugDrop) return;
        if (player == null) return;
        if (Keyboard.current == null) return;
        if (!Keyboard.current.spaceKey.wasPressedThisFrame) return;

        Flag carried = FlagCarriedBy(player);
        if (carried == null) return;

        // Fake a capture: leave the flag on the spot, then step the carrier off it.
        // The step matters. A drop always lands inside grab range of whoever was carrying it, so
        // without it the player would pick his own dropped flag straight back up and there would
        // be no way to watch the return and timeout rules at all. A real capture does the same
        // thing by teleporting the carrier to prison.
        Vector3 dropSpot = player.transform.position;

        CharacterMotor motor = player.GetComponent<CharacterMotor>();
        if (motor != null) motor.TeleportTo(dropSpot + new Vector3(0f, 0f, 4f));

        carried.Drop(dropSpot);

        Debug.Log("[Match] DEBUG: dropped " + carried.name + " at " + dropSpot.ToString("F2") +
                  " and stepped the player clear, so the return rules can be watched.");
#endif
    }

    // ------------------------------------------------------- time limit + elimination

    /// <summary>
    /// The optional time limit. On expiry the tie-break chain is: more free members, then the
    /// lower AVERAGE fieldTime (fresher team), then a draw. A draw is a real outcome rather than
    /// an arbitrary win - handing victory to a coin flip would be worse than admitting the match
    /// was even. Set `matchTimeSeconds` to 0 to switch the limit off entirely.
    /// </summary>
    void CheckMatchClock()
    {
        if (matchTimeSeconds <= 0f) return;
        if (MatchClock < matchTimeSeconds) return;

        int blueFree = TeamManager.FreeCount(Team.Blue);
        int redFree = TeamManager.FreeCount(Team.Red);

        if (blueFree != redFree)
        {
            Team byNumbers = blueFree > redFree ? Team.Blue : Team.Red;
            if (logRules) Debug.Log("[Match] TIME UP after " + MatchClock.ToString("F1") + " s -> " +
                                    byNumbers + " wins on free members (" + blueFree + " v " + redFree + ")");
            EndMatch(byNumbers, MatchOutcome.TimeLimit);
            return;
        }

        float blueAverage = AverageFieldTime(Team.Blue);
        float redAverage = AverageFieldTime(Team.Red);

        if (Mathf.Abs(blueAverage - redAverage) > tieEpsilon)
        {
            Team fresher = blueAverage < redAverage ? Team.Blue : Team.Red;
            if (logRules) Debug.Log("[Match] TIME UP after " + MatchClock.ToString("F1") + " s -> " +
                                    fresher + " wins on freshness (" + blueAverage.ToString("F2") +
                                    " v " + redAverage.ToString("F2") + ")");
            EndMatch(fresher, MatchOutcome.TimeLimit);
            return;
        }

        if (logRules) Debug.Log("[Match] TIME UP after " + MatchClock.ToString("F1") + " s -> DRAW (" +
                                blueFree + " free each, average fieldTime " +
                                blueAverage.ToString("F2") + " v " + redAverage.ToString("F2") + ")");
        EndDraw();
    }

    /// <summary>
    /// Every member of a team is in prison -> that team loses, immediately. There is nobody left
    /// who could rescue them, so waiting would only stall the match.
    ///
    /// This is why rescues are resolved at the TOP of the frame: by the time this runs, a rescue
    /// that was going to happen this frame has already happened, and the team is no longer
    /// all-imprisoned. Elimination only ever fires when it is genuinely unavoidable.
    /// </summary>
    void CheckElimination()
    {
        for (int t = 0; t < TeamUtil.All.Length; t++)
        {
            Team team = TeamUtil.All[t];
            if (!TeamManager.AllImprisoned(team)) continue;

            Team winner = team.Opponent();

            if (logRules)
            {
                Debug.Log("[Match] ELIMINATION: every member of " + team + " is imprisoned (" +
                          TeamManager.Count(team) + " of " + TeamManager.Count(team) +
                          ") -> " + winner + " wins.");
            }

            EndMatch(winner, MatchOutcome.Elimination);
            return;
        }
    }

    /// <summary>The mean fieldTime of a team, or 0 when it has no members. Used by the tie-break.</summary>
    float AverageFieldTime(Team team)
    {
        List<CharacterStatus> members = TeamManager.Members(team);
        if (members.Count == 0) return 0f;

        float total = 0f;
        for (int i = 0; i < members.Count; i++)
        {
            if (members[i] == null) continue;
            total += members[i].fieldTime;
        }

        return total / members.Count;
    }

    // ------------------------------------------------------------------ match end

    /// <summary>
    /// The one place a match is decided. Raises the generic team events, and the legacy
    /// player events that GameManager listens to (the human is always on Blue).
    /// </summary>
    void EndMatch(Team winner, MatchOutcome reason)
    {
        matchActive = false;
        TeamManager.MatchActive = false;
        outcome = reason;

        if (logRules)
        {
            Debug.Log("[Match] MATCH OVER: " + winner + " wins.  " + TeamCounts() +
                      "  clock=" + MatchClock.ToString("F1") + " s");
        }

        if (TeamWon != null) TeamWon(winner);
        if (TeamLost != null) TeamLost(winner.Opponent());

        if (winner == Team.Blue)
        {
            if (PlayerWon != null) PlayerWon();
        }
        else
        {
            if (PlayerLost != null) PlayerLost();
        }
    }

    /// <summary>Ends the match with no winner. Nothing but the time limit can reach this.</summary>
    void EndDraw()
    {
        matchActive = false;
        TeamManager.MatchActive = false;
        outcome = MatchOutcome.Draw;

        if (MatchDrawn != null) MatchDrawn();
    }

    // -------------------------------------------------------------- small helpers

    /// <summary>The flag owned by a team. Blue owns Flag_Blue, Red owns Flag_Red.</summary>
    public Flag FlagOf(Team owner)
    {
        return owner == Team.Blue ? blueFlag : redFlag;
    }

    /// <summary>The safe circle owned by a team.</summary>
    public BaseZone BaseOf(Team team)
    {
        return team == Team.Blue ? blueBase : redBase;
    }

    /// <summary>Where a member of this team is imprisoned (in the ENEMY half of the map).</summary>
    public Transform PrisonOf(Team team)
    {
        return team == Team.Blue ? prisonForBlue : prisonForRed;
    }

    /// <summary>
    /// Both teams' free / total counts in one string. Every capture, rescue and match end logs
    /// this, so a single log line is always enough to see the state of both teams at once.
    /// </summary>
    string TeamCounts()
    {
        return "Blue free=" + TeamManager.FreeCount(Team.Blue) + "/" + TeamManager.Count(Team.Blue) +
               "  Red free=" + TeamManager.FreeCount(Team.Red) + "/" + TeamManager.Count(Team.Red);
    }

    /// <summary>The flag this character is carrying, or null if it is carrying none.</summary>
    Flag FlagCarriedBy(CharacterStatus who)
    {
        if (who == null) return null;

        if (redFlag != null && redFlag.IsCarried && redFlag.carrier == who.transform) return redFlag;
        if (blueFlag != null && blueFlag.IsCarried && blueFlag.carrier == who.transform) return blueFlag;
        return null;
    }

    bool IsInAnyBase(Vector3 position)
    {
        if (blueBase != null && blueBase.Contains(position)) return true;
        if (redBase != null && redBase.Contains(position)) return true;
        return false;
    }

    /// <summary>
    /// A standing spot inside a prison, laid out as a 2 x 2 grid 0.7 m out from the centre in
    /// both directions.
    ///
    /// The old layout spread the four slots over 3.6 m along Z only, which burst the 3 x 3 m
    /// prison plate: the outer two prisoners stood outside their own cage. A 2 x 2 grid of 1.4 m
    /// spacing keeps every prisoner comfortably on the plate - the outermost body reaches
    /// 0.7 + 0.5 = 1.2 m from the centre, inside the plate's 1.5 m half-width.
    /// The prison plates have no colliders, so the ground height (y = 0) is the right height.
    /// </summary>
    Vector3 PrisonSpot(Transform prison, int slot)
    {
        if (prison == null) return Vector3.zero;

        int safeSlot = Mathf.Clamp(slot, 0, 3);

        // Slots 0/1 back row, 2/3 front row, alternating left and right within each row.
        float x = (safeSlot % 2 == 0) ? -0.7f : 0.7f;
        float z = (safeSlot < 2) ? -0.7f : 0.7f;

        return new Vector3(prison.position.x + x, 0f, prison.position.z + z);
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }
}
