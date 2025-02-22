

public class ERegeneration : EnchantmentEffect, ITurnStartEvent
{
    public int turnStartEventOrder { get; set; } = 15;

    public void PlayTurnStartEvent()
    {
        HealData healData = new HealData(owner, GetPercentageAsValue(owner.stats.Vitality));

        owner.Health().Heal(healData);
        IDamageable.RaiseHealthChangeEvent(true);
    }

    protected override void OnNewTurn(CharacterGridUnit actingUnit, int turnNumber)
    {
        //We use On New Turn instead of on Owner turn start because On New Turn gets triggered before Turn Start events are checked.
        base.OnNewTurn(actingUnit, turnNumber);
        if (actingUnit == owner && !owner.Health().isKOed)
            FantasyCombatManager.Instance.AddTurnStartEventToQueue(this);
    }

}
