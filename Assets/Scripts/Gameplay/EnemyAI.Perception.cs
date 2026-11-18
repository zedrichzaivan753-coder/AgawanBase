using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EnemyAI, part: perception.
///
/// What the brain is allowed to know: who it can see, what it remembers, and the flat (XZ)
/// maths every other part of the brain uses.
///
/// One type, split across files for readability only - see EnemyAI.cs for the brain itself.
/// </summary>
public partial class EnemyAI
{
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

            // A foe standing in a bush is invisible. Skipping it here is the whole stealth rule:
            // every state below is chosen from this one answer, so a hidden body is never
            // chased, intercepted or camped.
            if (foe.isHidden) continue;

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
        // still remember - but never read its live state while doing so. A target that has
        // hidden is forgotten outright: it went somewhere we are not allowed to know about.
        if (memoryTimer > 0f && memoryTarget != null && !memoryTarget.isCaptured &&
            !memoryTarget.isHidden)
        {
            return memoryTarget;
        }

        memoryTarget = nearest;
        return null;
    }

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
