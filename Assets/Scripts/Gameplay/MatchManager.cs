using System;
using UnityEngine;

/// <summary>
/// The single authority for the rules of the game. Nothing else decides who wins a touch.
///
/// The rulings it makes:
///   - A touch between the Blue player and a free Red enemy is resolved when they are closer
///     than <see cref="tagDistance"/>. The character with the LOWER fieldTime wins; the loser
///     is teleported into the enemy prison and frozen.
///   - Exactly equal fieldTime is a draw, and nothing happens.
///   - The Blue player reaching the Red flag while free = Victory.
///   - A free Red reaching the Blue flag = Game Over.
///
/// Note on bases: a character inside its own base always has fieldTime 0, so it can never lose
/// a touch there. That is how a base acts as a safe zone, with no extra special case needed.
/// </summary>
public class MatchManager : MonoBehaviour
{
    [Header("Scene references")]
    [Tooltip("The Blue safe zone (Base_Blue).")]
    public BaseZone blueBase;

    [Tooltip("The Red safe zone (Base_Red).")]
    public BaseZone redBase;

    [Tooltip("Where a captured BLUE player is held (PrisonForBlue).")]
    public Transform prisonForBlue;

    [Tooltip("Where captured RED enemies are held (PrisonForRed).")]
    public Transform prisonForRed;

    [Tooltip("The Blue flag that the enemies try to reach (Flag_Blue).")]
    public Transform blueFlag;

    [Tooltip("The Red flag the player tries to reach (Flag_Red).")]
    public Transform redFlag;

    [Tooltip("The Blue player.")]
    public CharacterStatus player;

    [Tooltip("The Red enemies.")]
    public CharacterStatus[] enemies = new CharacterStatus[2];

    [Header("Rules")]
    [Tooltip("How close two opponents must be to count as a touch, in metres.")]
    public float tagDistance = 1.2f;

    [Tooltip("How close a free character must be to the enemy flag to grab it, in metres.")]
    public float flagDistance = 1.5f;

    [Tooltip("Difference in fieldTime below which a touch counts as a draw.")]
    public float tieEpsilon = 0.01f;

    [Tooltip("ON = a touch only counts when BOTH characters are out in the field, " +
             "so no base can be attacked at all. " +
             "OFF (default) = the bases are protected purely by fieldTime being 0 inside them, " +
             "which means an enemy base can be raided by a perfectly fresh (fieldTime 0) player.")]
    public bool requiresBothOutsideBases = false;

    [Tooltip("ON = equal fieldTime means nothing happens.")]
    public bool tieIsNoCapture = true;

    [Header("State - read only")]
    [Tooltip("Set by GameManager. The rules only run while this is true.")]
    public bool matchActive;

    [Tooltip("How many Red enemies have been captured.")]
    public int redsCaptured;

    /// <summary>Raised when the Blue player is captured (Game Over).</summary>
    public event Action PlayerLost;

    /// <summary>Raised when the Blue player reaches the Red flag while free (Victory).</summary>
    public event Action PlayerWon;

    /// <summary>Raised whenever the number of captured enemies changes.</summary>
    public event Action<int> CaptureCountChanged;

    /// <summary>How many enemies the match started with, for the HUD counter.</summary>
    public int TotalEnemies
    {
        get { return enemies != null ? enemies.Length : 0; }
    }

    void Awake()
    {
        // Loud, early complaints beat a silent rules bug.
        if (player == null) Debug.LogError("MatchManager: 'player' is not assigned.");
        if (enemies == null || enemies.Length == 0) Debug.LogError("MatchManager: 'enemies' is empty.");
        if (redFlag == null || blueFlag == null) Debug.LogError("MatchManager: flags are not assigned.");
        if (prisonForRed == null || prisonForBlue == null) Debug.LogError("MatchManager: prisons are not assigned.");
    }

    void Update()
    {
        if (!matchActive) return;

        // Touches are resolved before flag grabs: if you are tagged you are no longer "free",
        // so reaching the flag in the same instant does not save you.
        CheckCharacterTouches();
        if (!matchActive) return;   // the player was captured this frame

        CheckFlagTouches();
    }

    // ---------------------------------------------------------------- touches

    void CheckCharacterTouches()
    {
        if (player == null || player.isCaptured) return;

        for (int i = 0; i < enemies.Length; i++)
        {
            CharacterStatus enemy = enemies[i];
            if (enemy == null || enemy.isCaptured) continue;   // prisoned enemies cannot fight

            float distance = FlatDistance(player.transform.position, enemy.transform.position);
            if (distance > tagDistance) continue;

            // Optional rule: ignore every touch that happens inside a base.
            if (requiresBothOutsideBases &&
                (IsInAnyBase(player.transform.position) || IsInAnyBase(enemy.transform.position)))
            {
                continue;
            }

            ResolveTouch(player, enemy, i);

            if (player.isCaptured || !matchActive) return;
        }
    }

    /// <summary>Applies the "lower fieldTime wins" rule to one touching pair.</summary>
    void ResolveTouch(CharacterStatus blue, CharacterStatus red, int redIndex)
    {
        float blueTime = blue.fieldTime;
        float redTime = red.fieldTime;

        // A draw: nothing happens.
        if (tieIsNoCapture && Mathf.Abs(blueTime - redTime) <= tieEpsilon)
        {
            Debug.Log("[Match] draw - " + blue.name + " and " + red.name +
                      " both have fieldTime " + blueTime.ToString("F2") + ", nobody is captured.");
            return;
        }

        bool blueWins = blueTime < redTime;
        CharacterStatus loser = blueWins ? red : blue;

        Debug.Log("[Match] touch: " + blue.name + " fieldTime=" + blueTime.ToString("F2") +
                  " vs " + red.name + " fieldTime=" + redTime.ToString("F2") +
                  "  -> " + (blueWins ? blue.name : red.name) + " wins (lower is fresher), " +
                  loser.name + " is captured.");

        if (loser == blue)
        {
            // The player lost: freeze him in the enemy prison and end the match.
            blue.Capture(PrisonSpot(prisonForBlue, 0));
            matchActive = false;
            Debug.Log("[Match] the BLUE player was captured -> GAME OVER");
            if (PlayerLost != null) PlayerLost();
        }
        else
        {
            // An enemy lost: freeze it in its own prison and count it.
            red.Capture(PrisonSpot(prisonForRed, redIndex));
            redsCaptured++;
            Debug.Log("[Match] captured enemy " + red.name + " (" + redsCaptured + "/" + TotalEnemies + ")");
            if (CaptureCountChanged != null) CaptureCountChanged(redsCaptured);
        }
    }

    // ------------------------------------------------------------------ flags

    void CheckFlagTouches()
    {
        // The player grabs the Red flag while free -> Victory.
        if (player != null && !player.isCaptured && IsTouching(player.transform.position, redFlag))
        {
            matchActive = false;
            Debug.Log("[Match] the BLUE player touched the RED flag while free -> VICTORY");
            if (PlayerWon != null) PlayerWon();
            return;
        }

        // A free enemy reaches the Blue flag -> Game Over.
        for (int i = 0; i < enemies.Length; i++)
        {
            CharacterStatus enemy = enemies[i];
            if (enemy == null || enemy.isCaptured) continue;
            if (!IsTouching(enemy.transform.position, blueFlag)) continue;

            matchActive = false;
            Debug.Log("[Match] " + enemy.name + " touched the BLUE flag while free -> GAME OVER");
            if (PlayerLost != null) PlayerLost();
            return;
        }
    }

    // ------------------------------------------------------- small helpers

    bool IsTouching(Vector3 position, Transform target)
    {
        if (target == null) return false;
        return FlatDistance(position, target.position) <= flagDistance;
    }

    bool IsInAnyBase(Vector3 position)
    {
        if (blueBase != null && blueBase.Contains(position)) return true;
        if (redBase != null && redBase.Contains(position)) return true;
        return false;
    }

    /// <summary>
    /// A standing spot inside a prison, offset per slot so two prisoners do not share a spot.
    /// The prison plates have no colliders, so the ground height (y = 0) is the right height.
    /// </summary>
    Vector3 PrisonSpot(Transform prison, int slot)
    {
        if (prison == null) return Vector3.zero;
        float offset = (slot - (TotalEnemies - 1) * 0.5f) * 1.2f;
        return new Vector3(prison.position.x, 0f, prison.position.z + offset);
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }
}
