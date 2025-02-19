

public class ECritCounter : EnchantmentEffect
{
    protected override DamageReceivedModifier OnAlterDamageReductionAttack(bool isBackstab)
    {
        if (Evade.Instance.counterAttacker == owner)
        {
            DamageReceivedModifier damageReceivedAlteration = new DamageReceivedModifier(1);
            damageReceivedAlteration.isCritical = true;

            return new DamageReceivedModifier(1);
        }

        return new DamageReceivedModifier(1);
    }
}
