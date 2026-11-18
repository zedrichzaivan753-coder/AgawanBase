using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Week 15 configuration regression for the sprint rules.
///
/// WHY THESE ARE CONFIGURATION TESTS AND NOT BEHAVIOUR TESTS: SprintStamina does its work in
/// Update(), and nothing ticks Update() in edit mode. Rather than fake a frame, these tests pin
/// the numbers a designer would change - the ones that decide whether the chase is winnable.
/// The behaviour itself (drain, lock-out, refill) is exercised in play mode by the scenario suite.
///
/// The carry penalty deserves its own note: PT6 described "the carry penalty on sprint", and the
/// code implements it as a STAMINA penalty with no speed penalty at all. That is deliberate and
/// documented in SprintStamina.cs:45-54 - the chaser and the carrier share a top speed, so a
/// speed penalty would make the run home impossible. These tests lock that decision in so it
/// cannot be "fixed" later by mistake.
/// </summary>
public class SprintConfigTests
{
    /// <summary>TC-STA-02: the carry penalty is regen-only, by design.</summary>
    [Test]
    public void CarryPenalty_IsAStaminaPenalty_NotASpeedPenalty()
    {
        GameObject go = new GameObject("SprintConfigProbe");
        SprintStamina stamina = go.AddComponent<SprintStamina>();

        try
        {
            Assert.AreEqual(1f, stamina.carrySpeedMultiplier, 0.0001f,
                "carrying a flag must cost no speed: a slower carrier could never reach home");
            Assert.AreEqual(0.5f, stamina.carryRegenMultiplier, 0.0001f,
                "carrying a flag must halve stamina regeneration - that is the carry penalty");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    /// <summary>TC-STA-01 / TC-STA-03: the sprint numbers must stay in a state where sprinting
    /// can start, run out and start again. A tuning slip here silently disables the mechanic.</summary>
    [Test]
    public void SprintNumbers_AllowSprintingToStartAgainAfterRunningOut()
    {
        GameObject go = new GameObject("SprintConfigProbe");
        SprintStamina stamina = go.AddComponent<SprintStamina>();

        try
        {
            Assert.Greater(stamina.sprintSpeedMultiplier, 1f,
                "sprinting must be faster than walking, or it is not a sprint");
            Assert.Greater(stamina.maxStamina, 0f, "the stamina pool must be positive");
            Assert.Greater(stamina.drainPerSecond, 0f, "sprinting must cost stamina");
            Assert.Greater(stamina.regenPerSecond, 0f, "stamina must come back");
            Assert.Less(stamina.restartThreshold, stamina.maxStamina,
                "the restart threshold must be below the pool, or sprint could never restart");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
