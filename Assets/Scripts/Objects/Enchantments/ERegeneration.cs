

public class ERegeneration : EnchantmentEffect, ITurnStartEvent
{
    public int turnStartEventOrder { get; set; } = 15;

    public void PlayTurnStartEvent()
    {
        owner.Health().Heal(GetPercentageAsValue(owner.stats.Vitality), 0, true, true);
    }

    protected override void OnNewTurn(CharacterGridUnit actingUnit, int turnNumber)
    {
        //We use On New Turn instead of on Owner turn start because On New Turn gets triggered before Turn Start events are checked.
        base.OnNewTurn(actingUnit, turnNumber);
        if (actingUnit == owner && !owner.Health().isKOed)
            FantasyCombatManager.Instance.AddTurnStartEventToQueue(this);
    }

}
