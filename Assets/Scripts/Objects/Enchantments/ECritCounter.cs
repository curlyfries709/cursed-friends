

public class ECritCounter : EnchantmentEffect
{
    protected override DamageReceivedAlteration OnAlterDamageReductionAttack(bool isBackstab)
    {
        if (Evade.Instance.counterAttacker == owner)
        {
            DamageReceivedAlteration damageReceivedAlteration = new DamageReceivedAlteration(1);
            damageReceivedAlteration.isCritical = true;

            return new DamageReceivedAlteration(1);
        }

        return new DamageReceivedAlteration(1);
    }
}
