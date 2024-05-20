using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using AnotherRealm;

public class TerritoryBorder : MonoBehaviour
{
    [SerializeField] bool banEntry = false;
    [SerializeField] Dialogue bannedEntryDialogue;
    [Header("Movement")]
    [SerializeField] Transform moveAwayHeader;
    [SerializeField] float rotationTime = 0.15f;
    [Header("Obstacle")]
    [Tooltip("Whether this is an obstacle that is activated & deactivated during runtime")]
    [SerializeField] protected bool markAsDynamicObstacle = false;

    protected Collider myCollider;
    PlayerStateMachine player;

    bool movingAway = false;

    private void Awake()
    {
        myCollider = GetComponent<Collider>();
        myCollider.isTrigger = true;

        ProhibitEntry(banEntry);
        player = StoryManager.Instance.GetPlayerStateMachine();
    }

    private void OnEnable()
    {
        FantasyCombatManager.Instance.CombatBegun += OnCombatBegin;
        FantasyCombatManager.Instance.CombatEnded += OnCombatEnd;

        if (markAsDynamicObstacle)
            PathFinding.Instance.SetDynamicObstacle(myCollider as BoxCollider, myCollider, true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (FantasyCombatManager.Instance.InCombat() || !banEntry || movingAway) { return; } //Do not execute during combat

        if (other.CompareTag("Player"))
        {
            PlayOnEnterDialogue();
        }
    }

    protected void PlayOnEnterDialogue()
    {
        //Subscribe to On Dialogue End
        DialogueManager.Instance.DialogueEnded += OnEntryDialogueEndEvent;

        DialogueManager.Instance.PlayDialogue(bannedEntryDialogue, false);
    }

    protected virtual void OnEntryDialogueEndEvent()
    {
        //Unsubscribe
        DialogueManager.Instance.DialogueEnded -= OnEntryDialogueEndEvent;
        MovePlayerAway();
    }

    protected void MovePlayerAway()
    {
        ControlsManager.Instance.DisableControls();

        StartCoroutine(AutoMoveRoutine());
    }

    IEnumerator AutoMoveRoutine()
    {
        movingAway = true;

        Vector3 moveAwayDestination = CombatFunctions.GetClosestTransform(moveAwayHeader, player.transform.position).position;

        //Calculate Move time
        float moveTime = Vector3.Distance(player.transform.position, moveAwayDestination) / player.walkSpeed; //t = D/S
        //Calculate Rotation
        Vector3 targetRot = Quaternion.LookRotation((moveAwayDestination - player.transform.position).normalized).eulerAngles;

        //Begin AutoMove
        player.BeginAutoMove(true, player.walkSpeed);

        player.transform.DOMove(moveAwayDestination, moveTime);
        player.transform.DORotate(targetRot, rotationTime);

        //Prepare to Decelerate
        float decelerationTime = player.SpeedChangeRate * Time.deltaTime;

        yield return new WaitForSeconds(moveTime - decelerationTime);

        //Begin Deceleration
        player.BeginAutoMove(true, 0);

        //Stop Movement
        yield return new WaitForSeconds(decelerationTime);
        MovementComplete();
    }

    private void MovementComplete()
    {
        //Re-Enable Controls
        player.BeginAutoMove(false, 0);
        ControlsManager.Instance.SwitchCurrentActionMap("Player");
        movingAway = false;
    }

    private void OnCombatBegin(BattleStarter.CombatAdvantage combatAdvantage)
    {
        //Change Collider to be non-trigger
        myCollider.isTrigger = false;
    }

    private void OnCombatEnd(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        //Change Collider to be a trigger
        myCollider.isTrigger = true;
    }

    private void OnDisable()
    {
        FantasyCombatManager.Instance.CombatBegun -= OnCombatBegin;
        FantasyCombatManager.Instance.CombatEnded -= OnCombatEnd;

        if (markAsDynamicObstacle)
        {
            PathFinding.Instance.SetDynamicObstacle(myCollider as BoxCollider, myCollider, false);
        }
    }

    private void ProhibitEntry(bool prohibit)
    {
        banEntry = prohibit;
    }
}
