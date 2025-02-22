
public class EBackstabMultiplier : EnchantmentEffect
{
    protected override DamageModifier OnModifyDamageDealt(DamageData damageData)
    {
        if (damageData.isBackstab)
        {
            DamageModifier damageModifier = new DamageModifier();
            damageModifier.healthChangeMultiplier = numberValue;

            return damageModifier;
        }

        return base.OnModifyDamageDealt(damageData);
    }
}
