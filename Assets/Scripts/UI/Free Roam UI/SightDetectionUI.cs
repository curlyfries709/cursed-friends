using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SightDetectionUI : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] GameObject sightDetection;
    [SerializeField] Image sightDetectionFG;
    [Header("Alerts")]
    [SerializeField] GameObject exclamation;
    [SerializeField] GameObject question;
    [Header("Values")]
    [SerializeField] float exclamationStartScale = 0.6f;
    [Header("Timers")]
    [SerializeField] float exclamationAnimationTime;
    [SerializeField] float exclamationDisplayTime;
    [SerializeField] float questionAnimationTime;
    [Header("Destinations")]
    [SerializeField] Transform exclamationDestination;
    [SerializeField] Transform questionDestination;


    Vector3 exclamationOrigin;
    Vector3 questionOrigin;
    Tween myTween = null;


    private void Awake()
    {
        exclamation.SetActive(true);

        exclamationOrigin = exclamation.transform.localPosition;
        questionOrigin = question.transform.localPosition;

        exclamation.transform.localScale = Vector3.zero;
    }

    public void Alert()
    {
        //Play SFX
        AudioManager.Instance.PlaySFX(SFXType.EnemyAlert);

        //Start Routine
        StartCoroutine(AlertRoutine());
    }

    public void UpdateSightDetection(float newValue)
    {
        ResetLooking();

        bool activateIcon = newValue > 0 ? true : false;

        if (activateIcon && !sightDetection.activeInHierarchy)
            AudioManager.Instance.PlaySFX(SFXType.EnemySeeing);

        sightDetection.SetActive(activateIcon);
        sightDetectionFG.fillAmount = newValue;
    }

    public void ResetSightDetection()
    {
        sightDetection.SetActive(false);
        sightDetectionFG.fillAmount = 0;
    }

    public void Looking(bool isLooking)
    {
        if (isLooking)
        {
            //Play SFX
            AudioManager.Instance.PlaySFX(SFXType.EnemySearch);

            //Activate UI
            question.SetActive(true);

            question.transform.localScale = new Vector3(exclamationStartScale, exclamationStartScale, exclamationStartScale);
            question.transform.localPosition = questionOrigin;

            question.transform.DOScale(1f, exclamationAnimationTime);
            myTween = question.transform.DOLocalMoveY(questionDestination.localPosition.y, questionAnimationTime).SetLoops(7, LoopType.Yoyo);
        }
        else
        {
            ResetLooking();
        }
    }

    private void ResetLooking()
    {
        question.SetActive(false);

        if (myTween != null)
        {
            myTween.Kill();
            myTween = null;
        }
    }


    IEnumerator AlertRoutine()
    {
        exclamation.transform.localPosition = exclamationOrigin;
        exclamation.transform.localScale = new Vector3(exclamationStartScale, exclamationStartScale, exclamationStartScale);
        exclamation.transform.DOLocalMove(exclamationDestination.localPosition, exclamationAnimationTime);
        exclamation.transform.DOScale(1.2f, exclamationAnimationTime);
        yield return new WaitForSeconds(exclamationDisplayTime);
        exclamation.transform.localScale = Vector3.zero;
    }


}
