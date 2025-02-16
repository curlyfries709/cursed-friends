using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using DG.Tweening;
using Cinemachine;
using System.Linq;
using AnotherRealm;

public class Evade : MonoBehaviour, ITurnEndEvent
{
    public static Evade Instance { get; private set; }
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
    public int turnEndEventOrder { get; set; }

    CharacterGridUnit attacker;
    public CharacterGridUnit counterAttacker { get; private set; } = null;

    GameObject counterCanvas;
    CounterAttack counterAttackToPlay;

    List<CharacterGridUnit> targets = new List<CharacterGridUnit>();
    List<int> skillTargetsLengthAtTimeOfTargetEvade = new List<int>();

    List<Type> otherEventTypesThatCancelThis = new List<Type>();

    bool triggerEvadeEvent = false;

    //Event
    public Action<CharacterGridUnit, CharacterGridUnit, int> UnitEvaded;
    public Action<CharacterGridUnit> CounterTriggered;
    public Action PlayEvadeEvent;

    private void Awake()
    {
        Instance = this;
        turnEndEventOrder = transform.GetSiblingIndex();

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
        UnitEvaded += OnAttackEvaded;
        PlayEvadeEvent += PlayEvade;
    }

    private void OnAttackEvaded(CharacterGridUnit attacker, CharacterGridUnit target, int skillTargetsCount)
    {
        //Only ever one Attacker but could be multiple targets.
        triggerEvadeEvent = true;
        this.attacker = attacker;

        if (!targets.Contains(target))
        {
            targets.Add(target);
            skillTargetsLengthAtTimeOfTargetEvade.Add(skillTargetsCount);
        }
    }

    private void PlayEvade()
    {
        if (triggerEvadeEvent)
        {
            triggerEvadeEvent = false;

            foreach (CharacterGridUnit target in targets)
            {
                int targetIndex = targets.IndexOf(target);

                //Face Attacker
                //Vector3 targetRotationVector = -attacker.transform.forward;
                Vector3 targetRotationVector = CombatFunctions.GetAttackLookDirection(attacker, target);

                Quaternion lookRotation = Quaternion.LookRotation(targetRotationVector);
                Vector3 targetRotation = lookRotation.eulerAngles;
                targetRotation = new Vector3(0, targetRotation.y, 0);

                target.transform.DORotate(targetRotation, rotateToAttackerTime);
                target.unitAnimator.SetTrigger(target.unitAnimator.animIDEvade);

                //Start Countdown if single target or All Units Evaded for attack that targets multiple simultaneously...In this scenario Skill Target length should always be the same.
                target.GetComponent<FantasyHealth>().OnEvade(skillTargetsLengthAtTimeOfTargetEvade[targetIndex] <= 1 || 
                    (targets.Count == skillTargetsLengthAtTimeOfTargetEvade[targetIndex] && skillTargetsLengthAtTimeOfTargetEvade.Distinct().Count() == 1));

                //target.GetComponent<FantasyHealth>().OnEvade();

                Vector3 targetLeftDirection = -(new Vector3(targetRotationVector.z, 0, -targetRotationVector.x));
                Vector3 destination = target.transform.position + (targetLeftDirection * evadeDistance);

                target.transform.DOMove(destination, evadeTime);
            }

            CounterAttackCheck();
        }
    }

    public void PlayTurnEndEvent()
    {
        FantasyCombatManager.Instance.ActionComplete += ReturnCounterAttackerToPos;
        SetupUI();

        counterAttackToPlay.TriggerCounterAttack(attacker);
        CounterTriggered?.Invoke(counterAttacker);
    }

    public void OnEventCancelled()
    {
        counterAttackToPlay = null;
        counterAttacker = null; //As This is null, intially scheduled counterattack should return to pos when ReturnAllTargetsToPos is called...Tested and it works.
    }

    private void CounterAttackCheck()
    {
        CharacterGridUnit eligibleCounterAttackUnit = null;
        counterAttackToPlay = null;
        counterAttacker = null;

        //Check if evader in Range & Affinity of Attack. 
        foreach (CharacterGridUnit evader in targets)
        {
            if (TheCalculator.Instance.CanCounter(attacker, evader.counterAttack.attackElement) && IsEvaderInRangeForCounter(evader))
            {
                //Then Check if has higher speed than current eligible unit
                if (!eligibleCounterAttackUnit || evader.stats.Speed > eligibleCounterAttackUnit.stats.Speed)
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

    private bool IsEvaderInRangeForCounter(CharacterGridUnit evader)
    {
        foreach (GridPosition evaderGridPosition in evader.GetGridPositionsOnTurnStart())
        {
            GridPosition closestAttackerGridPos = LevelGrid.Instance.gridSystem.GetGridPosition(attacker.GetClosestPointOnColliderToPosition(evader.transform.position));
            GridPosition gridPositionDistance = evaderGridPosition - closestAttackerGridPos;

            int xDistance = Mathf.Abs(gridPositionDistance.x);
            int zDistance = Mathf.Abs(gridPositionDistance.z);

            int counterRange = evader.counterAttack.GetRange();

            if((xDistance == 0 || zDistance == 0) && xDistance <= counterRange && zDistance <= counterRange)
            {
                return true;
            }
        }

        return false;
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

        counterVCam.Follow = attacker.counterCamTarget;

        counterTargetGroup.AddMember(counterAttacker.counterCamTarget, weight, counterAttackerRadius);
        counterTargetGroup.AddMember(attacker.counterCamTarget, weight, attackerRadius);
        

        counterCam.SetActive(true);
    }

    IEnumerator FlashRoutine()
    {
        yield return new WaitForSeconds(FantasyCombatManager.Instance.GetSkillFeedbackDisplayTime() - timeToTriggerFlashBeforeCounter);
        flasher.Fade(true);
    }

    private void ReturnAllTargetsToPos()
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
        skillTargetsLengthAtTimeOfTargetEvade.Clear();
    }

    private void ReturnCounterAttackerToPos()
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
        UnitEvaded -= OnAttackEvaded;
        PlayEvadeEvent -= PlayEvade;
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
