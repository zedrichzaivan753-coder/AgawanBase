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

    [Tooltip("Image with type = Filled, fillMethod = Horizontal. Full = all stamina.")]
    public Image staminaFill;

    [Tooltip("Small text next to the stamina bar, e.g. 'STAMINA  70%'.")]
    public Text staminaLabel;

    [Tooltip("Shouty banner that only appears while the player is carrying the flag.")]
    public Text flagLabel;

    [Tooltip("The Red flag, so the HUD can tell whether the player is carrying it.")]
    public Flag redFlag;

    // The player's shared sprint component. Read only - the HUD never changes the game.
    SprintStamina stamina;

    void Awake()
    {
        // The stamina bar belongs to the player, so grab his sprint component once here.
        if (player != null) stamina = player.GetComponent<SprintStamina>();

        // A Filled Image only draws a PARTIAL bar if it has a sprite. With no sprite Unity
        // ignores fillAmount completely and always draws a full rectangle, so complain loudly
        // here rather than shipping a stamina bar that never moves.
        if (staminaFill != null && staminaFill.sprite == null)
        {
            Debug.LogWarning("HudController: 'staminaFill' has no sprite, so its fillAmount is " +
                             "ignored and the bar will always look full. Give it a sprite, or copy " +
                             "FreshnessFill as the template.");
        }
    }

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

        if (stamina != null && staminaFill != null)
        {
            staminaFill.fillAmount = stamina.Stamina01;
        }

        if (stamina != null && staminaLabel != null)
        {
            // The percentage is ALWAYS shown, sprinting or not, so you can see exactly how much
            // stamina is left at the moment you need it. Only the word in front changes.
            // Build the wanted text, then only assign it when it has actually changed.
            // That keeps the HUD from creating a new string every single frame.
            string wanted;
            if (player != null && player.isCaptured)
            {
                wanted = "STAMINA  -";
            }
            else
            {
                string percent = Mathf.RoundToInt(stamina.Stamina01 * 100f) + "%";
                wanted = (stamina.IsSprinting ? "SPRINTING  " : "STAMINA  ") + percent;
            }

            if (wanted != staminaLabel.text) staminaLabel.text = wanted;
        }

        if (flagLabel != null)
        {
            // The carry banner only exists while the player actually holds the flag, so the widget
            // is switched off the rest of the time rather than left showing an empty string.
            bool carrying = redFlag != null && redFlag.IsCarried &&
                            player != null && redFlag.carrier == player.transform;

            // The comparison matters: SetActive every frame would rebuild the canvas constantly.
            if (flagLabel.gameObject.activeSelf != carrying) flagLabel.gameObject.SetActive(carrying);

            if (carrying)
            {
                const string wanted = "YOU HAVE THE FLAG!  Get back to your base!";
                if (wanted != flagLabel.text) flagLabel.text = wanted;
            }
        }

        if (game != null && captureLabel != null)
        {
            captureLabel.text = "Captured  " + game.RedsCaptured + " / " + game.TotalReds;
        }
    }
}
