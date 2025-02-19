using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthModifier
{

}


public class AttackData
{
    public CharacterGridUnit attacker;
    public Element attackElement;
    public Item attackIngredient; //Update to attackItem

    public SkillForceData forceData;
    public List<InflictedStatusEffectData> inflictedStatusEffects;

    public int damage;
    public int numOfTargets;

    public bool isCritical;
    public bool canEvade;

    public AttackData() { }
    public AttackData(CharacterGridUnit attacker, Element attackElement, int damage, bool isCritical, List<InflictedStatusEffectData> inflictedStatusEffects, SkillForceData forceData, int numOfTargets)
    {
        this.attacker = attacker;
        this.attackElement = attackElement;
        this.damage = damage;
        this.isCritical = isCritical;
        this.inflictedStatusEffects = inflictedStatusEffects;
        this.forceData = forceData;
        this.numOfTargets = numOfTargets;

        attackIngredient = null;
        canEvade = true;
    }
}

public struct DamageData
{
    public GridUnit target;
    public CharacterGridUnit attacker;

    public Affinity affinityToAttack;
    public AttackData hitByAttackData;
    public int damageReceived;

    //Bools
    public bool isBackstab;
    public bool isCritical;
    public bool isKOHit;
    public bool isGuarding;
    public bool isKnockbackDamage;

    public DamageData(GridUnit target, CharacterGridUnit attacker, Affinity affinityToAttack, int damageReceived)
    {
        this.target = target;
        this.attacker = attacker;

        this.affinityToAttack = affinityToAttack;
        this.damageReceived = damageReceived;

        hitByAttackData = new AttackData();

        isBackstab = false;
        isCritical = false;
        isKOHit = false;
        isGuarding = false;
        isKnockbackDamage = false;
    }
}

