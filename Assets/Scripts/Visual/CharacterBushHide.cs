using UnityEngine;

/// <summary>
/// Makes a character's BODY disappear while the game says it is standing under cover.
///
/// It hides the character's "Visual" child - the whole low-poly kid rig, including the flat blob
/// shadow quad - and nothing else. The overhead bars are gated separately by CharacterBars, so
/// both parts flip on the same frame and the body never appears with two floating bars over it.
///
/// WHY THE CHILD AND NEVER THE ROOT: CharacterController, CharacterMotor, CharacterStatus,
/// CharacterFacing, EnemyAI and SprintStamina all live on the character root. Deactivating the
/// root would stop movement, the AI brain and the freshness timer, so this script touches only the
/// cosmetic child. The rig builder gives every part under "Visual" no collider, so nothing
/// physical changes either - the character keeps walking, keeps its NavMesh-free motor and keeps
/// its cover circle exactly as before.
///
/// WHAT DECIDES "UNDER COVER": CharacterStatus.isHidden, the game's own stealth flag - the same
/// bool MatchManager and EnemyAI already honour. It is false while carrying a flag (a thief is
/// never covered) and is not refreshed while captured, so both cases are checked here too:
/// a prisoner and a flag carrier always stay visible, which is exactly what the rules say.
///
/// The marker ring, icon and chevron (CharacterIndicator) are deliberately NOT touched: they stay
/// on screen so a hidden body can still be located.
///
/// COST: one bool comparison per frame per character, and one SetActive only on the frame the
/// state actually flips.
/// </summary>
[DisallowMultipleComponent]
public class CharacterBushHide : MonoBehaviour
{
    [Tooltip("The child holding the low-poly body. Left empty, the child named 'Visual' is found " +
             "at startup.")]
    public Transform visual;

    [Tooltip("Log every time this body goes under cover or comes back out. Turn off for the " +
             "final build.")]
    public bool logHiding = true;

    [Header("State - read only")]
    [Tooltip("True while the body is hidden because the character is standing under cover.")]
    public bool isUnderCover;

    CharacterStatus status;
    bool warnedAboutMissingVisual;
    bool warnedAboutMissingStatus;

    void Awake()
    {
        status = GetComponent<CharacterStatus>();

        // The rig builder creates the child under this exact name, so no prefab wiring is needed.
        if (visual == null) visual = transform.Find("Visual");
    }

    void LateUpdate()
    {
        // LateUpdate, not Update: CharacterStatus fills in isHidden during Update, so by now the
        // flag describes the position the character has actually finished this frame in.
        if (status == null)
        {
            if (!warnedAboutMissingStatus)
            {
                warnedAboutMissingStatus = true;
                Debug.LogWarning("[Hide] " + name + " has no CharacterStatus, so it can never be " +
                                 "treated as under cover. Add CharacterStatus to the prefab.");
            }
            return;
        }

        bool hide = status.isHidden && !status.isCaptured;
        if (hide == isUnderCover) return;

        isUnderCover = hide;
        Apply(hide);

        if (logHiding)
        {
            if (hide)
            {
                Debug.Log("[Hide] " + name + " is under cover - body and bars hidden.");
            }
            else
            {
                Debug.Log("[Hide] " + name + " is out of cover - body and bars visible again.");
            }
        }
    }

    /// <summary>Shows or hides the body. Called only when the state changes.</summary>
    void Apply(bool hide)
    {
        if (visual == null)
        {
            if (!warnedAboutMissingVisual)
            {
                warnedAboutMissingVisual = true;
                Debug.LogWarning("[Hide] " + name + " has no child called 'Visual', so its body " +
                                 "cannot be hidden by cover. Assign the 'visual' field by hand.");
            }
            return;
        }

        visual.gameObject.SetActive(!hide);
    }

    /// <summary>
    /// A body must never be left invisible because this component was switched off while it was
    /// hidden - a restart rebuilds the roster, and an invisible prefab would be a ghost character.
    /// </summary>
    void OnDisable()
    {
        if (!isUnderCover) return;

        isUnderCover = false;
        if (visual != null) visual.gameObject.SetActive(true);
    }
}
