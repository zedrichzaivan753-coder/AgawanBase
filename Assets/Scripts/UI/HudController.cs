using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Copies game state into the HUD widgets. Display only: it never changes the game,
/// and it never decides anything. It just reads CharacterStatus and GameManager.
/// </summary>
public class HudController : MonoBehaviour
{
    [Header("Scene references")]
    [Tooltip("For the captured counter.")]
    public GameManager game;

    [Tooltip("For the freshness bar and the exposure timer.")]
    public CharacterStatus player;

    [Header("HUD widgets")]
    [Tooltip("Image with type = Filled, fillMethod = Horizontal. Full = fresh, empty = stale.")]
    public Image freshnessFill;

    [Tooltip("Small text under the bar, e.g. '4.2 s in the field'.")]
    public Text freshnessLabel;

    [Tooltip("Big text on the right, e.g. 'Captured  1 / 2'.")]
    public Text captureLabel;

    void Update()
    {
        if (player != null && freshnessFill != null)
        {
            freshnessFill.fillAmount = player.Freshness01;
        }

        if (player != null && freshnessLabel != null)
        {
            if (player.isCaptured) freshnessLabel.text = "CAPTURED";
            else if (player.IsInHomeBase) freshnessLabel.text = "SAFE AT BASE";
            else freshnessLabel.text = player.fieldTime.ToString("F1") + " s in the field";
        }

        if (game != null && captureLabel != null)
        {
            captureLabel.text = "Captured  " + game.RedsCaptured + " / " + game.TotalReds;
        }
    }
}
