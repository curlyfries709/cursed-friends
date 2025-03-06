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

    protected override void CalculateAttackAffectedGridPositions()
    {
        selectedGridPositions = new List<GridPosition>(chainPasser.GetCurrentGridPositions());
        SetSelectedUnits();
    }

    public override bool TryTriggerSkill()
    {
        if (CanTriggerSkill(true))
        {
            BeginSkill(0,0, false);//Unit Position Updated here

            //Add Again Event 
            Again.Instance.SetUnitToGoAgain(actionTargets[0] as CharacterGridUnit);

            //Start Routine.
            StartCoroutine(CheerRoutine());
            
            return true;
        }

        return false;
    }

    IEnumerator CheerRoutine()
    {
        //Activate Cam & Play Animation.
        myCharacter.unitAnimator.TriggerSkill("Cheer");
        ActivateVisuals(true);
        cheerCanvas.SetActive(true);
        StartCoroutine(CheerCanvasRoutine());

        //Add Again Event for Target.

        yield return new WaitForSeconds(cheerTime);

        cheerCanvas.SetActive(false);
        DeactivateCam();

        myCharacter.unitAnimator.Idle();
        EndAction();
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
