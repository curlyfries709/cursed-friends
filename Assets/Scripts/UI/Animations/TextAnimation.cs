using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class TextAnimation : MonoBehaviour
{
    [Header("Scaling")]
    [SerializeField] float startScale;
    [SerializeField] float endScale = 1f;
    [Header("Times")]
    [SerializeField] float animDuration;
    [Header("Transitions")]
    [Range(1, 50)]
    [SerializeField] int transitionInPercentage = 25;
    [Range(1, 50)]
    [SerializeField] int transitionOutPercentage = 15;
    [Header("Conditions")]
    [SerializeField] bool deactivateSelfOnComplete = true;
    [Space(5)]
    [SerializeField] bool scaleOut = true;
    [SerializeField] bool fadeOut = false;
    [SerializeField] CanvasGroup componentToFade;
    [Header("Shake Variables")]
    [SerializeField] bool includeShake;
    [Tooltip("The shake strength.")]
    [SerializeField] float shakeStrength = 10f;
    [Tooltip("Indicates how much will the shake vibrate.")]
    [SerializeField] int shakeVibrato = 10;
    [Tooltip("Indicates how much the shake will be random (0 to 180 - values higher than 90 kind of suck, so beware). Setting it to 0 will shake along a single direction.")]
    [Range(1, 90)]
    [SerializeField] float shakeRandomness = 90f;
    [Tooltip("If TRUE the shake will automatically fadeOut smoothly within the tween's duration, otherwise it will not.")]
    [SerializeField] bool fadeOutShake = false;
    [Header("Transform & GOs")]
    [SerializeField] Transform startingPoint;
    public Transform destination;


    private float transitionInTime;
    private float transitionOutTime;
    private float shakeTime;


    private void OnEnable()
    {
        SetVariables();

        TransitionIn();

        Invoke("TransitionOut", animDuration);
    }

    private void TransitionIn()
    {
        transform.DOScale(endScale, transitionInTime);

        if (includeShake)
        {
            transform.DOShakePosition(shakeTime, shakeStrength, shakeVibrato, shakeRandomness, false, fadeOutShake);
        }
        
        if (destination)
        {
            transform.DOLocalMove(destination.localPosition, animDuration);
        }
    }

    private void TransitionOut()
    {
        if (scaleOut)
        {
            transform.DOScale(0, transitionOutTime);
        }

        if (fadeOut && componentToFade)
        {
            componentToFade.DOFade(0, transitionOutTime);
        }
        
        Invoke("Deactivate", transitionOutTime);
    }

    private void Deactivate()
    {
        if (!deactivateSelfOnComplete) { return; }

        gameObject.SetActive(false);
        if(componentToFade)
            componentToFade.alpha = 1;
    }

    private void SetVariables()
    {
        transform.localScale = new Vector3(startScale, startScale, startScale);

        if (startingPoint)
        {
            transform.localPosition = startingPoint.localPosition;
        }
            
        shakeTime = animDuration - transitionOutTime;

        transitionInTime = (transitionInPercentage / 100f) * animDuration;
        transitionOutTime = (transitionOutPercentage / 100f) * animDuration;
    }
}
