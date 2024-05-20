using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class VerticalHover : MonoBehaviour
{
    [SerializeField] float verticalMoveDistance = 5;
    [SerializeField] float animTime = 0.75f;

    Tween activeTween;
    float startHeight;

    private void Awake()
    {
        RectTransform rectTransform = transform as RectTransform;
        startHeight = rectTransform.anchoredPosition.y;
    }

    private void OnEnable()
    {
        RectTransform rectTransform = transform as RectTransform;

        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, startHeight);
        activeTween = rectTransform.DOAnchorPosY(rectTransform.anchoredPosition.y + verticalMoveDistance, animTime).SetUpdate(true).SetLoops(-1, LoopType.Yoyo);
    }

    private void OnDisable()
    {
        activeTween?.Kill();
    }
}
