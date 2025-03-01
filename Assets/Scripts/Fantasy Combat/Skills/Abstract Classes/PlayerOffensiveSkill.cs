using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;
using System.Linq;
using Sirenix.OdinInspector;
using AnotherRealm;


public  abstract class PlayerOffensiveSkill : PlayerBaseSkill, IOffensiveSkill
{
    [Title("OFFENSIVE SKILL DATA")]
    [SerializeField] protected OffensiveSkillData offensiveSkillData;
    [Title("VFX & Spawn Points")]
    [SerializeField] protected Transform hitVFXPoolHeader;
    [SerializeField] protected List<Transform> hitVFXSpawnOffsets;
    [Space(10)]
    [ShowIf("offensiveSkillData.isMagical")]
    [SerializeField] protected Transform magicalAttackVFXDestination;

    //Storage
    protected List<MMF_Player> targetFeedbacksToPlay = new List<MMF_Player>();
    private List<GameObject> hitVFXPool = new List<GameObject>();

    protected override void Awake()
    {
        base.Awake();

        if (myUnit)
        {
            offensiveSkillData.SetupData(this, myUnit);
        }

        //Generate Pool Reference
        if (hitVFXPoolHeader)
        {
            foreach (Transform child in hitVFXPoolHeader)
            {
                hitVFXPool.Add(child.gameObject);
            }
        }
    }

    public override void Setup(SkillPrefabSetter skillPrefabSetter, SkillData skillData)
    {
        base.Setup(skillPrefabSetter, skillData);
        offensiveSkillData.SetupData(this, myUnit);
    }

    public override void OnSkillInterrupted(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        if (battleResult != BattleResult.Restart) { return; }
        //STOP ALL FEEDBACKS

        foreach (var feedback in targetFeedbacksToPlay)
        {
            feedback?.StopFeedbacks();
        }

        IOffensiveSkill().StopAllSkillFeedbacks();
    }

    protected void Attack() //MOVE SOME FUNCTIONALTY TO INTERFACE
    {
        targetFeedbacksToPlay.Clear();

        Affinity attackAffinity = Affinity.None;

        List<Affinity> allTargetsAffinity = new List<Affinity>();

        foreach (GridUnit target in skillTargets)
        {
            int targetIndex = skillTargets.IndexOf(target);

            Affinity targetAffinity = IOffensiveSkill().DamageTarget(target, skillTargets.Count, ref anyTargetsWithReflectAffinity);
            allTargetsAffinity.Add(targetAffinity);

            GameObject hitVFX = hitVFXPool.Count > 0 ? hitVFXPool[targetIndex] : null;

            AffinityFeedback feedbacks = target.Health().GetDamageFeedbacks(CombatFunctions.GetVFXSpawnTransform(hitVFXSpawnOffsets, target), hitVFX);
            targetFeedbacksToPlay.Add(CombatFunctions.GetTargetFeedback(feedbacks, targetAffinity));
        }

        if (isSingleTarget)
        {
            attackAffinity = allTargetsAffinity[0];

            if (magicalAttackVFXDestination)
                magicalAttackVFXDestination.transform.position = skillTargets[0].camFollowTarget.position;
        }
        else
        {
            //If all targets Evaded
            if(allTargetsAffinity.Distinct().Count() == 1 && allTargetsAffinity[0] == Affinity.Evade)
            {
                attackAffinity = Affinity.Evade;
            }
        }

        IOffensiveSkill().MoveToAttack(skillTargets[0], GetSkillOwnerMoveTransform(), GetCardinalDirectionAsVector());

        myCharacter.unitAnimator.TriggerSkill(offensiveSkillData.animationTriggerName);
        CombatFunctions.PlayAttackFeedback(attackAffinity, offensiveSkillData.attackFeedbacks);
    }


    public void PlayTargetsAffinityFeedback()
    {
        //Called Via A feedback
        foreach(var feedback in targetFeedbacksToPlay)
        {
            feedback?.PlayFeedbacks();
        }
    }

    protected override void SetUnitsToShow()
    {
        IOffensiveSkill().SetUnitsToShow(selectedUnits, forceDistance);
    }

    //GETTERS
    public override int GetSkillIndex()
    {
        switch (IOffensiveSkill().GetSkillElement())
        {
            case Element.Silver: 
                return 0;
            case Element.Steel: 
                return 1;
            case Element.Gold:
                return 2;
            case Element.Fire:
                return 3;
            case Element.Ice:
                return 4;
            case Element.Air:
                return 5;
            case Element.Earth:
                return 6;
            case Element.Holy:
                return 7;
            case Element.Curse: 
                return 8;
            default:
                Debug.Log("SKILL INDEX NOT SET FOR ELEMENT");
                return 0;
        }
    }

    public OffensiveSkillData GetOffensiveSkillData()
    {
        return offensiveSkillData;
    }

    public IOffensiveSkill IOffensiveSkill()
    {
        return this;
    }
}
