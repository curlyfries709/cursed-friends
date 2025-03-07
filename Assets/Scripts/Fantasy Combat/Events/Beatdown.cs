using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using DG.Tweening;
using AnotherRealm;
using System;
using System.Linq;

public class Beatdown : CombatAction
{
    [Header("Power")]
    [SerializeField] PowerGrade beatdownPowerGrade = PowerGrade.C;
    [Header("UI")]
    [SerializeField] CombatEventCanvas beatdownCanvas;
    [SerializeField] FadeUI flasher;
    [Header("Timers")]
    [SerializeField] float beatdownTime;
    [SerializeField] float flasherDelay;
    [SerializeField] float beatdownCanvasDisplayTime;
    [Space(10)]
    [SerializeField] float enemyMoveToTargetTime = 0.25f;
    [SerializeField] float enemyReturnToPosTime = 0.15f;
    [SerializeField] float enemyRotationTime = 0.1f;
    [SerializeField] float delayBeforeDamageDisplay = 0.25f;
    [Space(5)]
    [SerializeField] float enemyMoveJumpPower = 1;
    [Header("Camera")]
    [SerializeField] CinemachineVirtualCamera beatdownCam;
    [SerializeField] Transform beatdownTargetGroupTransform;
    [Header("Prefabs")]
    [SerializeField] GameObject fightCloudPrefab;
    [Space(5)]
    [SerializeField] Transform fightCloudOffset;
    [Header("Values")]
    [SerializeField] float targetGroupWeight = 1;
    [SerializeField] float targetGroupRadius = 1.5f;
    [Space(10)]
    [SerializeField] float camNoiseFrequency = 1;

    //Cache
    CharacterGridUnit beatdownStarter;

    CinemachineTargetGroup beatdownTargetGroup;
    CinemachineImpulseSource impulseSource;
    CinemachineBasicMultiChannelPerlin camNoise;

    List<CharacterGridUnit> unitsParticipatingInBeatdown = new List<CharacterGridUnit>();

    List<GameObject> spawnedFightClouds = new List<GameObject>();

    Dictionary<CharacterGridUnit, CharacterGridUnit> unitAssignedTargets = new Dictionary<CharacterGridUnit, CharacterGridUnit>();
    Dictionary<CharacterGridUnit, Vector3> unitAssignedGridPosToWorldDict = new Dictionary<CharacterGridUnit, Vector3>();
    Dictionary<CharacterGridUnit, AttackData> targetDamageDict = new Dictionary<CharacterGridUnit, AttackData>();

    public static Action EnemyBeatdownSurvived;

    private void Awake()
    {
        beatdownTargetGroup = beatdownTargetGroupTransform.GetComponent<CinemachineTargetGroup>();
        impulseSource = GetComponent<CinemachineImpulseSource>();
        beatdownCanvas.SetDuration(beatdownCanvasDisplayTime);
    }

    public void TriggerBeatdown(CharacterGridUnit beatdownStarter)
    {
        BeginAction();

        this.beatdownStarter = beatdownStarter;
        FantasyCombatManager.Instance.TeamAttackInitiator = beatdownStarter;

        SetCaches();
        StartCoroutine(BeatdownStarterRoutine());
    }

    IEnumerator BeatdownStarterRoutine()
    {
        ControlsManager.Instance.DisableControls();
        FantasyCombatManager.Instance.ShowHUD(false);

        //FantasyCombatManager.Instance.ActivateCurrentActiveCam(false);
        beatdownCanvas.gameObject.SetActive(true);
        beatdownStarter.GetPhotoShootSet().PlayBeatdownUI();

        yield return new WaitForSeconds(flasherDelay);
        flasher.Fade(true);

        yield return new WaitForSeconds(beatdownCanvasDisplayTime - flasherDelay);

        beatdownStarter.GetPhotoShootSet().DeactivateSet();
        PlayBeatdownAnimation();
    }

    private void PlayBeatdownAnimation()
    {
        SetTargetGroupRotation();
        camNoise.m_FrequencyGain = 0;
        beatdownCam.gameObject.SetActive(true);

        StartCoroutine(BeatdownRoutine());
    }


    IEnumerator BeatdownRoutine()
    {
        MoveAttackers(false);
        yield return new WaitForSeconds(enemyMoveToTargetTime);

        SpawnFightClouds();

        //Begin camera Shake
        camNoise.m_FrequencyGain = camNoiseFrequency;

        //Damage Units
        DamageBeatenUpUnits();

        yield return new WaitForSeconds(beatdownTime);
        //Destroy Fight Clouds.
        DestroyFightClouds();

        //Stop Cam Shake.
        camNoise.m_FrequencyGain = 0;

        //Return Enemies.
        MoveAttackers(true);

        //Short Delay
        yield return new WaitForSeconds(delayBeforeDamageDisplay);

        //Invoke method to display Damage Data. 
        Health.RaiseHealthChangeEvent(true);

        //Generate Camera Shake.
        impulseSource.GenerateImpulse();

        //Recover from knockdown before Action Complete called.
        yield return new WaitForSeconds(FantasyCombatManager.Instance.GetUnextendedSkillFeedbackDisplayTime() - 0.1f);
        Recover();
    }

    private void MoveAttackers(bool returnToGridPos)
    {
        foreach(CharacterGridUnit attacker in unitsParticipatingInBeatdown)
        {
            float duration = returnToGridPos ? enemyReturnToPosTime : enemyMoveToTargetTime;
            Vector3 destination = returnToGridPos ? LevelGrid.Instance.gridSystem.GetWorldPosition(attacker.GetGridPositionsOnTurnStart()[0]) : unitAssignedGridPosToWorldDict[attacker];

            if (!returnToGridPos)
            {
                Quaternion lookRotation = Quaternion.LookRotation((destination - attacker.transform.position).normalized);
                attacker.transform.DORotate(lookRotation.eulerAngles, enemyRotationTime);
            }

            attacker.transform.DOJump(destination, enemyMoveJumpPower, 1, duration);
        }
    }
    
    private void SpawnFightClouds()
    {
        foreach(CharacterGridUnit target in actionTargets)
        {
            GameObject fightCloud = Instantiate(fightCloudPrefab, target.transform.position + fightCloudOffset.localPosition, Quaternion.identity);
            spawnedFightClouds.Add(fightCloud);

            target.unitAnimator.ShowModel(false);
        }

        //Hide Attackers
        foreach(CharacterGridUnit attacker in unitsParticipatingInBeatdown)
        {
            attacker.unitAnimator.ShowModel(false);
        }
    }


    private void DamageBeatenUpUnits()
    {
        foreach(CharacterGridUnit attacker in unitsParticipatingInBeatdown)
        {
            CharacterGridUnit target = unitAssignedTargets[attacker];
            int damage = TheCalculator.Instance.CalculateRawDamage(attacker, false, beatdownPowerGrade, out bool isCritical, false);

            if (targetDamageDict.ContainsKey(target))
            {
                targetDamageDict[target].HPChange += damage;
            }
            else
            {
                targetDamageDict[target] = GetAttackData(damage);
            }

            //Gift Attacker Enhanced FP
            attacker.CharacterHealth().GainFP(TheCalculator.Instance.CalculateFPGain(true));
        }

        foreach(CharacterGridUnit target in actionTargets)
        {
            target.CharacterHealth().TakeDamage(targetDamageDict[target], DamageType.Ultimate);
        }
    }

    private AttackData GetAttackData(int damage)
    {
        AttackData attackData = new AttackData(beatdownStarter, new List<GridUnit>(unitsParticipatingInBeatdown), Element.None, damage, actionTargets.Count);
        attackData.canEvade = false;

        attackData.isPhysical = true;

        attackData.powerGrade = beatdownPowerGrade;
        attackData.canCrit = false;

        return attackData;
    }

    private void DestroyFightClouds()
    {
        foreach(GameObject fightCloud in spawnedFightClouds)
        {
            Destroy(fightCloud);
        }

        //Show Units
        foreach (CharacterGridUnit target in actionTargets)
        {
            target.unitAnimator.ShowModel(true);
        }

        foreach (CharacterGridUnit attacker in unitsParticipatingInBeatdown)
        {
            attacker.unitAnimator.ShowModel(true);
        }
    }

    private void SetTargetGroupRotation()
    {
        float sumRotation = 0;

        foreach(CharacterGridUnit unit in actionTargets)
        {
            sumRotation = sumRotation + unit.transform.eulerAngles.y;
        }

        float averageRotation = sumRotation / actionTargets.Count;
        averageRotation = Mathf.Round(averageRotation / 90f) * 90;

        beatdownTargetGroupTransform.rotation = Quaternion.Euler(new Vector3(0, averageRotation, 0));
    }

    public override void EndAction()
    {
        beatdownCam.gameObject.SetActive(false);
        ResetAttackerRotation();
        base.EndAction();
    }

    private void Recover()
    {
        if(beatdownStarter is PlayerGridUnit) { return; } //Recovery doesn't apply to enemies...Sorry Stoney :(

        bool beatdownSurvived = true;

        //Cure Knockdown
        foreach (CharacterGridUnit player in actionTargets)
        {
            if (player.CharacterHealth().isKOed)
            {
                beatdownSurvived = false;
            }
            StatusEffectManager.Instance.RecoverFromKnockdown(player);
        }

        if (beatdownSurvived)
            EnemyBeatdownSurvived();
    }

    private void ResetAttackerRotation()
    {
        foreach (CharacterGridUnit attacker in unitsParticipatingInBeatdown)
        {
            Vector3 desiredRotation = Quaternion.LookRotation(CombatFunctions.GetCardinalDirectionAsVector(attacker.transform)).eulerAngles;
            attacker.transform.rotation = Quaternion.Euler(new Vector3(0, desiredRotation.y, 0));
        }
    }

    private void SetCaches()
    {
        ClearData();

        bool isPlayer = beatdownStarter as PlayerGridUnit;

        if (isPlayer)
        {
            //Set Players
            unitsParticipatingInBeatdown = new List<CharacterGridUnit>(FantasyCombatManager.Instance.GetPlayerCombatParticipants(false, false));

            //Set Player Targets
            foreach (CharacterGridUnit enemy in FantasyCombatManager.Instance.GetEnemyCombatParticipants(false, true))
            {
                //Only Target knocked Down Units.
                if (StatusEffectManager.Instance.IsUnitKnockedDown(enemy))
                {
                    actionTargets.Add(enemy);
                }
            }

            //Ordered by Highest Level.
            actionTargets.OrderByDescending((unit) => unit.stats.level);
            SetActionTargets(actionTargets);
        }
        else
        {
            //Set Enemies
            foreach (CharacterGridUnit enemy in FantasyCombatManager.Instance.GetEnemyCombatParticipants(false, false))
            {
                //If on the same Team Then they can particpate.
                if (enemy.team == beatdownStarter.team && !unitsParticipatingInBeatdown.Contains(enemy))
                {
                    unitsParticipatingInBeatdown.Add(enemy);
                }
            }

            //Set Enemy Targets
            SetActionTargets(FantasyCombatManager.Instance.GetPlayerCombatParticipants(false, true).ToList<GridUnit>());
        }

        //In Case there are more attackers than targets.
        if(unitsParticipatingInBeatdown.Count < actionTargets.Count)
        {
            int amountToRemove = actionTargets.Count - unitsParticipatingInBeatdown.Count;
            int startIndex = actionTargets.Count - amountToRemove -1;

            actionTargets.RemoveRange(startIndex, amountToRemove);
        }

        camNoise = beatdownCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

        //Set TargetGroup
        foreach (var target in beatdownTargetGroup.m_Targets)
        {
            beatdownTargetGroup.RemoveMember(target.target);
        }

        foreach (CharacterGridUnit unit in actionTargets)
        {
            beatdownTargetGroup.AddMember(unit.camFollowTarget, targetGroupWeight, targetGroupRadius);
        }

        SetTargets();
    }

    private void SetTargets()
    {
        int targetIndex = 0;

        foreach(CharacterGridUnit attacker in unitsParticipatingInBeatdown)
        {
            CharacterGridUnit assignedTarget = actionTargets[targetIndex] as CharacterGridUnit;

            unitAssignedTargets[attacker] = assignedTarget;
            unitAssignedGridPosToWorldDict[attacker] = assignedTarget.transform.position;

            targetIndex++;

            if(targetIndex >= actionTargets.Count)
            {
                targetIndex = 0;
            }
        }
    }

    private void ClearData()
    {
        actionTargets.Clear();
        unitsParticipatingInBeatdown.Clear();
        unitAssignedGridPosToWorldDict.Clear();
        targetDamageDict.Clear();
        unitAssignedTargets.Clear();
    }



}
