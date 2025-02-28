using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class FiredUpEvent : MonoBehaviour, ITurnStartEvent
{
    [SerializeField] CombatEventCanvas firedUpCanvas;
    [SerializeField] FadeUI flasher;
    [Space(5)]
    [SerializeField] float firedUpDuration = 1.55f;
    [SerializeField] float flashDuration = 0.1f;

    //Cache
    CharacterGridUnit firedUpUnit;

    //Event
    public static Action<CharacterGridUnit> UnitFiredUp;

    public int turnStartEventOrder { get; set; } = 5;

    private void Awake()
    {
        firedUpCanvas.SetDuration(firedUpDuration);
    }

    private void OnEnable()
    {
        FantasyCombatManager.Instance.OnNewTurn += OnNewTurn;
    }

    public void PlayTurnStartEvent()
    {
        UnitFiredUp?.Invoke(firedUpUnit);

        StatusEffectManager.Instance.UnitFiredUp(firedUpUnit);

        StartCoroutine(UIRoutine());
    }

    private void OnNewTurn(CharacterGridUnit firedUpUnit, int turnNumber)
    {
        if(!firedUpUnit.CharacterHealth().isFiredUp && firedUpUnit.CharacterHealth().CanTriggerFiredUp())
        {
            this.firedUpUnit = firedUpUnit;
            FantasyCombatManager.Instance.AddTurnStartEventToQueue(this);
        }
    }

    IEnumerator UIRoutine()
    {
        flasher.Fade(true);
        yield return new WaitForSeconds(flashDuration);
        firedUpCanvas.gameObject.SetActive(true);
        firedUpUnit.GetPhotoShootSet().PlayFiredUpUI();
        yield return new WaitForSeconds(firedUpDuration);
        OnFiredUpComplete();
    }

    private void OnFiredUpComplete()
    {
        firedUpUnit.GetPhotoShootSet().DeactivateSet();
        (this as ITurnStartEvent).EventComplete();
    }

    private void OnDisable()
    {
        FantasyCombatManager.Instance.OnNewTurn -= OnNewTurn;
    }

}
