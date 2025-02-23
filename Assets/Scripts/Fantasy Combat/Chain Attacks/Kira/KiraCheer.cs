using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnotherRealm;

public class KiraCheer : PlayerBaseChainAttack
{
    [Header("CHEER DATA")]
    [SerializeField] float cheerTime = 2f;
    [SerializeField] GameObject cheerCanvas;
    [SerializeField] Transform cheerTextHeader;

    PlayerGridUnit target;

    protected override void CalculateAttackAffectedGridPositions()
    {
        selectedGridPositions = new List<GridPosition>(chainPasser.GetCurrentGridPositions());
        SetSelectedUnits();
    }

    public override bool TryTriggerSkill()
    {
        if (CanTriggerSkill(true))
        {
            //Since this only targets one unit
            target = selectedUnits[0] as PlayerGridUnit;

            BeginSkill(0,0, false);//Unit Position Updated here

            //Add Again Event 
            Again.Instance.SetUnitToGoAgain(target);

            //Start Routine.
            StartCoroutine(CheerRoutine());
            
            return true;
        }

        return false;
    }

    IEnumerator CheerRoutine()
    {
        //Activate Cam & Play Animation.
        myUnit.unitAnimator.TriggerSkill("Cheer");
        ActivateVisuals(true);
        cheerCanvas.SetActive(true);
        StartCoroutine(CheerCanvasRoutine());

        //Add Again Event for Target.

        yield return new WaitForSeconds(cheerTime);

        cheerCanvas.SetActive(false);
        DeactivateCam();

        FantasyCombatManager.Instance.ActionComplete();

        myUnit.unitAnimator.Idle();
    }

    IEnumerator CheerCanvasRoutine()
    {
        float waitTime = cheerTime / cheerTextHeader.childCount;
        int counter = 0;

        while(counter < cheerTextHeader.childCount)
        {
            cheerTextHeader.GetChild(counter).gameObject.SetActive(true);
            yield return new WaitForSeconds(waitTime);
            counter++;
        }
    }

    public override void SkillSelected()
    {
        //Skill Triggers Immediately So Unnecessary.
    }

    public override int GetSkillIndex()
    {
        return CombatFunctions.GetNonAffinityIndex(OtherSkillType.Support);
    }

}
