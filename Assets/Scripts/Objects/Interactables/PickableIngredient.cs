
using UnityEngine;
using MoreMountains.Feedbacks;

public class PickableIngredient : RespawnableInteract
{
    [Header("Ingredient")]
    [SerializeField] Ingredient ingredient;
    [Space(10)]
    [SerializeField] MMF_Player pickupFeedback;

    public override void HandleInteraction(bool inCombat)
    {
        if (inCombat) { return; }
        OnRemovedFromRealm(true);
    }

    protected override void OnRemovedFromRealm(bool deactiveImmediately)
    {
        pickupFeedback?.PlayFeedbacks();

        InventoryManager.Instance.AddToInventory(PartyManager.Instance.GetLeader(), ingredient);

        base.OnRemovedFromRealm(deactiveImmediately);
    }
}
