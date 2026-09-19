using UnityEngine;

/// <summary>
/// A circular home base on the ground.
/// Standing inside your OWN base resets your fieldTime to 0, and 0 always wins a touch --
/// that is what makes a base a safe zone. Being inside the ENEMY base gives you nothing.
/// Attach this to Base_Blue and Base_Red.
/// </summary>
public class BaseZone : MonoBehaviour
{
    [Tooltip("Which team owns this base.")]
    public Team team = Team.Blue;

    [Tooltip("Radius of the safe circle in world units, measured on the flat XZ plane.")]
    public float radius = 4f;

    /// <summary>True when worldPosition is inside this base's circle. Height is ignored.</summary>
    public bool Contains(Vector3 worldPosition)
    {
        Vector3 centre = transform.position;
        float dx = worldPosition.x - centre.x;
        float dz = worldPosition.z - centre.z;
        return (dx * dx + dz * dz) <= radius * radius;
    }

    /// <summary>Draws the safe circle in the Scene view so it is easy to line up with the base plate.</summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = team == Team.Blue ? Color.cyan : new Color(1f, 0.4f, 0.2f);
        Vector3 centre = new Vector3(transform.position.x, 0f, transform.position.z);
        Gizmos.DrawWireSphere(centre, radius);
    }
}
