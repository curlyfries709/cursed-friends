using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Status Effect", menuName = "Configs/Status Effect", order = 5)]
public class StatusEffectData : ScriptableObject
{
    [Header("Special & Abnormal Effect Behaviour")]
    [Tooltip("E.G: Overburdened; Drunk")]
    public bool canBeAppliedOutsideCombat;
    [Space(10)]
    public bool preventsUnitFromActing;
    public bool hasTurnEndEffect;
    public bool canBeGuarded;
    [Space(5)]
    public bool banFPGain;
    public bool loseFPEachTurn;

    [Header("Stat Buff And Debuff Data")]
    public bool isStatBuffOrDebuff;
    public string buffNickname;

    [Header("Visual")]
    [Tooltip("The UI Prefab")]
    public GameObject effectVisualPrefab;
    [Space(10)]
    [Tooltip("The VFX Prefab to display on affected Unit")]
    public GameObject unitVFXPrefab;
    [Tooltip("Where on the unit to place the VFX")]
    public BodyPart unitVFXBodyPart;
}
