using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Animations;
using Cinemachine;

public class OffensiveOrb : PlayerOffensiveSkill, IOrb
{
    [Title("Orb Config")]
    [SerializeField] Orb orbData;
    [Space(10)]
    [SerializeField] GameObject orbModel;
    [SerializeField] ParentConstraint orbParentConstraint;

    CinemachineBlendListCamera cmBlendlist;

    protected override void Awake()
    {
        base.Awake();
        cmBlendlist = blendListCamera.GetComponent<CinemachineBlendListCamera>();
    }

    public override void SkillSelected()
    {
        if (!skillTriggered)
        {
            GridVisual();
        }
    }

    //Trigger Skill Logic

    public override bool TryTriggerSkill()
    {
        if (CanTriggerSkill(true))
        {
            SetCamera();

            //Setup Orb Parent
            SetParentConstraint();
            
            BeginAction(returnToGridPosTime, delayBeforeReturn, true, orbData);//Unit Position Updated here

            myUnit.unitAnimator.HideWeapon();
            ActivateVisuals(true);

            //Attack
            Attack(); //Damage Target Called in here.

            FantasyCombatManager.Instance.ActionComplete += OnActionComplete;

            return true;
        }
        else
        {
            return false;
        }
    }

    private void OnActionComplete()
    {
        FantasyCombatManager.Instance.ActionComplete -= OnActionComplete;
        myUnit.unitAnimator.ShowWeapon();
    }

    private void SetParentConstraint()
    {
        ConstraintSource constraintSource = new ConstraintSource();
        constraintSource.sourceTransform = myUnit.unitAnimator.GetRightHand();
        constraintSource.weight = 1;

        orbParentConstraint.SetSource(0, constraintSource);
    }

    private void SetCamera()
    {
        cmBlendlist.LookAt = myUnit.camFollowTarget;
        cmBlendlist.Follow = myUnit.camFollowTarget;
    }

    public override void SkillCancelled()
    {
        HideSelectedSkillGridVisual();

        AudioManager.Instance.PlaySFX(SFXType.TabBack);
        //Go back to Orb List
        collectionManager.OpenItemMenu(player, false);
    }


}
