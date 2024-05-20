using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using System;


public class StrategicBonusUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI bonusName;
    [SerializeField] TextMeshProUGUI bonusAmount;

    public void Setup(string bName, string bAmount)
    {
        bonusName.text = bName;
        bonusAmount.text = bAmount;
    }
}
