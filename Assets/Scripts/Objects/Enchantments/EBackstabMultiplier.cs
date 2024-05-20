
public class EBackstabMultiplier : EnchantmentEffect
{
    protected override DamageReceivedAlteration OnAlterDamageReductionAttack(bool isBackstab)
    {
        if (isBackstab)
        {
            //Number Value is Multiplier
            return new DamageReceivedAlteration(numberValue);
        }

        return new DamageReceivedAlteration(1);
    }
}
