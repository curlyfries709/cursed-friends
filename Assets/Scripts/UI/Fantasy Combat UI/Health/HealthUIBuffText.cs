using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class HealthUIBuffText : FadeUI
{
    [SerializeField] float displayTime = 0.25f;
    [Header("Components")]
    [SerializeField] UnitHealthUI healthUI;
    [SerializeField] TextMeshProUGUI myText;
    [Header("Destination")]
    [SerializeField] Transform destination;

    private Vector3 origin;
    float totalTime;

    protected override void Awake()
    {
        base.Awake();
        origin = transform.localPosition;
        totalTime = displayTime + fadeOutTime + fadeInTime;
    }

    private void OnEnable()
    {
        StartCoroutine(DisplayRoutine());
        currentTween = transform.DOLocalMove(destination.localPosition, totalTime);
    }

    public void Setup(string text, Color textColor)
    {
        myText.text = text;
        myText.color = textColor;

        Fade(true);
    }

    IEnumerator DisplayRoutine()
    {
        yield return new WaitForSeconds(displayTime);
        Fade(false);
    }

    public override void FadeOutComplete()
    {
        base.FadeOutComplete();

        if(healthUI)
            healthUI.DisplayBuff();

        currentTween.Kill();
        transform.localPosition = origin;
    }
}
