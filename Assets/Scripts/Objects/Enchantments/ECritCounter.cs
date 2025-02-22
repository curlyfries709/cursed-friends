

public class ECritCounter : EnchantmentEffect
{
    protected override DamageModifier OnModifyDamageDealt(DamageData damageData)
    {
        if (Evade.Instance.counterAttacker == owner)
        {
            DamageModifier damageModifier = new DamageModifier();
            damageModifier.isCrit = new HealthModifier.Modifier<bool>(true, HealthModifier.Priority.High);

            return damageModifier;
        }

        return base.OnModifyDamageDealt(damageData);
    }
}
