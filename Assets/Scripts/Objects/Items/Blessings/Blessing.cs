using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Blessing", menuName = "Item/Blessing", order = 1)]
public class Blessing : Item
{
    [Header("Bless Data")]
    [Tooltip("Name to display during Bless UI")]
    public string nickname;
    [Tooltip("Shortened description to display on Combat HUD")]
    public string hudDescription;
    [Space(10)]
    public Color iconColor = Color.white;
    [Header("Duration & Cooldown")]
    [Tooltip("Number of turns effect should last")]
    public int duration = 4;
    public int cooldownAtWisLv1 = 5;
    [Header("Stat Manipulation")]
    [Range(0, 100)] public int hpIncrease = 0;
    [Range(0, 100)] public int spIncrease = 0;
    [Space(5)]
    [Range(0, 100)] public int maxHpIncrease = 0;
    [Range(0, 100)] public int spCostReduction = 0;
    [Space(10)]
    [Range(0, 100)] public int damageDealtIncrease = 0;
    [Range(0, 100)] public int damageReceivedReduction = 0;
    [Space(10)]
    [Range(0, 100)] public int techniqueIncrease = 0;
    [Range(0, 100)] public int evasionIncrease = 0;
    [Space(10)]
    [Range(0, 100)] public int speedIncrease = 0;
    [Tooltip("Value is added to the unit's move range")]
    [Range(0, 5)] public int movementIncrease = 0;
    [Space(10)]
    [Range(0, 100)] public int healIncrease = 0;
    [Tooltip("Number of turns to increase Effects for")]
    [Range(0, 10)] public int statusEffectsDuration = 0;
    [Space(10)]
    [Range(0, 100)] public int critChanceIncrease = 0;
    [Header("Affinity Manipulation")]
    public List<ElementAffinity> elementAffinitiesToAlter;
    [Header("STATUS EFFECTS & BUFFS")]
    public List<ChanceOfInflictingStatusEffect> statusEffectsToApply;
    [Header("OTHER ACTIVATE EFFECTS")]
    [Tooltip("Accelerate all players on activate")]
    public bool accelerate = false;
    [Tooltip("Should charged attacks be triggered immediately?")]
    public bool removeChargeTime = false;
    


    public int GetMaxHealth()
    {
        return cooldownAtWisLv1 * TheCalculator.Instance.GetCooldownHealRate();
    }
}
