
public class EBackstabMultiplier : EnchantmentEffect
{
    protected override DamageReceivedModifier OnAlterDamageReductionAttack(bool isBackstab)
    {
        if (isBackstab)
        {
            //Number Value is Multiplier
            return new DamageReceivedModifier(numberValue);
        }

        return new DamageReceivedModifier(1);
    }
}
