using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;

public class ActionMenu : MonoBehaviour
{
    [SerializeField] Transform playerTransform;
    [SerializeField] TextMeshProUGUI attackText;
    [Header("Tactics")]
    [SerializeField] GameObject tacticAvailable;
    [SerializeField] GameObject tacticUsed;
    [Header("Animation Data")]
    [SerializeField] float deactiveThreshold = 8f;
    [SerializeField] float animationTime = 0.25f;

    //Caches 
    CanvasGroup canvasGroup;
    Transform camTransform;

    //Bools
    bool animating = false;
    bool deactivating = false;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        camTransform = Camera.main.transform;
    }

    private void OnEnable()
    {
        canvasGroup.alpha = 0;
        animating = false;
        deactivating = false;

        if (CalculateDistance() < deactiveThreshold)
        {
            animating = true;
            canvasGroup.DOFade(1, animationTime).OnComplete(() => AnimationComplete());
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(deactivating) { return; }

        if (CalculateDistance() > deactiveThreshold)
        {
            if(canvasGroup.alpha != 0 && !animating)
            {
                animating = true;
                canvasGroup.DOFade(0, animationTime).OnComplete(() => AnimationComplete());
            }
        }
        else
        {
            if(canvasGroup.alpha != 1 && !animating)
            {
                animating = true;
                canvasGroup.DOFade(1, animationTime).OnComplete(() => AnimationComplete()); ;
            }
        }

        AllowInteraction(FantasyCombatManager.Instance.GetIsCombatInteractionAvailable());
    }

    private void AllowInteraction(bool allow)
    {
        attackText.text = allow ? "Interact" : "Attack";
    }

    public void Enable(bool enable)
    {
        if (enable)
        {
            gameObject.SetActive(true);
        }
        else
        {
            deactivating = true;
            canvasGroup.DOFade(0, animationTime).OnComplete(() => DisableSelf());
        }
    }

    public void SwitchToTacticUsedMode(bool used)
    {
        tacticAvailable.SetActive(!used);
        tacticUsed.SetActive(used);
    }

    private void AnimationComplete()
    {
        animating = false;
    }

    private void DisableSelf()
    {
        gameObject.SetActive(false);
    }

    private float CalculateDistance()
    {
        Vector3 playerPosForDistance = new Vector3(playerTransform.position.x, 0, playerTransform.position.z);
        Vector3 camPosForDistance = new Vector3(camTransform.position.x, 0, camTransform.position.z);
        return Vector3.Distance(playerPosForDistance, camPosForDistance);
    }
}
