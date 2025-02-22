using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Reflect : MonoBehaviour, ITurnEndEvent
{
    [Header("Timers")]
    [SerializeField] float startRoutineDelay = 0.28f;
    [SerializeField] float timeToShakeCam = 0.04f;
    [SerializeField] float timeToDisplayDamage = 0.09f;

    //Cached Variables
    public int turnEndEventOrder { get; set; }

    CharacterGridUnit damageReceiver;
    AttackData damageData;

    //Event
    public static Action<CharacterGridUnit, CharacterGridUnit, AttackData> DamageReflected;

    private void Awake()
    {
        turnEndEventOrder = transform.GetSiblingIndex();
    }

    private void OnEnable()
    {
        DamageReflected += OnDamageReflect;
    }

    public void OnDamageReflect(CharacterGridUnit yourAttacker, CharacterGridUnit reflector, AttackData damageData)
    {
        this.damageData = damageData;
        damageReceiver = yourAttacker;
        damageData.attacker = reflector;

        FantasyCombatManager.Instance.AddTurnEndEventToQueue(this);
    }

    public void PlayTurnEndEvent()
    {
        damageReceiver.unitAnimator.IdleBeforeReflect();

        damageReceiver.Health().TakeDamage(damageData, DamageType.Reflect);
        StatusEffectManager.Instance.PlayDamageTurnEndEvent(damageReceiver);

        //Due to various animations that play based on receiver's affinities, we're do this via a coroutine than animation events.
        //StartCoroutine(ReflectRoutine());
    }

    public void OnEventCancelled()
    {
        //Event Cannot be cancelled so do nothing
        Debug.LogError("REFLECT EVENT CANCELLED!");
    }

    IEnumerator ReflectRoutine()
    {
        yield return new WaitForSeconds(timeToShakeCam + startRoutineDelay);
        StatusEffectManager.Instance.ShakeCam();
        yield return new WaitForSeconds(timeToDisplayDamage - timeToShakeCam);
        IDamageable.RaiseHealthChangeEvent(true);
    }

    private void OnDisable()
    {
        DamageReflected -= OnDamageReflect;
    }

    public List<Type> GetEventTypesThatCancelThis()
    {
        return new List<Type>();
    }
}
