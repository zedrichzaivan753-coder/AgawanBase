using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EnemyAI, part: acting.
///
/// What each state physically does once it has been chosen. These are the only methods in
/// the brain that move the character.
///
/// One type, split across files for readability only - see EnemyAI.cs for the brain itself.
/// </summary>
public partial class EnemyAI
{
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

        WalkTo(home);

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

        WalkTo(flagPosition);
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

        WalkTo(aimPoint);
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

        WalkTo(aimPoint);

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

        WalkTo(prisoner);

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

        WalkTo(flagPosition);

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

        WalkTo(aimPoint);

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

        WalkTo(home);
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

        WalkTo(patrolTarget);
    }

    /// <summary>
    /// Walks towards a world point, around whatever is in the way.
    ///
    /// This is the ONE place the brain moves a character towards something. Without a navigator
    /// (or on a map with no NavMesh baked) it is exactly the straight-line maths it replaced, so
    /// the original open field behaves as it always did.
    /// </summary>
    void WalkTo(Vector3 worldTarget)
    {
        Vector2 direction = navigator != null
            ? navigator.DirectionTo(worldTarget)
            : FlatDirection(transform.position, worldTarget);

        motor.Move(direction);
    }

    void SetSprint(bool wants)
    {
        if (stamina != null) stamina.WantsSprint = wants;
    }
}
