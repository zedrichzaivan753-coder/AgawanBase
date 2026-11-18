using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EnemyAI, part: scoring.
///
/// Every Score* method, plus the Consider/RoleBonus pair that turns a score into a decision.
/// Pure reads of the facts RefreshFacts gathered - none of these methods moves anything.
///
/// One type, split across files for readability only - see EnemyAI.cs for the brain itself.
/// </summary>
public partial class EnemyAI
{
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
}
