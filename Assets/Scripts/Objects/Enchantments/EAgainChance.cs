using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EAgainChance : EnchantmentEffect
{
    protected override void OnUnitTurnStart()
    {
        base.OnUnitTurnStart();

        if (!owner.CharacterHealth().isFiredUp)
        {
            int randNum = Random.Range(0, 101);

            if (randNum <= percentageValue)
            {
                Again.Instance.SetUnitToGoAgain(owner);
            }
        }
    }
}
