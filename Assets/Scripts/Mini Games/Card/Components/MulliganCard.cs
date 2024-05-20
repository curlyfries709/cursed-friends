using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MulliganCard : CardComponent
{
    public override void OnHover()
    {
        SetTooltip();
    }

    public override void OnSelect()
    {
        CardGameManager.Instance.MulliganCard(transform.GetSiblingIndex());
    }
}
