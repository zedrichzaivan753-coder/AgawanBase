using UnityEngine;

/// <summary>
/// A very small chase AI for one Red enemy. It has exactly two jobs:
///
///  1. CHASE the Blue player when he comes near.
///  2. GO HOME to its own base when nobody is near. Standing in its own base resets
///     fieldTime to 0, which is what makes an enemy safe again.
///
/// Because leaving the base is what makes an enemy attackable, every chase is a gamble:
/// the player can bait an enemy out, run home to reset to 0, then come back fresher and tag it.
/// </summary>
[RequireComponent(typeof(CharacterMotor))]
[RequireComponent(typeof(CharacterStatus))]
public class EnemyAI : MonoBehaviour
{
    [Tooltip("Start chasing once the Blue player is closer than this, in metres.")]
    public float detectRadius = 12f;

    [Tooltip("Keep chasing until the player is this many times further away than detectRadius. " +
             "The gap between the two distances stops the enemy flickering between chase and home.")]
    public float giveUpMultiplier = 1.4f;

    [Tooltip("How close to the guard post counts as 'home'.")]
    public float homeStopDistance = 0.5f;

    [Tooltip("Where this enemy guards, as an offset (x, z) from its home base centre. " +
             "Set this per instance so two enemies do not stand on exactly the same spot.")]
    public Vector2 guardOffset = Vector2.zero;

    CharacterMotor motor;
    CharacterStatus status;
    CharacterStatus target;      // the Blue player
    bool isChasing;

    void Awake()
    {
        motor = GetComponent<CharacterMotor>();
        status = GetComponent<CharacterStatus>();
    }

    void Start()
    {
        // There is only one player in the prototype, so find it once and remember it.
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (player != null) target = player.GetComponent<CharacterStatus>();
        else Debug.LogWarning("EnemyAI: no PlayerController found in the scene.");
    }

    void Update()
    {
        // A captured enemy is frozen in prison and does nothing.
        if (status.isCaptured)
        {
            motor.Move(Vector2.zero);
            return;
        }

        // No player to chase: just guard home.
        if (target == null || target.isCaptured)
        {
            GoHome();
            return;
        }

        float distanceToPlayer = FlatDistance(transform.position, target.transform.position);

        // Latch the decision so the enemy commits to a chase instead of wobbling at the edge.
        if (!isChasing && distanceToPlayer <= detectRadius)
        {
            isChasing = true;
        }
        else if (isChasing && distanceToPlayer > detectRadius * giveUpMultiplier)
        {
            isChasing = false;
        }

        if (isChasing) Chase();
        else GoHome();
    }

    /// <summary>Walk straight at the player.</summary>
    void Chase()
    {
        motor.Move(FlatDirection(transform.position, target.transform.position));
    }

    /// <summary>Walk back to the centre of this enemy's own base and wait there.</summary>
    void GoHome()
    {
        if (status.homeBase == null)
        {
            motor.Move(Vector2.zero);
            return;
        }

        Vector3 post = status.homeBase.transform.position + new Vector3(guardOffset.x, 0f, guardOffset.y);

        float distance = FlatDistance(transform.position, post);
        if (distance <= homeStopDistance) motor.Move(Vector2.zero);
        else motor.Move(FlatDirection(transform.position, post));
    }

    // --- flat (XZ) helpers: a top-down game only cares about the ground plane ---

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
