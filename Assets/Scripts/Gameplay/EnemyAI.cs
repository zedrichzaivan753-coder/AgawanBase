using UnityEngine;

/// <summary>
/// The Red enemy brain. It has five states and one rule that decides between them:
/// only ever pick a fight you would win.
///
/// The game rule is: out in the field, the character with the LOWER fieldTime wins a touch,
/// and standing in your own base forces fieldTime back to 0. So a Red enemy may only chase
/// when its own fieldTime is lower than the player's - otherwise it is the one that gets
/// teleported to prison.
///
///   PATROL     - nothing worth doing: hold the guard post, which is also how the flag is
///                defended. Standing in the base keeps fieldTime at 0, so the guard is fresh.
///   CHASE      - the player is out of their base, we are fresher, close enough, and not winded.
///                This is also the state used to run down a player who stole our flag.
///   RETREAT    - the player is safe in their base, or is fresher than us: run home and reset.
///   INTERCEPT  - the player is carrying our flag and we could NOT win a touch against him, so
///                instead of feeding him a free capture we block the road to his base.
///   RETURNFLAG - our own flag is lying on the ground. Run at it and touch it to send it home.
///
/// The *decision* is only re-made every decisionInterval (about a quarter of a second), never
/// every frame, which keeps the AI cheap on a 30 FPS Android build. Walking still happens
/// every frame so the movement stays smooth.
///
/// The AI is never told anything the player could not see for himself. It reads its own
/// distance to the flag and the player's, never the other enemy's position, and every decision
/// is delayed by a reaction time - so it can always be predicted, and beaten.
/// </summary>
[RequireComponent(typeof(CharacterMotor))]
[RequireComponent(typeof(CharacterStatus))]
[RequireComponent(typeof(SprintStamina))]
public class EnemyAI : MonoBehaviour
{
    /// <summary>The five things a Red enemy can be doing.</summary>
    public enum AiState
    {
        /// <summary>Guarding the home base. Safe, and fieldTime stays at 0.</summary>
        Patrol,

        /// <summary>Running at the player, trying to win a touch.</summary>
        Chase,

        /// <summary>Running home so fieldTime resets to 0 and we become safe again.</summary>
        Retreat,

        /// <summary>
        /// Cutting the flag carrier off. Used when the player has our flag but a touch would be
        /// lost, so bumping into him would only hand him a free capture.
        /// </summary>
        Intercept,

        /// <summary>Fetching our own flag back after it was dropped in the field.</summary>
        ReturnFlag
    }

    [Header("Range")]
    [Tooltip("Start chasing once the player is closer than this, in metres.")]
    public float detectRadius = 12f;

    [Tooltip("While already chasing, keep going until the player is this many times further away " +
             "than detectRadius. The gap between the two distances stops the enemy flickering.")]
    public float giveUpMultiplier = 1.4f;

    [Tooltip("How close to the guard post counts as 'home'.")]
    public float homeStopDistance = 0.5f;

    [Tooltip("Where this enemy guards, as an offset (x, z) from its home base centre. " +
             "Set this per instance so two enemies do not stand on exactly the same spot.")]
    public Vector2 guardOffset = Vector2.zero;

    [Header("Decision timing")]
    [Tooltip("Seconds between AI decisions. 0.2 - 0.3 is plenty and keeps the AI cheap.")]
    public float decisionInterval = 0.25f;

    [Tooltip("Random amount added to / taken off each decision interval, so the two enemies desync.")]
    public float decisionJitter = 0.05f;

    [Tooltip("A state change may not happen sooner than this after the previous one.")]
    public float minStateTime = 0.6f;

    [Header("Freshness rule - only fight when I would win")]
    [Tooltip("Safety margin in seconds. We only chase when our fieldTime is lower than the " +
             "player's by at least this much. 0.3 s is roughly 1.5% of the 20 s freshness scale.")]
    public float chaseFresherMargin = 0.3f;

    [Header("Stamina")]
    [Tooltip("Stamina (0..1) needed before a chase may START, so it never chases while winded.")]
    public float chaseEnterStamina01 = 0.5f;

    [Tooltip("If stamina drops below this while chasing, give up and walk home to recover.")]
    public float chaseExitStamina01 = 0.1f;

    [Tooltip("Only sprint while further away than this from the player, so it arrives with stamina in hand.")]
    public float sprintDistance = 7f;

    [Tooltip("Run flat out while retreating - getting home is what saves it.")]
    public bool sprintWhileRetreating = true;

    [Header("Flag duty")]
    [Tooltip("This team's OWN flag (Flag_Red). Assigned per scene instance. The AI guards it, " +
             "runs down whoever steals it, and fetches it back if it is dropped.")]
    public Flag ourFlag;

    [Tooltip("How far ahead of the carrier we aim when intercepting, in metres.")]
    public float interceptLeadDistance = 4f;

    [Tooltip("When our flag is dropped, go for it unless the player is at least this many times " +
             "closer to it than we are. Otherwise we would lose the race and waste the trip.")]
    public float flagRaceAdvantage = 1.5f;

    [Tooltip("How close to our dropped flag counts as having reached it.")]
    public float returnStopDistance = 0.6f;

    [Header("Patrol")]
    [Tooltip("Seconds spent standing at a patrol point before picking a new one.")]
    public float patrolPauseSeconds = 2.5f;

    [Tooltip("How far from the guard post a patrol point may be picked.")]
    public float patrolRadius = 1.5f;

    [Header("Personality - rolled once per instance so the two enemies differ")]
    [Tooltip("Roll random aggression and reaction time on startup.")]
    public bool randomizeOnStart = true;

    [Tooltip("Lowest aggression. Scales how far this enemy notices the player.")]
    public float aggressionMin = 0.85f;

    [Tooltip("Highest aggression. Scales how far this enemy notices the player.")]
    public float aggressionMax = 1.15f;

    [Tooltip("Reaction time is rolled between 0 and this many seconds.")]
    public float reactionTimeRange = 0.5f;

    [Header("Debug")]
    [Tooltip("Log every state change with both fieldTime values. Turn off for the final build.")]
    public bool logStateChanges;

    [Header("State - read only")]
    [Tooltip("What this enemy is doing right now.")]
    public AiState state = AiState.Patrol;

    [Tooltip("Rolled once at startup, it widens or narrows this enemy's notice range.")]
    public float aggression = 1f;

    [Tooltip("Rolled once at startup. A short pause after each decision makes it look alive.")]
    public float reactionTime;

    CharacterMotor motor;
    CharacterStatus status;
    CharacterStatus target;      // the Blue player
    SprintStamina stamina;

    float decisionTimer;         // counts down to the next decision
    float stateTimer;            // how long we have been in the current state
    float reactionTimer;         // short freeze right after a state change
    float patrolTimer;           // how long we have been waiting at a patrol point
    Vector3 patrolTarget;        // where we are walking to while patrolling

    /// <summary>Shared stamina as 0..1. 0 when the component is missing, which means "no sprinting".</summary>
    float Stamina01
    {
        get { return stamina != null ? stamina.Stamina01 : 0f; }
    }

    /// <summary>
    /// True when we would WIN a touch against the player: our fieldTime is lower, with a
    /// small safety margin so a coin-flip is never taken.
    /// </summary>
    bool IAmFresher
    {
        get { return (status.fieldTime + chaseFresherMargin) < target.fieldTime; }
    }

    /// <summary>True when the player is the one carrying OUR flag.</summary>
    bool PlayerHasOurFlag
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

        if (randomizeOnStart)
        {
            // Two enemies with different numbers never move in lockstep.
            aggression = Random.Range(aggressionMin, aggressionMax);
            reactionTime = Random.Range(0f, reactionTimeRange);
        }

        patrolTarget = GuardPost();
    }

    void Start()
    {
        // There is only one player in the prototype, so find it once and remember it.
        // FindAnyObjectByType, not FindFirstObjectByType: the latter is deprecated on
        // Unity 6000.6 because it depends on instance-ID ordering (CS0618).
        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null) target = player.GetComponent<CharacterStatus>();
        else Debug.LogWarning("EnemyAI: no PlayerController found in the scene.");
    }

    void Update()
    {
        // A captured enemy is frozen in prison: no movement, no sprinting, no decisions.
        if (status.isCaptured)
        {
            SetSprint(false);
            motor.Move(Vector2.zero);
            return;
        }

        stateTimer += Time.deltaTime;
        decisionTimer -= Time.deltaTime;
        if (reactionTimer > 0f) reactionTimer -= Time.deltaTime;

        bool playerAvailable = (target != null && !target.isCaptured);

        // --- DECIDE: a few times a second, never every frame ---
        if (decisionTimer <= 0f)
        {
            decisionTimer = NextDecisionDelay();
            Decide(playerAvailable);
        }

        // --- ACT: every frame, so the walking looks smooth ---
        Act(playerAvailable);
    }

    // ------------------------------------------------------------------ deciding

    /// <summary>Chooses the state this enemy should be in. Only runs on the decision timer.</summary>
    void Decide(bool playerAvailable)
    {
        AiState next;

        if (!playerAvailable)
        {
            next = AiState.Patrol;                       // nobody to fight
        }
        else if (ShouldReturnFlag())
        {
            // Our own flag is on the ground, and that is the most urgent thing on the field:
            // every second it lies there is a second the player has to come back and re-take it.
            next = AiState.ReturnFlag;
        }
        else if (PlayerHasOurFlag && !target.IsInHomeBase)
        {
            // The player is running our flag home. Chase him if a touch would actually be won;
            // if it would not, block his road instead of gifting him a capture.
            next = CanStartChase() ? AiState.Chase : AiState.Intercept;
        }
        else if (target.IsInHomeBase || !IAmFresher)
        {
            // The player is standing in their own base (fieldTime 0, so untouchable), or they
            // are fresher than us. Either way a chase is a losing gamble: go home instead.
            next = AiState.Retreat;
        }
        else if (state == AiState.Chase)
        {
            next = StillWorthChasing() ? AiState.Chase : AiState.Patrol;
        }
        else if (CanStartChase())
        {
            next = AiState.Chase;
        }
        else
        {
            next = AiState.Patrol;
        }

        // Retreating from inside our own base is pointless - we are already safe there.
        if (next == AiState.Retreat && status.IsInHomeBase) next = AiState.Patrol;

        // Hysteresis: never change state twice in a hurry. This is what stops flip-flopping.
        if (next != state && stateTimer >= minStateTime) SetState(next);
    }

    /// <summary>
    /// Should we break off and go and fetch our own dropped flag?
    ///
    /// Deliberately honest: the only thing compared is our own distance against the PLAYER's,
    /// which is something this enemy could genuinely see from where it stands. It never peeks at
    /// the other enemy's position or at where the player is heading, so the player can always
    /// predict the race just by looking at the field.
    /// </summary>
    bool ShouldReturnFlag()
    {
        if (ourFlag == null || !ourFlag.IsDropped) return false;
        if (status.isCaptured) return false;                    // a prisoner fetches nothing

        float myDistance = FlatDistance(transform.position, ourFlag.transform.position);
        if (myDistance <= returnStopDistance) return true;      // already standing on it

        if (target != null && !target.isCaptured)
        {
            float playerDistance = FlatDistance(target.transform.position, ourFlag.transform.position);

            // If he is clearly closer he would beat us there and re-grab it in front of us.
            if (myDistance > playerDistance * flagRaceAdvantage) return false;
        }

        return true;
    }

    /// <summary>All the conditions for starting a chase, including "I am not winded".</summary>
    bool CanStartChase()
    {
        if (!IAmFresher) return false;
        if (stamina == null || !stamina.CanSprint) return false;    // winded or locked out
        if (Stamina01 < chaseEnterStamina01) return false;
        return FlatDistance(transform.position, target.transform.position) <= ChaseRange();
    }

    /// <summary>The looser version used once already chasing, so it commits instead of dithering.</summary>
    bool StillWorthChasing()
    {
        if (!IAmFresher) return false;
        if (stamina == null || !stamina.CanSprint) return false;
        if (Stamina01 < chaseExitStamina01) return false;
        return FlatDistance(transform.position, target.transform.position) <= ChaseRange();
    }

    /// <summary>
    /// How far away this enemy will notice the player. While already chasing the range is
    /// widened by giveUpMultiplier, which gives the chase a natural amount of stickiness.
    /// </summary>
    float ChaseRange()
    {
        float range = detectRadius * aggression;
        if (state == AiState.Chase) range *= giveUpMultiplier;
        return range;
    }

    void SetState(AiState next)
    {
        if (logStateChanges)
        {
            Debug.Log("[AI] " + name + ": " + state + " -> " + next +
                      "   myFT=" + status.fieldTime.ToString("F2") +
                      " playerFT=" + (target != null ? target.fieldTime.ToString("F2") : "n/a") +
                      " stamina=" + Stamina01.ToString("F2") +
                      " inBase=" + status.IsInHomeBase);
        }

        state = next;
        stateTimer = 0f;
        reactionTimer = reactionTime;   // a short freeze makes the change readable and desyncs the pair

        if (next == AiState.Patrol)
        {
            patrolTimer = 0f;
            patrolTarget = GuardPost();
        }
    }

    // ------------------------------------------------------------------- acting

    void Act(bool playerAvailable)
    {
        // Just changed our mind? Stand still for the reaction time before moving again.
        if (reactionTimer > 0f)
        {
            SetSprint(false);
            motor.Move(Vector2.zero);
            return;
        }

        if (state == AiState.Chase && playerAvailable) Chase();
        else if (state == AiState.Intercept && playerAvailable) Intercept();
        else if (state == AiState.ReturnFlag) FetchFlag();
        else if (state == AiState.Retreat) Retreat();
        else Patrol();
    }

    /// <summary>
    /// Block the carrier without touching him. A touch would be LOST, so actually bumping into
    /// him hands him a free capture and ends the game in his favour. Instead we run to a spot on
    /// the line between him and his base and stand in the way, which costs him time and lets a
    /// fresher team-mate - or his own empty stamina bar - catch up with him.
    /// </summary>
    void Intercept()
    {
        if (target == null)
        {
            motor.Move(Vector2.zero);
            SetSprint(false);
            return;
        }

        Vector3 carrierPosition = target.transform.position;

        // Where he is heading: his OWN base. If we cannot read it for some reason, just chase him.
        Vector3 hisGoal = target.homeBase != null ? target.homeBase.transform.position : carrierPosition;

        Vector2 towardsHisGoal = FlatDirection(carrierPosition, hisGoal);
        Vector2 aim = Flat(carrierPosition) + towardsHisGoal * interceptLeadDistance;
        Vector3 aimPoint = new Vector3(aim.x, 0f, aim.y);

        // Never step INSIDE his base to intercept. Inside it his fieldTime is 0 and ours is not,
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
            motor.Move(Vector2.zero);
            SetSprint(false);
            return;
        }

        motor.Move(FlatDirection(transform.position, aimPoint));

        // Sprint while there is still ground to make up, so we actually arrive first.
        SetSprint(FlatDistance(transform.position, carrierPosition) >= sprintDistance);
    }

    /// <summary>
    /// Run at our own dropped flag and touch it. This only does the walking - MatchManager judges
    /// the touch and sends the flag home, so there is still exactly one place that decides.
    /// </summary>
    void FetchFlag()
    {
        if (ourFlag == null)
        {
            motor.Move(Vector2.zero);
            SetSprint(false);
            return;
        }

        Vector3 flagPosition = ourFlag.transform.position;

        if (FlatDistance(transform.position, flagPosition) <= returnStopDistance)
        {
            motor.Move(Vector2.zero);
            SetSprint(false);
            return;   // standing on it, MatchManager returns it from here
        }

        motor.Move(FlatDirection(transform.position, flagPosition));

        // A dropped flag is a deadline, not a stroll: sprint while we still have stamina, and
        // SprintStamina quietly stops us once we are winded.
        SetSprint(true);
    }

    /// <summary>Walk straight at the player, sprinting in bursts while still far away.</summary>
    void Chase()
    {
        float distance = FlatDistance(transform.position, target.transform.position);
        motor.Move(FlatDirection(transform.position, target.transform.position));

        // Sprint only while far away. Once stamina runs out the shared lock-out in
        // SprintStamina stops it by itself, which turns this into short bursts with no
        // extra code here at all.
        SetSprint(distance >= sprintDistance);
    }

    /// <summary>
    /// Run home. The centre, not the guard post: getting inside the circle is what resets
    /// fieldTime to 0 and makes this enemy safe again.
    /// </summary>
    void Retreat()
    {
        if (status.homeBase == null)
        {
            motor.Move(Vector2.zero);
            SetSprint(false);
            return;
        }

        Vector3 home = FlatPoint(status.homeBase.transform.position);

        if (FlatDistance(transform.position, home) <= homeStopDistance)
        {
            motor.Move(Vector2.zero);
            SetSprint(false);
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

    /// <summary>The spot this enemy guards: its home base centre plus its own offset.</summary>
    Vector3 GuardPost()
    {
        if (status.homeBase == null) return transform.position;

        Vector3 centre = status.homeBase.transform.position;
        return new Vector3(centre.x + guardOffset.x, 0f, centre.z + guardOffset.y);
    }

    /// <summary>
    /// A random spot near the guard post, CLAMPED so it always stays inside the base circle.
    /// Without that clamp an idle enemy could park just outside its base, start collecting
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
