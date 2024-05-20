using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using MoreMountains.Feedbacks;

public class CombatEventCanvas : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] MMF_Player feedback;
    [Header("Timers")]
    [SerializeField] float fadeInTime = 0.15f;
    [SerializeField] float fadeOutTime = 0.15f;

    bool canDeactivateSelf = true;
    float duration = 0;

    private void OnEnable()
    {
        if(duration > 0)
        {
            StartCoroutine(DisplayRoutine());
        }
            
    }

    IEnumerator DisplayRoutine()
    {
        Show(true);
        yield return new WaitForSeconds(duration);
        Show(false);
    }

    public void Show(bool show)
    {
        if (show)
        {
            canvasGroup.alpha = 0;
            gameObject.SetActive(true);
            feedback?.PlayFeedbacks();
            canvasGroup.DOFade(1, fadeInTime);
        }
        else
        {
            canvasGroup.DOFade(0, fadeOutTime).OnComplete(() => Deactivate());
        } 
    }

    private void Deactivate()
    {
        if(canDeactivateSelf)
            gameObject.SetActive(false);

        feedback?.StopFeedbacks();
    }

    public void Setup(float duration, bool canDeactivateSelf)
    {
        this.duration = duration;
        this.canDeactivateSelf = canDeactivateSelf;
    }

    public void SetDuration(float value)
    {
        duration = value;
    }


}
