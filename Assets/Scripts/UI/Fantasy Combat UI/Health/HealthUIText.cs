using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class HealthUIText : MonoBehaviour
{
    [SerializeField] float maxScale = 1f;
    [Header("Times")]
    [Range(1, 50)]
    [SerializeField] int transitionInPercentage = 25;
    [Range(1, 50)]
    [SerializeField] int transitionOutPercentage = 15;
    [Header("Delay")]
    [SerializeField] bool includeDelay = false;
    [SerializeField] float delayTime = 0.15f;
    [Header("Shake Variables")]
    [Tooltip("The shake strength.")]
    [SerializeField] float shakeStrength = 10f;
    [Tooltip("Indicates how much will the shake vibrate.")]
    [SerializeField] int shakeVibrato = 10;
    [Tooltip("Indicates how much the shake will be random (0 to 180 - values higher than 90 kind of suck, so beware). Setting it to 0 will shake along a single direction.")]
    [Range(1, 90)]
    [SerializeField] float shakeRandomness = 90f;
    [Tooltip("If TRUE the shake will automatically fadeOut smoothly within the tween's duration, otherwise it will not.")]
    [SerializeField] bool fadeOut = false;
    [Header("Transform & GOs")]
    [SerializeField] GameObject canvas;
    [SerializeField] Transform destination;
    

    private float transitionInTime;
    private float transitionOutTime;
    private float shakeTime;
    private float displayTime;

    private Vector3 origin;

    //Cache
    UnitHealthUI healthUI;

    private void Awake()
    {
        healthUI = canvas.GetComponent<UnitHealthUI>();
        origin = transform.localPosition;
    }

    private void OnEnable()
    {
        SetVariables();

        transform.localScale = Vector3.zero;

        if (includeDelay)
        {
            Invoke("TransitionIn", delayTime);
        }
        else
        {
            TransitionIn();
        }
        
        Invoke("TransitionOut", displayTime);
    }

    private void TransitionIn()
    {
        transform.DOScale(maxScale, transitionInTime);
        transform.DOShakePosition(shakeTime, shakeStrength, shakeVibrato, shakeRandomness, false, fadeOut);

        if (destination)
        {
            transform.DOLocalMove(destination.localPosition, displayTime);
        }
    }

    private void TransitionOut()
    {
        transform.DOScale(0, transitionOutTime);
        Invoke("Deactivate", transitionOutTime);
    }

    private void Deactivate()
    {
        transform.localPosition = origin;
        healthUI.FadeOutComplete();
        gameObject.SetActive(false);
    }

    private void SetVariables()
    {
        displayTime = FantasyCombatManager.Instance.GetSkillFeedbackDisplayTime();

        shakeTime = displayTime - transitionOutTime;
        transitionInTime = (transitionInPercentage / 100f) * displayTime;
        transitionOutTime = (transitionOutPercentage / 100f) * displayTime;
    }
}
