using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using AnotherRealm;

public class OrbTeleportation : PlayerBaseSkill, IOrb
{
    [Title("Orb Config")]
    [SerializeField] Orb orbData;
    [Title("Transforms")]
    [SerializeField] Transform targetA;
    [SerializeField] Transform targetB;
    [Title("Timers")]
    [SerializeField] float teleportWaitTime = 0.25f;
    [SerializeField] float teleportTime = 0.5f;
    [SerializeField] float postTeleportTime = 0.25f;

    GridPosition originalPosition;
    GridPosition targetGridPosition;
    GridUnit selectedUnit;

    public override void SkillSelected()
    {
        if (!skillTriggered)
        {
            GridVisual();
        }
    }

    public override bool TryTriggerSkill()
    {
        if (CanTriggerSkill(true) && IsValidUnitSelected())
        {
            BeginAction(0, 0, true, orbData);//Unit Position Updated here

            originalPosition = myUnit.GetCurrentGridPositions()[0];
            targetGridPosition = selectedGridPositions[0];
            selectedUnit = selectedUnits.Count > 0 ? skillTargets[0] : null;

            StartCoroutine(TeleportRoutine());

            return true;
        }
        else
        {
            return false;
        }
    }

    private void SetupCamera()
    {
        targetA.position = myUnitMoveTransform.transform.position;
        targetB.position = LevelGrid.Instance.gridSystem.GetWorldPosition(targetGridPosition);

        ActivateVisuals(true);
    }

    IEnumerator TeleportRoutine()
    {
        //Hide UI
        myUnit.Health().DeactivateHealthVisualImmediate();
        if (selectedUnit)
            selectedUnit.GetDamageable().DeactivateHealthVisualImmediate();

        //Cut to Camera
        SetupCamera();

        //Trigger Anim

        yield return new WaitForSeconds(teleportWaitTime);

        //Hide Units.
        myUnit.unitAnimator.ShowModel(false);

        if (selectedUnit)
        {
            CharacterGridUnit targetChar = selectedUnit as CharacterGridUnit;

            if (targetChar)
            {
                targetChar.unitAnimator.ShowModel(false);
            }
            else
            {
                selectedUnit.gameObject.SetActive(false);
            }
        }

        //Move to new Grid Pos
        myUnit.Warp(LevelGrid.Instance.gridSystem.GetWorldPosition(targetGridPosition), myUnit.transform.rotation);

        if (selectedUnit)
            selectedUnit.Warp(LevelGrid.Instance.gridSystem.GetWorldPosition(originalPosition), selectedUnit.transform.rotation);

        yield return new WaitForSeconds(teleportTime);
        //Remove From Grid
        LevelGrid.Instance.RemoveUnitFromGrid(myUnit);
        if(selectedUnit)
            LevelGrid.Instance.RemoveUnitFromGrid(selectedUnit);

        //Show & Set New Grid Pos
        myUnit.unitAnimator.ShowModel(true);
        myUnit.SetGridPositions();

        if (selectedUnit)
        {
            CharacterGridUnit targetChar = selectedUnit as CharacterGridUnit;

            if (targetChar)
            {
                targetChar.unitAnimator.ShowModel(true);
                targetChar.SetGridPositions();
            }
            else
            {
                selectedUnit.gameObject.SetActive(true);
            }
        }

        yield return new WaitForSeconds(postTeleportTime);

        //Call Action Complete
        FantasyCombatManager.Instance.ActionComplete();
    }



    //Validation
    private bool IsValidUnitSelected()
    {
        if(selectedUnits.Count == 0)
        {
            return true;
        }

        //Check if Selected can fit in space. 
        return true;

    }

    public override int GetSkillIndex()
    {
        return CombatFunctions.GetNonAffinityIndex(OtherSkillType.Tactic);
    }

    public override void SkillCancelled()
    {
        HideSelectedSkillGridVisual();

        AudioManager.Instance.PlaySFX(SFXType.TabBack);

        //Go back to Orb List
        collectionManager.OpenItemMenu(player, false);
    }


    public override void OnSkillInterrupted(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        if (battleResult != BattleResult.Restart) { return; }

        StopAllCoroutines();
        //feedbackToPlay?.StopFeedbacks();
        ActivateVisuals(false);
    }

}
