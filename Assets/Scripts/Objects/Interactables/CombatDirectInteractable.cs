
using Sirenix.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CombatDirectInteractable : Interact
{
    [Header("Combat Interaction")]
    [SerializeField] protected CombatDirectInteractableSkill interactableSkill;
    [Space(10)]
    [SerializeField] protected Transform validInteractionGridPositionsHeader;
    [Header("Combat UI")]
    [SerializeField] protected FadeUI combatInteractUI;
    [Header("Components")]
    [Tooltip("If this is attached to a dynamic obstacle, set it here")]
    [SerializeField] protected ARDynamicObstacle dynamicObstacle;

    //Caches 
    List<GridPosition> validInteractionGridPositions = new List<GridPosition>();

    protected override void Start()
    {
        base.Start();
        SetValidInteractionGridPositions();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if(dynamicObstacle)
            dynamicObstacle.TransformUpdated += SetValidInteractionGridPositions;
    }

    public override void HandleInteraction(bool inCombat)
    {
        if(!inCombat) { return; }

        TriggerInteractableSkill();
    }

    public void TriggerInteractableSkill()
    {
        CharacterGridUnit interactor = FantasyCombatManager.Instance.GetActiveUnit();
        interactableSkill.TriggerSkill(interactor, this);
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (dynamicObstacle)
            dynamicObstacle.TransformUpdated -= SetValidInteractionGridPositions;
    }

    public override bool IsCorrectPositionAndRotation(Transform interactor)
    {
        bool isCorrectRotation = base.IsCorrectPositionAndRotation(interactor);

        if (FantasyCombatManager.Instance.InCombat())
        {
            return isCorrectRotation && IsCorrectGridPosition(interactor);
        }

        return isCorrectRotation;
    }

    protected bool IsCorrectGridPosition(Transform interactor)
    {
        CharacterGridUnit unit = interactor.GetComponent<CharacterGridUnit>();

        if(!unit)
            return false;

        if (validInteractionGridPositions.IsNullOrEmpty())
            SetValidInteractionGridPositions();

        return validInteractionGridPositions.Intersect(unit.GetCurrentGridPositions()).Count() > 0;
    }

    protected void SetValidInteractionGridPositions()
    {
        validInteractionGridPositions.Clear();

        foreach(Transform child in validInteractionGridPositionsHeader)
        {
            GridPosition gridPosition = LevelGrid.Instance.gridSystem.GetGridPosition(child.position);
            validInteractionGridPositions.Add(gridPosition);
        }
    }

    public bool IsInteractionEnabled()
    {
        return interactionCollider.enabled;
    }

    public override void ShowInteractUI(bool show)
    {
        if (show && !InteractionManager.Instance.enableInteraction) { return; }

        if (FantasyCombatManager.Instance.InCombat())
        {
            combatInteractUI.Fade(show);
        }
        else
        {
            base.ShowInteractUI(show);
        }
    }

    public CombatDirectInteractableSkill GetInteractableSkill()
    {
        return interactableSkill;
    }
}
