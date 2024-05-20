using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattlefieldRow : CardComponent
{
    [SerializeField] CardGameManager.BattleRow battleRow;

    public override void OnHover()
    {
        //Do Nothing
    }

    public override void OnSelect()
    {
        
    }

    public CardGameManager.BattleRow GetBattleRow()
    {
        return battleRow;
    }
}
