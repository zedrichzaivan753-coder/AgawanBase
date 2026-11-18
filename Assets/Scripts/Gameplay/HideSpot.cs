using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A bush a character can stand inside to disappear from the enemy.
///
/// It is a plain distance test on the XZ plane, exactly like MatchManager's touch rule and
/// BaseZone's safe circle - no trigger volumes, no Rigidbody, nothing that can fail silently on a
/// CharacterController that owns its own collider. Every bush files itself in a static list the
/// moment it is switched on, so neither the AI nor the rules ever search the scene.
///
/// WHO ASKS THIS
/// -------------
///   CharacterStatus.Update  - keeps its own isHidden flag up to date and logs every change.
///   MatchManager            - refuses to pair a hidden character into a touch, so hiding really
///                             does stop a capture instead of only stopping a chase.
///   EnemyAI.PickTarget      - refuses to see a hidden opponent.
///
/// A character carrying a flag is never hidden (see CharacterStatus), so the flag can never be
/// walked home by somebody who cannot be tagged.
/// </summary>
public class HideSpot : MonoBehaviour
{
    [Tooltip("Radius of the hiding area in world units, measured on the flat XZ plane. " +
             "Should sit just inside the foliage so the character is visually covered.")]
    public float radius = 1f;

    [Tooltip("Log every character that enters or leaves this bush. Turn off for the final build.")]
    public bool logHiding = true;

    /// <summary>Every bush in the loaded scene, kept up to date by OnEnable / OnDisable.</summary>
    static readonly List<HideSpot> all = new List<HideSpot>();

    void OnEnable()
    {
        if (!all.Contains(this)) all.Add(this);
    }

    void OnDisable()
    {
        all.Remove(this);
    }

    /// <summary>
    /// Domain reload is disabled on play in this project, so static state can survive from one
    /// play session to the next. Wiping the registry at subsystem registration is what keeps a
    /// stale entry from a previous session out of the first frame.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        all.Clear();
    }

    /// <summary>True when worldPosition is inside this bush. Height is ignored.</summary>
    public bool Contains(Vector3 worldPosition)
    {
        Vector3 centre = transform.position;
        float dx = worldPosition.x - centre.x;
        float dz = worldPosition.z - centre.z;
        return (dx * dx + dz * dz) <= radius * radius;
    }

    /// <summary>The bush this character is standing in, or null when it is out in the open.</summary>
    public static HideSpot SpotFor(CharacterStatus character)
    {
        if (character == null) return null;

        Vector3 p = character.transform.position;

        // At most a handful of bushes per map, so a plain index loop is cheaper than any lookup
        // and allocates nothing. Destroyed entries are dropped as they are found.
        for (int i = all.Count - 1; i >= 0; i--)
        {
            HideSpot spot = all[i];
            if (spot == null)
            {
                all.RemoveAt(i);
                continue;
            }

            if (spot.Contains(p)) return spot;
        }

        return null;
    }

    /// <summary>True when this character is standing in a bush.</summary>
    public static bool IsHidden(CharacterStatus character)
    {
        return SpotFor(character) != null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.35f, 0.9f, 0.35f, 0.8f);
        Vector3 centre = new Vector3(transform.position.x, 0f, transform.position.z);
        Gizmos.DrawWireSphere(centre, radius);
    }
}
