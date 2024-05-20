using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
[RequireComponent(typeof(CanvasGroup))]
public class FadeUI : MonoBehaviour
{
    [Header("Behaviour")]
    [SerializeField] bool shouldFlash;
    [Range(0.1f, 1)]
    [SerializeField] float maxAlpha = 1;
    [Header("Transition times")]
    public float fadeInTime = 0.25f;
    public float fadeOutTime = 0.25f;

    protected bool fadingIn = false;

    //Cache
    public CanvasGroup canvasGroup { get; private set; }
    protected Action fadeCompleteCallback = null;

    protected Tween currentTween;

    protected virtual void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }


    public void Fade(bool show, Action fadeCompleteCallback = null)
    {
        this.fadeCompleteCallback = fadeCompleteCallback;

        SetCanvasGroup();

        if (show && !gameObject.activeInHierarchy)
        {
            fadingIn = true;
            gameObject.SetActive(true);
            canvasGroup.alpha = 0;
            currentTween = canvasGroup.DOFade(maxAlpha, fadeInTime).SetUpdate(true).OnComplete(() => FadeInComplete());
        }
        else if(!show && gameObject.activeInHierarchy)
        {
            fadingIn = false;
            currentTween = canvasGroup.DOFade(0, fadeOutTime).SetUpdate(true).OnComplete(() => FadeOutComplete());
        }
    }

    private void FadeInComplete()
    {
        currentTween = null;
        fadeCompleteCallback?.Invoke();

        if (!shouldFlash) { return; }
        Fade(false);
    }

    public virtual void FadeOutComplete()
    {
        currentTween = null;
        fadeCompleteCallback?.Invoke();
        gameObject.SetActive(false);
    }

    private void SetCanvasGroup()
    {
        if (!canvasGroup)
            canvasGroup = GetComponent<CanvasGroup>();
    }
}
