using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using DG.Tweening;
using Cinemachine;
using AnotherRealm;

public class Evade : MonoBehaviour, ITurnEndEvent
{
    public static Evade Instance { get; private set; }

    [Header("Mode")]
    [Tooltip("When there are multiple valid counterattacks, how should we pick? Speed: Highest Speed. Score: Highest Score (Power Grade + Num of targets) and Random")]
    [SerializeField] TriggerMode triggerMode = TriggerMode.Speed;
    [Header("Distance")]
    [Range(0.5f, 1)]
    [SerializeField] float evadeDistance = 0.5f;
    [Header("Timers")]
    [SerializeField] float counterCanvasDisplayTime = 0.5f;
    [SerializeField] float timeToTriggerFlashBeforeCounter = 0.075f;
    [Space(10)]
    [SerializeField] float rotateToAttackerTime = 0.15f;
    [SerializeField] float evadeTime = 0.3f;
    [SerializeField] float returnTime = 0.25f;
    [Header("GameObjects")]
    [SerializeField] GameObject playerCounterCanvas;
    [SerializeField] GameObject enemyCounterCanvas;
    [Space(5)]
    [SerializeField] GameObject counterCam;
    [SerializeField] FadeUI flasher;
    [Header("Target Group Setting")]
    [SerializeField] CinemachineVirtualCamera counterVCam;
    [SerializeField] CinemachineTargetGroup counterTargetGroup;
    [Space(10)]
    [SerializeField] float weight = 1f;
    [SerializeField] float attackerRadius = 0.5f;
    [SerializeField] float counterAttackerRadius = 0.8f;

    //Caches

    GridUnit attacker;
    public CharacterGridUnit counterAttacker { get; private set; } = null;

    GameObject counterCanvas;
    CounterAttack counterAttackToPlay;

    List<CharacterGridUnit> targets = new List<CharacterGridUnit>();
    List<Type> otherEventTypesThatCancelThis = new List<Type>();

    //Event
    public Action<GridUnit, CharacterGridUnit> UnitEvaded;
    public Action<CharacterGridUnit> CounterTriggered;
    public Action<bool> TriggerEvadeEvent;

    public enum TriggerMode
    {
        Speed,
        Score,
        Random
    }

    private void Awake()
    {
        Instance = this;

        playerCounterCanvas.GetComponent<CombatEventCanvas>().SetDuration(counterCanvasDisplayTime);
        enemyCounterCanvas.GetComponent<CombatEventCanvas>().SetDuration(counterCanvasDisplayTime);

        SetEventsThatCancelThis();
    }

    private void SetEventsThatCancelThis()
    {
        otherEventTypesThatCancelThis.Add(typeof(KnockdownEvent));
    }

    private void OnEnable()
    {
        TriggerEvadeEvent += TriggerEvade;
    }

    public void PrepUnitToEvade(GridUnit attacker, CharacterGridUnit target)
    {
        //Only ever one Attacker but could be multiple targets.
        this.attacker = attacker;

        if (!targets.Contains(target))
        {
            targets.Add(target);
        }
    }

    private void TriggerEvade(bool triggerEvent)
    {
        if (!triggerEvent) //Event was cancelled
        {
            OnEventCancelled();
            return;
        }

        if(targets.Count == 0) { return; }

        foreach (CharacterGridUnit target in targets)
        {
            //Face Attacker
            //Vector3 targetRotationVector = -attacker.transform.forward;
            Vector3 targetRotationVector = CombatFunctions.GetAttackLookDirection(attacker, target);

            Quaternion lookRotation = Quaternion.LookRotation(targetRotationVector);
            Vector3 targetRotation = lookRotation.eulerAngles;
            targetRotation = new Vector3(0, targetRotation.y, 0);

            target.transform.DORotate(targetRotation, rotateToAttackerTime);
            target.unitAnimator.SetTrigger(target.unitAnimator.animIDEvade);

            target.GetComponent<CharacterHealth>().TriggerEvadeEvent();

            Vector3 targetLeftDirection = -(new Vector3(targetRotationVector.z, 0, -targetRotationVector.x));
            Vector3 destination = target.transform.position + (targetLeftDirection * evadeDistance);

            target.transform.DOMove(destination, evadeTime);

            UnitEvaded?.Invoke(attacker, target);
        }

        CounterAttackCheck();
    }

    public void PlayTurnEndEvent()
    {
        FantasyCombatManager.Instance.ActionComplete += ReturnCounterAttackerToPos;
        SetupUI();

        counterAttackToPlay.TriggerCounterAttack(attacker as CharacterGridUnit);
        CounterTriggered?.Invoke(counterAttacker);
    }

    public void OnEventCancelled()
    {
        targets.Clear();

        attacker = null;
        counterAttackToPlay = null;
        counterAttacker = null; //As This is null, intially scheduled counterattack should return to pos when ReturnAllTargetsToPos is called...Tested and it works.
    }

    private void CounterAttackCheck()
    {
        CharacterGridUnit eligibleCounterAttackUnit = null;
        counterAttackToPlay = null;
        counterAttacker = null;

        //Check if evader can counter
        foreach (CharacterGridUnit evader in targets)
        {
            if (evader.counterAttack.CanTrigger(attacker))
            {
                if (UpdateCurrentCounterAttacker(evader, eligibleCounterAttackUnit))
                {
                    eligibleCounterAttackUnit = evader;
                }
            }
        }

        if (eligibleCounterAttackUnit && FantasyCombatManager.Instance.AddTurnEndEventToQueue(this))
        {
            counterAttacker = eligibleCounterAttackUnit;
            counterAttackToPlay = eligibleCounterAttackUnit.counterAttack;

            if (FantasyCombatManager.Instance.IsTurnEndEventFirstInQueue(this))
                StartCoroutine(FlashRoutine());
        }

        FantasyCombatManager.Instance.ActionComplete += ReturnAllTargetsToPos;
    }

    private bool UpdateCurrentCounterAttacker(CharacterGridUnit evader, CharacterGridUnit currentCounterattacker)
    {
        if (!currentCounterattacker){ return true; }

        switch (triggerMode)
        {
            case TriggerMode.Speed:
               return evader.stats.Speed > currentCounterattacker.stats.Speed;
            case TriggerMode.Score:
                return evader.counterAttack.GetActionScore() > currentCounterattacker.counterAttack.GetActionScore();
            case TriggerMode.Random:
                return UnityEngine.Random.Range(0, 2) == 1;
            default:
                return false;
        }
    }

    private void SetupUI()
    {
        counterCanvas = counterAttacker.team == CombatTeam.Keenan ? playerCounterCanvas : enemyCounterCanvas;

        SetupCam();

        counterCanvas.SetActive(true);
        counterAttackToPlay.PlayCounterUI();
    }

    private void SetupCam()
    {
        foreach(var target in counterTargetGroup.m_Targets)
        {
            counterTargetGroup.RemoveMember(target.target);
        }

        counterVCam.Follow = (attacker as CharacterGridUnit).counterCamTarget;

        counterTargetGroup.AddMember(counterAttacker.counterCamTarget, weight, counterAttackerRadius);
        counterTargetGroup.AddMember(counterVCam.Follow, weight, attackerRadius);
        

        counterCam.SetActive(true);
    }

    IEnumerator FlashRoutine()
    {
        yield return new WaitForSeconds(FantasyCombatManager.Instance.GetSkillFeedbackDisplayTime() - timeToTriggerFlashBeforeCounter);
        flasher.Fade(true);
    }

    private void ReturnAllTargetsToPos(CombatAction completedAction)
    {
        FantasyCombatManager.Instance.ActionComplete -= ReturnAllTargetsToPos;

        foreach (CharacterGridUnit target in targets)
        {
            if (target != counterAttacker)
            {
                target.ReturnToPosAfterEvade(returnTime);
            }
        }

        targets.Clear();
    }

    private void ReturnCounterAttackerToPos(CombatAction completedAction)
    {
        FantasyCombatManager.Instance.ActionComplete -= ReturnCounterAttackerToPos;

        counterAttacker.returnToGridPosTime = returnTime;

        counterAttackToPlay.DeactivateCounterUI();
        counterAttacker.ReturnToCorrectPos();
        counterCam.SetActive(false);

        counterAttacker = null;
    }

    private void OnDisable()
    {
        TriggerEvadeEvent -= TriggerEvade;
    }
    public float GetTurnEndEventOrder()
    {
        return transform.GetSiblingIndex(); 
    }

    public float GetCounterCanvasDisplayTime()
    {
        return counterCanvasDisplayTime;
    }

    public List<Type> GetEventTypesThatCancelThis()
    {
        return otherEventTypesThatCancelThis;
    }


}
