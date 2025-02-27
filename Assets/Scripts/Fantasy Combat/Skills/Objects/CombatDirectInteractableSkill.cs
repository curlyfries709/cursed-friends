using MoreMountains.Feedbacks;
using UnityEngine;

public class CombatDirectInteractableSkill : CombatInteractableBaseSkill
{
    [Header("Interactable Components")]
    [SerializeField] protected Transform interactableMoveTransform;
    [SerializeField] protected BoxCollider interactableGridCollider;
    [Space(10)]
    [SerializeField] protected MMF_Player skillFeedbackToPlay;

    protected Transform interactorMoveTransform;
    CombatDirectInteractable myInteractable; 

    public void SetInteractorData(CharacterGridUnit interactor, Transform interactorMoveTransform)
    {
        myInteractor = interactor;
        this.interactorMoveTransform = interactorMoveTransform;
    }

    public void TriggerSkill(CharacterGridUnit myInteractor, CombatDirectInteractable myInteractable)
    {
        this.myInteractable = myInteractable;
        this.myInteractor = myInteractor;

        BeginAction();

        TriggerSkill(myInteractor);
    }

    protected override void TriggerSkill(CharacterGridUnit myInteractor)
    {
        skillFeedbackToPlay.PlayFeedbacks();
    }

    public void OnInteractableDestroyed()
    {
        if (myInteractable is RespawnableCombatDirectInteractable respawnable)
        {
            respawnable.OnInteractableDestroyed();
        }
    }

    public override void OnSkillInterrupted(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        throw new System.NotImplementedException();
    }

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
