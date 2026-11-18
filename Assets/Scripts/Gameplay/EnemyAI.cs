using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The TEAM-AGNOSTIC brain. The same script drives every AI in the match - the Red opponents
/// and the Blue team-mates alike - because it never assumes which team it is on. Everything it
/// needs is asked of CharacterStatus.team and the TeamManager registry, and it is switched on
/// and off by TeamManager.MatchActive rather than by anything Red-specific.
///
/// HOW IT DECIDES
/// --------------
/// Twice a second or so, every possible action is given a SCORE, and the highest score wins.
/// That replaces the old "check these situations in this order" cascade, and it is what lets a
/// job - a role from TeamManager - simply add weight to the actions that job cares about,
/// without any of them being forbidden outright.
///
/// One rule still outranks the scoring, because it is the rule of the game: out in the field
/// the character with the LOWER fieldTime wins a touch, and standing in your own base forces
/// fieldTime back to 0. So an AI may only chase when its own fieldTime is lower than its
/// target's - and that is scored as feasibility, not as a preference.
///
///   PATROL     - nothing worth doing: hold the guard post, which is also how the flag is
///                defended. Standing in the base keeps fieldTime at 0, so the guard is fresh.
///   CHASE      - the target is out of its base, we are fresher, close enough, and not winded.
///   RETREAT    - the target is safe in its base or fresher than us, or we have gone stale:
///                run home and reset fieldTime to 0.
///   INTERCEPT  - the target carries OUR flag and we could NOT win a touch against it, so
///                instead of feeding it a free capture we block the road to its base.
///   RETURNFLAG - our own flag is lying on the ground. Run at it and touch it to send it home.
///   RESCUE     - a team-mate is in the enemy prison and we are the nearest free one to it.
///   STEAL      - the ENEMY flag is there for the taking. Go and stand on it; MatchManager
///                judges the pick-up by distance, exactly like every other touch.
///   CARRY      - we are holding the enemy flag. Run it into our own base.
///   BAIT       - a team-mate is carrying the enemy flag. Stand in the road between the nearest
///                opponent and that carrier: the opponent has to come through us, and because
///                we are fresher we win the touch it starts.
///
/// STEAL and CARRY are why an AI can now win the match by itself: MatchManager picks the flag
/// up for whoever stands close enough and scores it for whoever carries it home, so a Raider
/// crossing the map is a real threat rather than decoration.
///
/// The decision is only re-made every decisionInterval (about a quarter of a second), never
/// every frame, which keeps the AI cheap on a 30 FPS Android build. Walking still happens
/// every frame so the movement stays smooth.
///
/// The AI is never told anything a player could not see for himself. It picks the nearest
/// opponent it can perceive, reads only public state, and every decision is delayed by a
/// reaction time - so it can always be predicted, and beaten.
/// </summary>
[RequireComponent(typeof(CharacterMotor))]
[RequireComponent(typeof(CharacterStatus))]
[RequireComponent(typeof(SprintStamina))]
public partial class EnemyAI : MonoBehaviour
{
    /// <summary>
    /// The nine things an AI character can be doing.
    ///
    /// The order matters: Unity stores a serialized enum as its number, so the three new states
    /// were APPENDED after the original six. Inserting them anywhere else would have silently
    /// changed the meaning of every already-saved state value in the scene and the prefab.
    /// </summary>
    public enum AiState
    {
        /// <summary>Guarding the home base. Safe, and fieldTime stays at 0.</summary>
        Patrol,

        /// <summary>Running at an opponent, trying to win a touch.</summary>
        Chase,

        /// <summary>Running home so fieldTime resets to 0 and we become safe again.</summary>
        Retreat,

        /// <summary>
        /// Cutting the flag carrier off. Used when an opponent has our flag but a touch would be
        /// lost, so bumping into it would only hand it a free capture.
        /// </summary>
        Intercept,

        /// <summary>Fetching our own flag back after it was dropped in the field.</summary>
        ReturnFlag,

        /// <summary>
        /// Running to the enemy prison to free an imprisoned team-mate. Only ONE free team-mate
        /// picks this, so a team never walks off the pitch to rescue a single prisoner.
        /// </summary>
        Rescue,

        /// <summary>Going for the enemy flag while it is takeable. The Raider's job.</summary>
        Steal,

        /// <summary>Holding the enemy flag and running it into our own base - the winning play.</summary>
        Carry,

        /// <summary>
        /// Escorting: standing between the nearest opponent and the team-mate who is carrying
        /// the enemy flag, so the opponent has to get through us first.
        /// </summary>
        Bait
    }

    [Header("Range")]
    [Tooltip("Start noticing an opponent within this distance, in metres, when the difficulty " +
             "preset is switched OFF. With the preset on, visionRadius below is used instead.")]
    public float detectRadius = 12f;

    [Tooltip("While already chasing, keep going until the target is this many times further away " +
             "than the notice range. The gap between the two distances stops the AI flickering.")]
    public float giveUpMultiplier = 1.4f;

    [Tooltip("How close to the guard post counts as 'home'.")]
    public float homeStopDistance = 0.5f;

    [Tooltip("Where this character guards, as an offset (x, z) from its home base centre. " +
             "Set per instance - the spawner hands each team-mate its own formation slot - so " +
             "two team-mates do not stand on exactly the same spot.")]
    public Vector2 guardOffset = Vector2.zero;

    [Header("Difficulty preset - Easy / Normal / Hard from Match Settings")]
    [Tooltip("Overwrite the tuning below with the preset the player chose on the Match Setup " +
             "screen. BOTH teams read the same preset, so a match stays a fair contest.")]
    public bool applyDifficultyPreset = true;

    [Tooltip("How far this AI can notice an opponent, in metres. Set from the preset.")]
    public float visionRadius = 12f;

    [Tooltip("How long this AI remembers where it last saw an opponent, in seconds. Only ever " +
             "extends a chase already under way - it never lets the AI start one from memory.")]
    public float memorySeconds = 3f;

    [Tooltip("0 = purely cautious, 1 = takes chances. Feeds the STEAL score.")]
    public float riskTolerance = 0.3f;

    [Tooltip("Multiplies how badly this AI wants the enemy flag. The main 'steal rate' lever.")]
    public float stealBias = 1f;

    [Tooltip("Random spread applied to every score before the winner is chosen. This is what " +
             "makes the AI unpredictable: two team-mates with the same orders can still pick " +
             "differently. Expressed as a fraction, so 0.15 = up to 15% either way.")]
    public float noise = 0.15f;

    [Header("Decision timing")]
    [Tooltip("Seconds between AI decisions. 0.2 - 0.3 is plenty and keeps the AI cheap.")]
    public float decisionInterval = 0.25f;

    [Tooltip("Random amount added to / taken off each decision interval, so the team-mates desync.")]
    public float decisionJitter = 0.05f;

    [Tooltip("A state change may not happen sooner than this after the previous one.")]
    public float minStateTime = 0.6f;

    [Header("Freshness rule - only fight when I would win")]
    [Tooltip("Safety margin in seconds. We only chase when our fieldTime is lower than the " +
             "target's by at least this much. 0.3 s is roughly 1.5% of the 20 s freshness scale.")]
    public float chaseFresherMargin = 0.3f;

    [Header("Stamina")]
    [Tooltip("Stamina (0..1) needed before a chase may START, so it never chases while winded.")]
    public float chaseEnterStamina01 = 0.5f;

    [Tooltip("If stamina drops below this while chasing, give up and walk home to recover.")]
    public float chaseExitStamina01 = 0.1f;

    [Tooltip("Only sprint while further away than this from the target, so it arrives with stamina in hand.")]
    public float sprintDistance = 7f;

    [Tooltip("Run flat out while retreating - getting home is what saves it.")]
    public bool sprintWhileRetreating = true;

    [Header("Flag duty")]
    [Tooltip("This team's OWN flag. Assigned per spawn by the spawner. The AI guards it, runs " +
             "down whoever steals it, and fetches it back if it is dropped.")]
    public Flag ourFlag;

    [Tooltip("The ENEMY flag - the only one this character is allowed to pick up. Assigned per " +
             "spawn by the spawner. STEAL walks to it; CARRY runs it home.")]
    public Flag enemyFlag;

    [Tooltip("How far ahead of the carrier we aim when intercepting, in metres.")]
    public float interceptLeadDistance = 4f;

    [Tooltip("When our flag is dropped, go for it unless an opponent is at least this many times " +
             "closer to it than we are. Otherwise we would lose the race and waste the trip.")]
    public float flagRaceAdvantage = 1.5f;

    [Tooltip("How close to our dropped flag counts as having reached it.")]
    public float returnStopDistance = 0.6f;

    [Tooltip("How close to the enemy flag counts as having reached it. Kept under MatchManager's " +
             "flagDistance, so standing here still leaves us genuinely inside pick-up range.")]
    public float stealStopDistance = 1f;

    [Tooltip("How far in front of the flag carrier we stand while escorting, in metres.")]
    public float baitStandOff = 3f;

    [Header("Rescue duty")]
    [Tooltip("How close to the prison we have to walk before MatchManager frees the prisoner. " +
             "Kept a little under MatchManager's rescueDistance, so stopping here still leaves " +
             "the rescuer genuinely inside rescue range.")]
    public float rescueStopDistance = 1.1f;

    [Header("Patrol")]
    [Tooltip("Seconds spent standing at a patrol point before picking a new one.")]
    public float patrolPauseSeconds = 2.5f;

    [Tooltip("How far from the guard post a patrol point may be picked.")]
    public float patrolRadius = 1.5f;

    [Header("Personality - rolled once per instance so team-mates differ")]
    [Tooltip("Roll random aggression and reaction time on startup.")]
    public bool randomizeOnStart = true;

    [Tooltip("Lowest aggression. Scales how far this character notices an opponent.")]
    public float aggressionMin = 0.85f;

    [Tooltip("Highest aggression. Scales how far this character notices an opponent.")]
    public float aggressionMax = 1.15f;

    [Tooltip("Shortest reaction pause, in seconds, when the preset is ON.")]
    public float reactionMin = 0.1f;

    [Tooltip("Longest reaction pause, in seconds, when the preset is ON.")]
    public float reactionMax = 0.35f;

    [Tooltip("Reaction time is rolled between 0 and this many seconds when the preset is OFF.")]
    public float reactionTimeRange = 0.5f;

    [Header("Utility scores - the weight of each action")]
    [Tooltip("Base score for holding the guard post.")]
    public float weightPatrol = 10f;

    [Tooltip("Added to PATROL while standing inside our own base, where fieldTime resets to 0.")]
    public float weightSafeAtHome = 25f;

    [Tooltip("Base score for running down an opponent we would beat.")]
    public float weightChase = 55f;

    [Tooltip("Score for going home to reset fieldTime, scaled by how stale we are.")]
    public float weightRetreat = 45f;

    [Tooltip("Added to RETREAT when an opponent that would beat us is closing in.")]
    public float weightDanger = 45f;

    [Tooltip("Score for blocking the opponent who is carrying our flag.")]
    public float weightIntercept = 60f;

    [Tooltip("Score for fetching our own dropped flag.")]
    public float weightReturnFlag = 65f;

    [Tooltip("Score for going to free an imprisoned team-mate.")]
    public float weightRescue = 70f;

    [Tooltip("Added to RESCUE for every team-mate already in prison. A full cage loses the match.")]
    public float weightPressure = 10f;

    [Tooltip("Base score for going after the enemy flag, before the Raider bonus and before " +
             "riskTolerance and stealBias scale it. This is the number the difficulty preset " +
             "moves the most, so it is the one to tune if the AI steals too little or too much.")]
    public float weightSteal = 45f;

    [Tooltip("Score for carrying the enemy flag home. Deliberately far above every other " +
             "action: once the flag is in our hands, nothing else matters.")]
    public float weightCarry = 200f;

    [Tooltip("Base score for escorting the team-mate who is carrying the enemy flag.")]
    public float weightBait = 30f;

    [Tooltip("How strongly a role pulls the actions it owns. Added for the job a character " +
             "cares about, subtracted for the ones it does not.")]
    public float roleBonus = 30f;

    [Header("Debug")]
    [Tooltip("Log every state change with both fieldTime values. Turn off for the final build.")]
    public bool logStateChanges;

    [Tooltip("Draw the notice radius, the guard-post walk and the line to the current target in " +
             "the Scene view while this character is selected.")]
    public bool drawSightGizmos = true;

    [Header("State - read only")]
    [Tooltip("What this character is doing right now.")]
    public AiState state = AiState.Patrol;

    [Tooltip("The job TeamManager has given this character. Read-only: roles are assigned there.")]
    public AiRole role = AiRole.Flex;

    [Tooltip("Rolled once at startup, it widens or narrows this character's notice range.")]
    public float aggression = 1f;

    [Tooltip("Rolled once at startup. A short pause after each decision makes it look alive.")]
    public float reactionTime;

    CharacterMotor motor;
    CharacterStatus status;
    CharacterStatus target;         // the opponent we are working against, visible or remembered
    CharacterStatus memoryTarget;   // the last opponent we actually had eyes on
    CharacterStatus rescueTarget;   // the imprisoned team-mate we have been told to free
    CharacterStatus chosenPrisoner; // set while scoring, so Rescue knows who it picked
    CharacterStatus mateFlagCarrier; // the team-mate carrying the enemy flag
    SprintStamina stamina;
    MapNavigator navigator;         // turns "walk at that point" into "walk around what is in the way"

    float decisionTimer;         // counts down to the next decision
    float stateTimer;            // how long we have been in the current state
    float reactionTimer;         // short freeze right after a state change
    float patrolTimer;           // how long we have been waiting at a patrol point
    Vector3 patrolTarget;        // where we are walking to while patrolling

    // --- facts gathered once per decision, so the scorers are pure reads ---
    float sightRange;            // how far this character notices opponents right now
    bool targetVisible;          // false when we are acting on a memory of where it went
    bool holdEnemyFlag;          // the enemy flag is in our hands
    bool iWouldLoseToNearestFoe; // a touch against the target would be lost

    // --- memory of where an opponent was last seen ---
    Vector3 lastSeenPoint;
    float lastSeenFieldTime;
    float memoryTimer;

    /// <summary>Shared stamina as 0..1. 0 when the component is missing, which means "no sprinting".</summary>
    float Stamina01
    {
        get { return stamina != null ? stamina.Stamina01 : 0f; }
    }

    /// <summary>How stale this character is, 0 = just reset, 1 = as stale as the HUD can show.</summary>
    float Staleness01()
    {
        return Mathf.Clamp01(status.fieldTime / CharacterStatus.MaxFieldTimeSeconds);
    }

    /// <summary>True while there is an opponent to work against, seen or remembered.</summary>
    bool FoeKnown
    {
        get { return target != null && !target.isCaptured; }
    }

    /// <summary>
    /// Where the opponent is: its live position while we can see it, and the spot we last saw
    /// it otherwise. The AI is never allowed to read a position it could not have watched.
    /// </summary>
    Vector3 FoeAimPoint
    {
        get { return targetVisible ? target.transform.position : lastSeenPoint; }
    }

    /// <summary>
    /// The opponent's fieldTime: live while it is in sight, and the value we read when we last
    /// had eyes on it otherwise. Reading a hidden character's live timer would be cheating.
    /// </summary>
    float FoeFieldTime
    {
        get { return targetVisible ? target.fieldTime : lastSeenFieldTime; }
    }

    /// <summary>Flat distance to the opponent, live or remembered.</summary>
    float FoeDistance
    {
        get { return FlatDistance(transform.position, FoeAimPoint); }
    }

    /// <summary>True only when we can actually SEE the opponent standing in its own base.</summary>
    bool FoeInHomeBase
    {
        get { return targetVisible && target.IsInHomeBase; }
    }

    /// <summary>
    /// True when we would WIN a touch against our target: our fieldTime is lower, with a
    /// small safety margin so a coin-flip is never taken.
    /// </summary>
    bool IAmFresher
    {
        get
        {
            if (!FoeKnown) return false;
            return (status.fieldTime + chaseFresherMargin) < FoeFieldTime;
        }
    }

    /// <summary>True when our target is the one carrying OUR flag.</summary>
    bool TargetHasOurFlag
    {
        get
        {
            return ourFlag != null && ourFlag.IsCarried &&
                   target != null && ourFlag.carrier == target.transform;
        }
    }

    void Awake()
    {
        motor = GetComponent<CharacterMotor>();
        status = GetComponent<CharacterStatus>();
        stamina = GetComponent<SprintStamina>();

        // Navigation is found (or added) here rather than required on the prefab, so no character
        // can ever exist without it. With no NavMesh baked it degrades to the old straight-line
        // walking instead of failing outright.
        navigator = GetComponent<MapNavigator>();
        if (navigator == null) navigator = gameObject.AddComponent<MapNavigator>();

        // The preset FIRST, so that everything below - including the aggression and reaction
        // time rolled right after it, and the stagger the spawner applies next - is already
        // running on the numbers the player chose on the Match Setup screen.
        if (applyDifficultyPreset) ApplyDifficulty();

        if (randomizeOnStart)
        {
            // Team-mates with different numbers never move in lockstep.
            aggression = Random.Range(aggressionMin, aggressionMax);

            reactionTime = applyDifficultyPreset
                ? Random.Range(reactionMin, reactionMax)
                : Random.Range(0f, reactionTimeRange);
        }

        patrolTarget = GuardPost();
    }

    /// <summary>
    /// Copies the Easy / Normal / Hard preset into the live tuning. Only the SPEED and BOLDNESS
    /// of the AI changes - never the rules, and never one team more than the other, because
    /// both teams read the same profile. That is what makes a difficulty setting fair.
    /// </summary>
    void ApplyDifficulty()
    {
        DifficultyProfile p = MatchSettings.Profile;

        decisionInterval = p.decisionInterval;      // reaction speed
        reactionMin = p.reactionMin;
        reactionMax = p.reactionMax;
        chaseFresherMargin = p.chaseFresherMargin;  // how close a call it will take

        aggressionMin = p.aggressionMin;            // notice range
        aggressionMax = p.aggressionMax;
        visionRadius = p.visionRadius;
        memorySeconds = p.memorySeconds;

        riskTolerance = p.riskTolerance;            // boldness
        stealBias = p.stealBias;
        noise = p.noise;
    }

    void Update()
    {
        // Nothing to think about while the match is not live: the Title, Match Setup, Ready,
        // Paused and end screens all leave this character standing exactly where it is.
        if (!TeamManager.MatchActive)
        {
            SetSprint(false);
            motor.Move(Vector2.zero);
            return;
        }

        // A captured character is frozen in prison: no movement, no sprinting, no decisions.
        if (status.isCaptured)
        {
            SetSprint(false);
            motor.Move(Vector2.zero);
            return;
        }

        stateTimer += Time.deltaTime;
        decisionTimer -= Time.deltaTime;
        if (reactionTimer > 0f) reactionTimer -= Time.deltaTime;
        if (memoryTimer > 0f) memoryTimer -= Time.deltaTime;

        bool targetAvailable = (target != null && !target.isCaptured);

        // --- DECIDE: a few times a second, never every frame ---
        // Target selection happens here too, so the (at most four) distance checks are also
        // throttled to a few times a second instead of running every frame.
        if (decisionTimer <= 0f)
        {
            decisionTimer = NextDecisionDelay();
            target = PickTarget();
            targetAvailable = (target != null && !target.isCaptured);
            Decide();
        }

        // --- ACT: every frame, so the walking looks smooth ---
        Act(targetAvailable);
    }

    /// <summary>
    /// Pushes this brain's first decision out by a fixed delay. Called once by the spawner with
    /// index / count of one decision interval, so eight AI never all think on the same frame.
    /// Only ever delays the first decision - it never pulls one forward.
    /// </summary>
    public void StaggerFirstDecision(float seconds)
    {
        if (seconds > decisionTimer) decisionTimer = seconds;
    }

    // ------------------------------------------------------------------ deciding

    /// <summary>The team-mate carrying the enemy flag, if it is not us and not an opponent.</summary>
    CharacterStatus FindMateCarryingEnemyFlag()
    {
        if (enemyFlag == null || !enemyFlag.IsCarried || enemyFlag.carrier == null) return null;
        if (enemyFlag.carrier == transform) return null;

        CharacterStatus carrier = enemyFlag.carrier.GetComponent<CharacterStatus>();
        if (carrier == null || carrier.isCaptured) return null;
        if (carrier.team != status.team) return null;

        return carrier;
    }

    /// <summary>Chooses the state this character should be in. Only runs on the decision timer.</summary>
    void Decide()
    {
        RefreshFacts();

        AiState next = BestAction();

        // Retreating from inside our own base is pointless - we are already safe there.
        if (next == AiState.Retreat && status.IsInHomeBase) next = AiState.Patrol;

        // Hysteresis: never change state twice in a hurry. This is what stops flip-flopping.
        if (next != state && stateTimer >= minStateTime) SetState(next);
    }

    /// <summary>
    /// Scores every action and returns the best one.
    ///
    /// The order the actions are considered in is only a tie-break: when two actions score
    /// exactly the same - which the noise makes rare - the earlier one in this list wins. The
    /// list is ordered the way the old state machine ranked things, so ties resolve to the same
    /// behaviour the prototype already had.
    /// </summary>
    AiState BestAction()
    {
        float bestScore = float.NegativeInfinity;
        AiState best = AiState.Patrol;

        Consider(AiState.Carry, ScoreCarry(), ref bestScore, ref best);
        Consider(AiState.Retreat, ScoreRetreat(), ref bestScore, ref best);
        Consider(AiState.Rescue, ScoreRescue(), ref bestScore, ref best);
        Consider(AiState.Intercept, ScoreIntercept(), ref bestScore, ref best);
        Consider(AiState.ReturnFlag, ScoreReturnFlag(), ref bestScore, ref best);
        Consider(AiState.Chase, ScoreChase(), ref bestScore, ref best);
        Consider(AiState.Steal, ScoreSteal(), ref bestScore, ref best);
        Consider(AiState.Bait, ScoreBait(), ref bestScore, ref best);
        Consider(AiState.Patrol, ScorePatrol(), ref bestScore, ref best);

        return best;
    }

    void SetState(AiState next)
    {
        if (logStateChanges)
        {
            Debug.Log("[AI] " + name + " (" + status.team + ", " + TeamManager.RoleName(role) + "): " +
                      state + " -> " + next +
                      "   myFT=" + status.fieldTime.ToString("F2") +
                      " foeFT=" + (FoeKnown ? FoeFieldTime.ToString("F2") : "n/a") +
                      " stamina=" + Stamina01.ToString("F2") +
                      " inBase=" + status.IsInHomeBase);
        }

        state = next;
        stateTimer = 0f;
        reactionTimer = reactionTime;   // a short freeze makes the change readable and desyncs team-mates

        if (next == AiState.Patrol)
        {
            patrolTimer = 0f;
            patrolTarget = GuardPost();
        }

        // A rescue only knows which prisoner to run at if the scorer that chose it said so.
        if (next == AiState.Rescue) rescueTarget = chosenPrisoner;
    }

    /// <summary>How long to wait before making the next decision, with a little randomness.</summary>
    float NextDecisionDelay()
    {
        float jitter = randomizeOnStart ? Random.Range(-decisionJitter, decisionJitter) : 0f;
        return Mathf.Max(0.05f, decisionInterval + jitter);
    }

    // ------------------------------------------------------------ patrol points

    /// <summary>The spot this character guards: its home base centre plus its own offset.</summary>
    Vector3 GuardPost()
    {
        if (status == null) status = GetComponent<CharacterStatus>();
        if (status == null || status.homeBase == null) return transform.position;

        Vector3 centre = status.homeBase.transform.position;
        return new Vector3(centre.x + guardOffset.x, 0f, centre.z + guardOffset.y);
    }

    /// <summary>
    /// A random spot near the guard post, CLAMPED so it always stays inside the base circle.
    /// Without that clamp an idle character could park just outside its base, start collecting
    /// fieldTime, and become taggable for free while doing nothing.
    /// </summary>
    Vector3 NextPatrolPoint()
    {
        Vector3 post = GuardPost();
        if (status.homeBase == null) return post;

        Vector2 scatter = Random.insideUnitCircle * patrolRadius;
        Vector2 candidate = new Vector2(post.x + scatter.x, post.z + scatter.y);

        Vector3 centre3 = status.homeBase.transform.position;
        Vector2 centre = new Vector2(centre3.x, centre3.z);
        float safeRadius = Mathf.Max(0f, status.homeBase.radius - 0.5f);

        Vector2 clamped = centre + Vector2.ClampMagnitude(candidate - centre, safeRadius);
        return new Vector3(clamped.x, 0f, clamped.y);
    }

    // ------------------------------------------------------------------- ranges

    /// <summary>
    /// How far this character notices an opponent. The preset supplies the base radius and the
    /// rolled aggression scales it, so two Easy brains differ from each other and both differ
    /// clearly from two Hard ones. While already committed to target-chasing the range is
    /// widened, which gives the pursuit a natural amount of stickiness.
    /// </summary>
    float noticeRange
    {
        get
        {
            float range = (applyDifficultyPreset ? visionRadius : detectRadius) * aggression;

            if (state == AiState.Chase || state == AiState.Intercept) range *= giveUpMultiplier;

            return range;
        }
    }

    /// <summary>Is the opponent inside the distance we are willing to run at it from?</summary>
    bool TargetInChaseRange()
    {
        return FoeDistance <= noticeRange;
    }

    // -------------------------------------------------------------------- gizmos

    /// <summary>
    /// Draws the notice radius, the walk to the guard post and the line to the current target.
    /// Editor only - Gizmos cost nothing in a build. Select a character to see its own.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (!drawSightGizmos) return;

        CharacterStatus self = status != null ? status : GetComponent<CharacterStatus>();
        Color tint = self != null ? self.team.ToColor() : Color.white;

        // The notice radius: only this far, and only characters this side of it, are perceivable.
        Gizmos.color = tint;
        Gizmos.DrawWireSphere(transform.position, Application.isPlaying ? sightRange : detectRadius * aggression);

        // Where the last decision sent us, and the guard post we patrol around.
        Vector3 post = GuardPost();
        Gizmos.color = new Color(1f, 0.86f, 0.32f);
        Gizmos.DrawLine(transform.position, post);
        Gizmos.DrawWireSphere(post, 0.35f);

        // The character we are working against - solid while we can see it, dim once we are
        // running only on our memory of where it went.
        if (target != null)
        {
            Gizmos.color = targetVisible ? Color.red : new Color(1f, 0.4f, 0.4f, 0.4f);
            Gizmos.DrawLine(transform.position, FoeAimPoint);
            Gizmos.DrawWireSphere(FoeAimPoint, 0.4f);
        }

        // The imprisoned team-mate we were sent for.
        if (rescueTarget != null && rescueTarget.isCaptured)
        {
            Gizmos.color = new Color(0.7f, 1f, 0.7f);
            Gizmos.DrawLine(transform.position, rescueTarget.transform.position);
        }
    }

}
