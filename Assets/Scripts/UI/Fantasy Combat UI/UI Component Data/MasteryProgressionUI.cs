using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;
using System;


public class MasteryProgressionUI : MonoBehaviour
{
    [Header("Title")]
    [SerializeField] TextMeshProUGUI masteryName;
    [SerializeField] TextMeshProUGUI masteryBenchmarkNum;
    [Space(10)]
    [SerializeField] string textToAppendToTitle;
    [Header("Values")]
    [SerializeField] TextMeshProUGUI masteryCurrentCount;
    [SerializeField] Image wedgeBar;
    [Space(10)]
    [SerializeField] float animationTime = 0.25f;

    int newCount;
    float barEndValue;

    public void Setup(string mName, int mBenchmark, int mCount, int newCount)
    {
        masteryName.text = mName + textToAppendToTitle; 
        masteryBenchmarkNum.text = mBenchmark.ToString();

        masteryCurrentCount.text = mCount.ToString();

        this.newCount = newCount;
        wedgeBar.fillAmount = (float)mCount / mBenchmark;

        barEndValue = (float)newCount / mBenchmark;
    }

    public void PlayAnimation()
    {
        int startCount = Int32.Parse(masteryCurrentCount.text);
        wedgeBar.DOFillAmount(barEndValue, animationTime);
        DOTween.To(() => startCount, x => startCount = x, newCount, animationTime).OnUpdate(() => masteryCurrentCount.text = startCount.ToString());
    }
   
}
