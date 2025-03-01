using MoreMountains.Feedbacks;
using System.Collections.Generic;
using UnityEngine;

public class CombatDirectInteractableSkill : CombatInteractableBaseSkill
{
    [Header("Interactable Components")]
    [SerializeField] protected Transform interactableMoveTransform;
    [SerializeField] protected BoxCollider interactableGridCollider;
    [Space(10)]
    [SerializeField] protected MMF_Player skillFeedbackToPlay;

    //Interactor Data
    protected CharacterGridUnit myInteractor;
    protected Transform interactorMoveTransform;

    //Interactable Data
    CombatDirectInteractable myInteractable; 

    public void SetInteractorData(CharacterGridUnit interactor, Transform interactorMoveTransform)
    {
        myInteractor = interactor;
        this.interactorMoveTransform = interactorMoveTransform;
    }

    public virtual void TriggerSkill(CharacterGridUnit myInteractor, CombatDirectInteractable myInteractable)
    {
        this.myInteractable = myInteractable;
        this.myInteractor = myInteractor;

        BeginAction();

        skillFeedbackToPlay.PlayFeedbacks();
    }

    public override Dictionary<GridPosition, IHighlightable> ActivateHighlightedUI(bool activate, PlayerBaseSkill selectedBySkill)
    {
        if (activate)
            CalculateSelectedGridPos();

        return highlightableData;
    }

    public override void OnSkillInterrupted(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        throw new System.NotImplementedException();
    }
    //SETTERS
    protected override void SetRespawnable()
    {
        respawnable = myInteractable as IRespawnable;
    }

    //GETTERS
    protected override Transform GetDirectionTransform()
    {
        //Use interactor's direction
        return interactorMoveTransform;
    }

    protected override Transform GetSkillOwnerMoveTransform()
    {
        return interactableMoveTransform;
    }

    protected override BoxCollider GetSkillOwnerMoveGridCollider()
    {
        return interactableGridCollider;
    }


}
