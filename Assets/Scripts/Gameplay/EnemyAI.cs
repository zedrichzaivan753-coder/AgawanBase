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
public class EnemyAI : MonoBehaviour
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

    // ---------------------------------------------------------------- target finding

    /// <summary>
    /// The nearest free member of the opposing team, if it is close enough to notice.
    ///
    /// Team-agnostic by construction: it asks TeamManager who the opponents are instead of
    /// assuming anything about who is the player. At most four entries, plain index loop, no
    /// allocation.
    ///
    /// Seeing an opponent refreshes the memory of where it was. When nothing is in sight but
    /// that memory is still alive, the last opponent we saw is kept as the target - so a chase
    /// survives a target that briefly runs out of range instead of being dropped instantly.
    /// The memory can only ever CONTINUE something already under way; no action may be started
    /// from it, which is what keeps the AI from inventing an opponent it cannot see.
    ///
    /// Sets targetVisible as it goes, because the answer is already known here and the scorers
    /// must not have to recompute it.
    /// </summary>
    CharacterStatus PickTarget()
    {
        List<CharacterStatus> foes = TeamManager.Members(status.team.Opponent());

        CharacterStatus nearest = null;
        float nearestSqr = float.MaxValue;

        for (int i = 0; i < foes.Count; i++)
        {
            CharacterStatus foe = foes[i];
            if (foe == null || foe == status || foe.isCaptured) continue;

            Vector3 p = foe.transform.position;
            float dx = p.x - transform.position.x;
            float dz = p.z - transform.position.z;
            float sqr = dx * dx + dz * dz;

            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = foe;
            }
        }

        float sight = noticeRange;
        targetVisible = nearest != null && nearestSqr <= sight * sight;

        if (targetVisible)
        {
            // Eyes on it: this is the freshest, most honest reading we will get.
            memoryTarget = nearest;
            lastSeenPoint = nearest.transform.position;
            lastSeenFieldTime = nearest.fieldTime;
            memoryTimer = memorySeconds;
            return nearest;
        }

        // Nothing in sight. Walk on where we last saw it, for as long as we can plausibly
        // still remember - but never read its live state while doing so.
        if (memoryTimer > 0f && memoryTarget != null && !memoryTarget.isCaptured) return memoryTarget;

        memoryTarget = nearest;
        return null;
    }

    // ------------------------------------------------------------------ deciding

    /// <summary>
    /// Gathers everything the scorers need, once. Doing it here rather than inside each scorer
    /// means the same fact is never worked out five times in one decision.
    /// </summary>
    void RefreshFacts()
    {
        role = TeamManager.RoleOf(status);
        sightRange = noticeRange;

        holdEnemyFlag = enemyFlag != null && enemyFlag.IsCarried && enemyFlag.carrier == transform;
        mateFlagCarrier = FindMateCarryingEnemyFlag();
        iWouldLoseToNearestFoe = FoeKnown && !IAmFresher;

        chosenPrisoner = null;
    }

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

    /// <summary>
    /// Enters one action into the running. A score of 0 or less means the action is not
    /// possible right now, so it never receives a role bonus: a job must not be able to unlock
    /// something the rules of the game forbid.
    /// </summary>
    void Consider(AiState action, float score, ref float bestScore, ref AiState best)
    {
        if (score <= 0f) return;

        score += RoleBonus(action);
        if (noise > 0f) score *= 1f + Random.Range(-noise, noise);

        if (score > bestScore)
        {
            bestScore = score;
            best = action;
        }
    }

    /// <summary>
    /// How much this character's job pulls it towards an action, and away from the ones another
    /// job owns. This is the whole of "role assignment" as far as the brain is concerned: a
    /// preference, never a permission, so a Defender with nobody left at home still goes for
    /// the prisoner.
    /// </summary>
    float RoleBonus(AiState action)
    {
        if (role == AiRole.Defender)
        {
            if (action == AiState.Patrol) return roleBonus;
            if (action == AiState.ReturnFlag) return roleBonus * 0.65f;
            if (action == AiState.Intercept) return roleBonus * 0.5f;
            if (action == AiState.Chase) return -roleBonus * 0.5f;
            if (action == AiState.Steal) return -roleBonus * 0.8f;
            if (action == AiState.Bait) return -roleBonus * 0.3f;
            return 0f;
        }

        if (role == AiRole.Raider)
        {
            if (action == AiState.Steal) return roleBonus * 0.5f;
            if (action == AiState.Bait) return roleBonus * 0.5f;
            if (action == AiState.Chase) return roleBonus * 0.3f;
            if (action == AiState.Patrol) return -roleBonus * 0.5f;
            if (action == AiState.ReturnFlag) return -roleBonus * 0.2f;
            return 0f;
        }

        if (role == AiRole.Rescuer)
        {
            if (action == AiState.Rescue) return roleBonus * 1.15f;
            if (action == AiState.ReturnFlag) return roleBonus * 0.3f;
            if (action == AiState.Steal) return -roleBonus * 0.5f;
            if (action == AiState.Bait) return -roleBonus * 0.2f;
            return 0f;
        }

        return 0f;   // FLEX: no preferences at all, so it simply follows what is most urgent
    }

    // ------------------------------------------------------------------- scoring
    //
    // Every scorer returns 0 when its action is impossible, so Consider() can skip the role
    // bonus for it. Nothing here moves the character or changes any state.

    /// <summary>
    /// Carrying the enemy flag home. Scored far above everything else on purpose: this is the
    /// action that wins the match, and it should never lose to a passing scuffle.
    /// </summary>
    float ScoreCarry()
    {
        if (!holdEnemyFlag) return 0f;
        return weightCarry;
    }

    /// <summary>
    /// Going after the enemy flag.
    ///
    /// This is the score the difficulty preset moves most. `stealBias` scales it straight up or
    /// down, and `riskTolerance` decides how much of it survives once the AI has been out in
    /// the field long enough to be worth tagging - an Easy brain gives up on the trip almost at
    /// once, a Hard one keeps going.
    /// </summary>
    float ScoreSteal()
    {
        if (enemyFlag == null || enemyFlag.IsCarried) return 0f;

        // nerve runs from -1 (stale and cautious) to +1 (fresh and bold).
        float nerve = riskTolerance + (1f - Staleness01()) - 1f;

        float score = weightSteal * stealBias * Mathf.Clamp01(0.35f + nerve);

        // A dropped enemy flag is a gift: nobody is defending it, and the trip is far shorter.
        if (enemyFlag.IsDropped) score += weightReturnFlag;

        return score;
    }

    /// <summary>
    /// Escorting the team-mate who has the enemy flag.
    ///
    /// Only a character that would WIN the touch may do this. Standing in an opponent's road
    /// with a losing fieldTime is not a decoy, it is a free capture - and being captured is one
    /// of the two ways this team loses the match.
    /// </summary>
    float ScoreBait()
    {
        if (mateFlagCarrier == null) return 0f;
        if (!FoeKnown) return 0f;
        if (!IAmFresher) return 0f;
        return weightBait * stealBias;
    }

    /// <summary>
    /// Going to free an imprisoned team-mate. Exactly one free team-mate ever picks this, and
    /// the score climbs with every additional prisoner, because a full cage is how a team loses.
    /// </summary>
    float ScoreRescue()
    {
        if (status.isCaptured) return 0f;

        CharacterStatus prisoner = TeamManager.NearestCapturedTeammate(status.team, transform.position);
        if (prisoner == null) return 0f;
        if (prisoner.rescueCooldownTimer > 0f) return 0f;   // just freed: no chain-rescue

        if (!IAmTheChosenRescuer(prisoner)) return 0f;

        chosenPrisoner = prisoner;
        return weightRescue + TeamManager.CapturedCount(status.team) * weightPressure;
    }

    /// <summary>
    /// Are we the one who goes?
    ///
    /// Every free team-mate can see the same prisoner, so without this rule a whole team would
    /// abandon the field to rescue one character. The closest free team-mate is the one who
    /// goes; ties go to the lower spawnIndex, which is a fixed per-character number, so two
    /// brains can never both decide they are the chosen one.
    /// </summary>
    bool IAmTheChosenRescuer(CharacterStatus prisoner)
    {
        float myDistance = FlatDistance(transform.position, prisoner.transform.position);
        List<CharacterStatus> mates = TeamManager.Members(status.team);

        for (int i = 0; i < mates.Count; i++)
        {
            CharacterStatus mate = mates[i];
            if (mate == null || mate == status || mate.isCaptured) continue;

            float theirDistance = FlatDistance(mate.transform.position, prisoner.transform.position);
            if (theirDistance < myDistance) return false;

            if (Mathf.Abs(theirDistance - myDistance) < 0.01f && mate.spawnIndex < status.spawnIndex)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Should we break off and go and fetch our own dropped flag?
    ///
    /// Deliberately honest: the only thing compared is our own distance against the one
    /// opponent we can perceive, which is something this character could genuinely see from
    /// where it stands. It never peeks at where that opponent is heading, so a player can
    /// always predict the race just by looking at the field.
    /// </summary>
    bool ShouldReturnFlag()
    {
        if (ourFlag == null || !ourFlag.IsDropped) return false;

        float myDistance = FlatDistance(transform.position, ourFlag.transform.position);
        if (myDistance <= returnStopDistance) return true;      // already standing on it

        if (FoeKnown)
        {
            float foeDistance = FlatDistance(FoeAimPoint, ourFlag.transform.position);

            // If it is clearly closer it would beat us there and re-grab it in front of us.
            if (myDistance > foeDistance * flagRaceAdvantage) return false;
        }

        return true;
    }

    /// <summary>Fetching our own dropped flag.</summary>
    float ScoreReturnFlag()
    {
        if (!ShouldReturnFlag()) return 0f;
        return weightReturnFlag;
    }

    /// <summary>
    /// Blocking the opponent who is carrying our flag. Only scored when a touch would be LOST -
    /// when it would be won, Chase does the job and scores higher.
    /// </summary>
    float ScoreIntercept()
    {
        if (!FoeKnown) return 0f;
        if (!TargetHasOurFlag) return 0f;
        if (FoeInHomeBase) return 0f;
        if (IAmFresher) return 0f;

        // An intercept already under way survives the carrier briefly leaving sight; a new one
        // is never started on a memory.
        if (!targetVisible && state != AiState.Intercept) return 0f;

        return weightIntercept;
    }

    /// <summary>
    /// Running down an opponent. This is where "only ever pick a fight you would win" lives:
    /// a chase is not merely discouraged when we are staler, it is impossible.
    /// </summary>
    float ScoreChase()
    {
        if (!FoeKnown) return 0f;
        if (FoeInHomeBase) return 0f;          // fieldTime is 0 in there: untouchable
        if (!IAmFresher) return 0f;            // the one rule of the game
        if (stamina == null || !stamina.CanSprint) return 0f;

        // Starting a chase needs more stamina than staying in one, which is the same hysteresis
        // the original state machine expressed with chaseEnterStamina01 / chaseExitStamina01.
        float needed = (state == AiState.Chase) ? chaseExitStamina01 : chaseEnterStamina01;
        if (Stamina01 < needed) return 0f;
        if (!TargetInChaseRange()) return 0f;

        // A chase already running keeps going on memory; a new one needs eyes on the target.
        if (!targetVisible && state != AiState.Chase) return 0f;

        float lead = Mathf.Clamp01((FoeFieldTime - status.fieldTime) / CharacterStatus.MaxFieldTimeSeconds);
        float closeness = 1f - Mathf.Clamp01(FoeDistance / Mathf.Max(0.01f, sightRange));

        return weightChase + lead * 30f + closeness * 15f;
    }

    /// <summary>
    /// Holding the guard post. Always possible, so this is the score everything else has to
    /// beat - and it is worth the most while standing inside the base, where fieldTime resets
    /// to 0 and the character is genuinely safe.
    /// </summary>
    float ScorePatrol()
    {
        if (status.IsInHomeBase) return weightPatrol + weightSafeAtHome;

        float score = weightPatrol;

        // Out in the field with our flag still on its pole, the useful place to be is near it.
        if (ourFlag != null && !ourFlag.IsCarried)
        {
            float distance = FlatDistance(transform.position, ourFlag.transform.position);
            score += (1f - Mathf.Clamp01(distance / Mathf.Max(0.01f, sightRange))) * 10f;
        }

        return score;
    }

    /// <summary>
    /// Going home to reset fieldTime to 0. Scaled by how stale this character is, so a fresh
    /// one has no reason to hurry back, and topped up when a foe that would beat us is close
    /// enough to act on it.
    /// </summary>
    float ScoreRetreat()
    {
        if (status.IsInHomeBase) return 0f;    // already safe: going home achieves nothing

        float score = weightRetreat * Staleness01();

        if (iWouldLoseToNearestFoe)
        {
            float closeness = 1f - Mathf.Clamp01(FoeDistance / Mathf.Max(0.01f, sightRange));
            score += weightDanger * closeness;
        }

        return score;
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

    // ------------------------------------------------------------------- acting

    void Act(bool targetAvailable)
    {
        // Just changed our mind? Stand still for the reaction time before moving again.
        if (reactionTimer > 0f)
        {
            Hold();
            return;
        }

        if (state == AiState.Carry) Carry();
        else if (state == AiState.Steal) Steal();
        else if (state == AiState.Chase && targetAvailable) Chase();
        else if (state == AiState.Intercept && targetAvailable) Intercept();
        else if (state == AiState.ReturnFlag) FetchFlag();
        else if (state == AiState.Rescue) GoRescue();
        else if (state == AiState.Bait) Bait();
        else if (state == AiState.Retreat) Retreat();
        else Patrol();
    }

    /// <summary>Stand still, and stop claiming to be sprinting.</summary>
    void Hold()
    {
        motor.Move(Vector2.zero);
        SetSprint(false);
    }

    /// <summary>
    /// Run the enemy flag home. This only does the walking - MatchManager scores the flag when
    /// the carrier is inside its own base, so there is still exactly one authority for a win.
    /// </summary>
    void Carry()
    {
        if (status.homeBase == null)
        {
            Hold();
            return;
        }

        Vector3 home = FlatPoint(status.homeBase.transform.position);

        if (FlatDistance(transform.position, home) <= homeStopDistance)
        {
            Hold();
            return;   // in the base: MatchManager ends the match from here
        }

        motor.Move(FlatDirection(transform.position, home));

        // Every second spent carrying is a second an opponent has to catch us, and the flag
        // already makes us slower - so this is the one run that always sprints.
        SetSprint(true);
    }

    /// <summary>
    /// Go and stand on the enemy flag. This only does the walking - MatchManager judges the
    /// pick-up by distance, exactly like every other touch in the game.
    /// </summary>
    void Steal()
    {
        if (enemyFlag == null || enemyFlag.IsCarried)
        {
            Hold();
            return;
        }

        Vector3 flagPosition = enemyFlag.transform.position;

        // stealStopDistance is deliberately INSIDE MatchManager's flagDistance, so standing here
        // still leaves us genuinely in pick-up range rather than one step short of it.
        if (FlatDistance(transform.position, flagPosition) <= stealStopDistance)
        {
            Hold();
            return;   // in range, MatchManager takes the flag from here
        }

        motor.Move(FlatDirection(transform.position, flagPosition));
        SetSprint(FlatDistance(transform.position, flagPosition) >= sprintDistance);
    }

    /// <summary>
    /// Stand between the nearest opponent and the team-mate carrying the enemy flag.
    ///
    /// The opponent's brain picks the nearest free opponent it can perceive, so a fresh
    /// character parked on that line becomes the thing it comes for - which is the entire point
    /// of the escort, and costs the carrier the escort's worth of head start.
    /// </summary>
    void Bait()
    {
        if (mateFlagCarrier == null || !FoeKnown)
        {
            Hold();
            return;
        }

        Vector3 carrier = FlatPoint(mateFlagCarrier.transform.position);
        Vector2 towards = FlatDirection(mateFlagCarrier.transform.position, FoeAimPoint);
        Vector3 aimPoint = carrier + new Vector3(towards.x, 0f, towards.y) * baitStandOff;

        if (FlatDistance(transform.position, aimPoint) <= homeStopDistance)
        {
            Hold();
            return;
        }

        motor.Move(FlatDirection(transform.position, aimPoint));
        SetSprint(FoeDistance >= sprintDistance);
    }

    /// <summary>
    /// Block the carrier without touching it. A touch would be LOST, so actually bumping into
    /// it hands it a free capture and could end the match in its favour. Instead we run to a
    /// spot on the line between it and its base and stand in the way, which costs it time and
    /// lets a fresher team-mate - or its own empty stamina bar - catch up with it.
    /// </summary>
    void Intercept()
    {
        if (!FoeKnown)
        {
            Hold();
            return;
        }

        Vector3 carrierPosition = FoeAimPoint;

        // Where it is heading: its OWN base. If we cannot read that for some reason, just chase.
        Vector3 itsGoal = target.homeBase != null ? target.homeBase.transform.position : carrierPosition;

        Vector2 towardsItsGoal = FlatDirection(carrierPosition, itsGoal);
        Vector2 aim = Flat(carrierPosition) + towardsItsGoal * interceptLeadDistance;
        Vector3 aimPoint = new Vector3(aim.x, 0f, aim.y);

        // Never step INSIDE its base to intercept. Inside it its fieldTime is 0 and ours is not,
        // so we would lose the touch and be captured for free. Guarding the doorway is just as
        // useful and does not donate a prisoner.
        if (target.homeBase != null && target.homeBase.Contains(aimPoint))
        {
            Vector3 centre = target.homeBase.transform.position;
            Vector2 fromCentre = new Vector2(aimPoint.x - centre.x, aimPoint.z - centre.z);
            if (fromCentre.sqrMagnitude < 0.0001f) fromCentre = new Vector2(1f, 0f);

            float standOff = target.homeBase.radius + 0.5f;
            Vector2 edge = new Vector2(centre.x, centre.z) + fromCentre.normalized * standOff;
            aimPoint = new Vector3(edge.x, 0f, edge.y);
        }

        // Already standing where we want to be: hold position instead of jittering on the spot.
        if (FlatDistance(transform.position, aimPoint) <= homeStopDistance)
        {
            Hold();
            return;
        }

        motor.Move(FlatDirection(transform.position, aimPoint));

        // Sprint while there is still ground to make up, so we actually arrive first.
        SetSprint(FlatDistance(transform.position, carrierPosition) >= sprintDistance);
    }

    /// <summary>
    /// Run at the imprisoned team-mate and stand next to it. This only does the walking -
    /// MatchManager judges the rescue by distance, exactly like the tag and flag rules, so there
    /// is still exactly one authority for what a rescue IS.
    /// </summary>
    void GoRescue()
    {
        if (rescueTarget == null || !rescueTarget.isCaptured)
        {
            Hold();
            return;
        }

        Vector3 prisoner = rescueTarget.transform.position;

        // rescueStopDistance is deliberately INSIDE MatchManager's rescueDistance, so stopping
        // here still leaves us genuinely in rescue range instead of one step short of it.
        if (FlatDistance(transform.position, prisoner) <= rescueStopDistance)
        {
            Hold();
            return;   // in range, MatchManager frees them from here
        }

        motor.Move(FlatDirection(transform.position, prisoner));

        // Sprint the approach: the prison is deep in enemy ground and every second there is a
        // second a fresh opponent can tag us. The walk home afterwards does not sprint, so we
        // have stamina in hand if we have to run for our life.
        SetSprint(FlatDistance(transform.position, prisoner) >= sprintDistance);
    }

    /// <summary>
    /// Run at our own dropped flag and touch it. This only does the walking - MatchManager
    /// judges the touch and sends the flag home, so there is still exactly one authority.
    /// </summary>
    void FetchFlag()
    {
        if (ourFlag == null)
        {
            Hold();
            return;
        }

        Vector3 flagPosition = ourFlag.transform.position;

        if (FlatDistance(transform.position, flagPosition) <= returnStopDistance)
        {
            Hold();
            return;   // standing on it, MatchManager returns it from here
        }

        motor.Move(FlatDirection(transform.position, flagPosition));

        // A dropped flag is a deadline, not a stroll: sprint while we still have stamina, and
        // SprintStamina quietly stops us once we are winded.
        SetSprint(true);
    }

    /// <summary>
    /// Walk at the opponent, sprinting in bursts while still far away. While running on a
    /// memory there is nothing to touch at the far end, so it walks to the spot and waits for
    /// the next decision to bring fresh news.
    /// </summary>
    void Chase()
    {
        if (!FoeKnown)
        {
            Hold();
            return;
        }

        Vector3 aimPoint = FoeAimPoint;
        float distance = FlatDistance(transform.position, aimPoint);

        motor.Move(FlatDirection(transform.position, aimPoint));

        // Sprint only while far away. Once stamina runs out the shared lock-out in
        // SprintStamina stops it by itself, which turns this into short bursts with no
        // extra code here at all.
        SetSprint(targetVisible && distance >= sprintDistance);
    }

    /// <summary>
    /// Run home. The centre, not the guard post: getting inside the circle is what resets
    /// fieldTime to 0 and makes this character safe again.
    /// </summary>
    void Retreat()
    {
        if (status.homeBase == null)
        {
            Hold();
            return;
        }

        Vector3 home = FlatPoint(status.homeBase.transform.position);

        if (FlatDistance(transform.position, home) <= homeStopDistance)
        {
            Hold();
            return;
        }

        motor.Move(FlatDirection(transform.position, home));
        SetSprint(sprintWhileRetreating);
    }

    /// <summary>Hold the guard post, drifting between a few nearby spots so it is not a statue.</summary>
    void Patrol()
    {
        SetSprint(false);   // never sprint on patrol: it saves stamina and keeps fieldTime at 0

        if (status.homeBase == null)
        {
            motor.Move(Vector2.zero);
            return;
        }

        if (FlatDistance(transform.position, patrolTarget) <= homeStopDistance)
        {
            motor.Move(Vector2.zero);

            patrolTimer += Time.deltaTime;
            if (patrolTimer >= patrolPauseSeconds)
            {
                patrolTimer = 0f;
                patrolTarget = NextPatrolPoint();
            }
            return;
        }

        motor.Move(FlatDirection(transform.position, patrolTarget));
    }

    void SetSprint(bool wants)
    {
        if (stamina != null) stamina.WantsSprint = wants;
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

    // --- flat (XZ) helpers: a top-down game only cares about the ground plane ---

    static Vector3 FlatPoint(Vector3 v)
    {
        return new Vector3(v.x, 0f, v.z);
    }

    static Vector2 Flat(Vector3 v)
    {
        return new Vector2(v.x, v.z);
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        return Vector2.Distance(Flat(a), Flat(b));
    }

    static Vector2 FlatDirection(Vector3 from, Vector3 to)
    {
        return (Flat(to) - Flat(from)).normalized;
    }
}
