
using UnityEngine;

public class CombatDirectInteract : RespawnableInteract
{
    [Header("Combat Interaction")]
    [SerializeField] protected GridUnit myUnit;
    [SerializeField] protected CombatInteractableBaseSkill interactableSkill;
    [Space(10)]
    [Tooltip("If it can be destroyed, then respawn behaviour will be set for it")]
    [SerializeField] bool canBeDestroyed = true;

    protected override void OnEnable()
    {
        base.OnEnable();

        if(canBeDestroyed)
            Health.UnitKOed += OnUnitKO;
    }
    public override void HandleInteraction(bool inCombat)
    {
        if(!inCombat) { return; }

        CharacterGridUnit interactor = FantasyCombatManager.Instance.GetActiveUnit();

        interactableSkill.TriggerSkill(interactor);
    }

    private void OnUnitKO(GridUnit unit)
    {
        if(unit != myUnit) { return; }

        OnRemovedFromRealm(false);
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (canBeDestroyed)
            Health.UnitKOed -= OnUnitKO;
    }
}
