using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Cinemachine;
using System.Collections;
using System;


public struct InflictedStatusEffectData
{
    public StatusEffectData effectData;
    public CharacterGridUnit target;
    public int numOfTurns;
    public int buffChange;

    public InflictedStatusEffectData(StatusEffectData effectData, CharacterGridUnit target, int numOfTurns, int buffChange)
    {
        this.effectData = effectData;
        this.target = target;
        this.numOfTurns = numOfTurns;
        this.buffChange = buffChange;
    }
}

public class StatusEffectManager : CombatAction
{
    public static StatusEffectManager Instance { get; private set; }

    [Header("Pool")]
    [SerializeField] Transform vfxPool;
    [Header("Buffs & Debuffs")]
    [SerializeField] float buffMultiplier = 1.25f;
    [SerializeField] float superBuffMultiplier = 1.5f;
    [Space(10)]
    [SerializeField] float debuffMultiplier = 0.75f;
    [SerializeField] float superDebuffMultiplier = 0.5f;
    [Header("Colors")]
    [SerializeField] Color buffColor;
    [SerializeField] Color superBuffColor;
    [Space(10)]
    [SerializeField] Color debuffColor;
    [SerializeField] Color superDebuffColor;
    [Header("Camera Settings")]
    [SerializeField] CinemachineVirtualCamera disablingStatusEffectCam;
    [SerializeField] CinemachineVirtualCamera turnEndStatusEffectCam;
    [SerializeField] float orbitPointRotationSpeed;
    [Header("Orbit Point Rotation Settings")]
    [SerializeField] Vector3 orbitPointStartingRotation;
    [Header("Timers")]
    [SerializeField] float showDisabledUnitDelay = 0.5f;
    [SerializeField] float unitDisabledDisplayTime = 1f;
    [Space(5)]
    [SerializeField] float turnEndEffectDelay = 0.25f;
    [Header("Keys Effects")]
    [SerializeField] StatusEffectData knockdown;
    [SerializeField] StatusEffectData firedUp;
    [Space(5)]
    [SerializeField] StatusEffectData overburdened;
    [SerializeField] StatusEffectData wounded;
    [Header("Buff Effects")]
    [SerializeField] List<StatusEffectData> allStatusBuffs = new List<StatusEffectData>();

    //Storage
    Dictionary<string, int> vfxPoolDict = new Dictionary<string, int>();

    //Status Effect Dict
    Dictionary<CharacterGridUnit, List<InflictedStatusEffectData>> statusEffectsAtBattleStart = new Dictionary<CharacterGridUnit, List<InflictedStatusEffectData>>();

    Dictionary<CharacterGridUnit, List<StatusEffect>> combatUnitStatusEffects = new Dictionary<CharacterGridUnit, List<StatusEffect>>();

    Dictionary<CharacterGridUnit, List<StatusEffect>> newlyAppliedEffects = new Dictionary<CharacterGridUnit, List<StatusEffect>>();
    Dictionary<CharacterGridUnit, List<InflictedStatusEffectData>> newlyStackedEffects = new Dictionary<CharacterGridUnit, List<InflictedStatusEffectData>>();

    Transform orbitPoint;
    CinemachineImpulseSource impulseSource;

    //CONSTANT
    const int numOfTurnAverage = 3;

    //Event
    public Action<CharacterGridUnit, GridUnit, StatusEffectData> Unitafflicted; //Parameters: Target, Attacker, StatusEffectData

    private void Awake()
    {
        Instance = this;
        disablingStatusEffectCam.gameObject.SetActive(false);
        impulseSource = turnEndStatusEffectCam.GetComponent<CinemachineImpulseSource>();
    }

    private void OnEnable()
    {
        Health.UnitKOed += OnUnitKO;
        Flee.UnitFled += OnUnitKO;
        FantasyCombatManager.Instance.CombatEnded += OnCombatEnd;

        PartyManager.Instance.PlayerPartyDataSet += IntializePlayerStatusEffects;
    }

    private void IntializePlayerStatusEffects()
    {
        foreach (PlayerGridUnit player in PartyManager.Instance.GetAllPlayerMembersInWorld())
        {
            IntializeUnitStatusEffects(player);
        }
    }

    private void Update()
    {
        if (disablingStatusEffectCam.gameObject.activeInHierarchy)
        {
            orbitPoint.Rotate(Vector3.down * orbitPointRotationSpeed * Time.deltaTime);
        }
    }

    public bool ApplyAndActivateStatusEffect(StatusEffectData effectData, CharacterGridUnit unit, GridUnit inflictor, int numOfTurns = numOfTurnAverage, int buffChange = 0, bool isFiredUpBuff = false)
    {
        bool wasApplied = ApplyStatusEffect(effectData, unit, inflictor, numOfTurns, buffChange, isFiredUpBuff);

        if (wasApplied)
        {
            TriggerNewlyAppliedEffects(unit);
        }

        return wasApplied;
    }

    public bool ApplyStatusEffect(StatusEffectData effectData, CharacterGridUnit unit, GridUnit inflictor, int numOfTurns = numOfTurnAverage, int buffChange = 0, bool isFiredUpBuff = false)
    {
        System.Type statusEffectType = System.Type.GetType(effectData.name);

        bool canApply = CanApplyStatusEffect(unit, effectData);

        if (!canApply)
        {
            return false;
        }

        //Check If Status Effect already applied. 
        if (combatUnitStatusEffects[unit].Any((effect) => effect.GetType().ToString() == effectData.name))
        {
            //Means status effect is already applied. 
            InflictedStatusEffectData newData = new InflictedStatusEffectData(effectData, unit, numOfTurns, buffChange);
            newlyStackedEffects[unit].Add(newData);
        }
        else
        {
            //Not already applied.
            GameObject header = unit.CharacterHealth().GetStatusEffectHeader();

            StatusEffect newStatusEffect = header.AddComponent(statusEffectType) as StatusEffect;

            newStatusEffect.Setup(unit, inflictor, effectData, numOfTurns, buffChange, isFiredUpBuff);

            combatUnitStatusEffects[unit].Add(newStatusEffect);
            newlyAppliedEffects[unit].Add(newStatusEffect);
        }

        return canApply;
    }

    //SE SPECIFIC METHODS
    public void UnitKnockedDown(CharacterGridUnit affectedUnit, bool shouldActivate = false)
    {
        if (!IsUnitKnockedDown(affectedUnit))
        {
            if (shouldActivate)
            {
                ApplyAndActivateStatusEffect(knockdown, affectedUnit, null, 1);
            }
            else
            {
                ApplyStatusEffect(knockdown, affectedUnit, null, 1);
            }
        } 
    }

    public void UnitOverburdened(CharacterGridUnit affectedUnit, bool isOverburdened)
    {
        bool hasStatusEffect = UnitHasStatusEffect(affectedUnit, overburdened);

        if (isOverburdened && !hasStatusEffect)
        {
            ApplyStatusEffect(overburdened, affectedUnit, null, 10);
            ActivateEffects(affectedUnit);
        }
        else if(!isOverburdened && hasStatusEffect)
        {
            CureStatusEffect(affectedUnit, overburdened);
        }  
    }

    public void UnitFiredUp(CharacterGridUnit unit)
    {
        ApplyAllBuffs(unit, 1, true);
        ApplyStatusEffect(firedUp, unit, null);
        ActivateEffects(unit);
    }

    public void RecoverFromKnockdown(CharacterGridUnit affectedUnit)
    {
        CureStatusEffect(affectedUnit, knockdown);
    }

    public void LoseFiredUp(CharacterGridUnit affectedUnit)
    {
        //Called by Knockdown because, this losses FIred Up!
        CureStatusEffect(affectedUnit, firedUp);
    }

    public void TrippedFromBlinded(CharacterGridUnit trippedUnit)
    {
        UnitKnockedDown(trippedUnit);
        ActivateEffects(trippedUnit);
        ShowUnitAfflictedByStatusEffect(trippedUnit);
    }

    public void ApplyAllBuffs(CharacterGridUnit unit, int buffValue, bool firedUpBuffs)
    {
        foreach (StatusEffectData buff in allStatusBuffs)
        {
            StatusEffect buffScript = combatUnitStatusEffects[unit].FirstOrDefault((effect) => effect.GetType().ToString() == buff.name);
            int buffChange = buffValue;
            //Check If Status Effect already applied. 
            //Fired Up removes all Stat Debuffs...Instead of destroying SEs & Readding. Updating Bufff Value is cheaper.
            if (buffScript && firedUpBuffs && buffScript.GetCurrentBuffValue() < 0)
            {
                //Required Buff Change
                //Current Buff + x = buffValue
                //x = buffValue - current

                int SEBuffValue = buffScript.GetCurrentBuffValue();
                buffChange = buffValue - SEBuffValue;
            }

            ApplyStatusEffect(buff, unit, unit, 3, buffChange, firedUpBuffs);
        }
    }

    //Display Routine
    public void ShowUnitAfflictedByStatusEffect(CharacterGridUnit unit)
    {
        BeginAction();

        orbitPoint = unit.statusEffectCamTarget;

        disablingStatusEffectCam.Follow = unit.statusEffectCamTarget;
        disablingStatusEffectCam.LookAt = unit.statusEffectCamTarget;

        orbitPoint.localRotation = Quaternion.Euler(orbitPointStartingRotation);

        StartCoroutine(showDisabledUnitRoutine(unit));
    }

    private IEnumerator showDisabledUnitRoutine(CharacterGridUnit unit)
    {
        unit.CharacterHealth().DeactivateHealthVisualImmediate();
        yield return new WaitForSeconds(showDisabledUnitDelay);
        disablingStatusEffectCam.gameObject.SetActive(true);
        unit.CharacterHealth().ActivateNameOnlyUI(true);
        yield return new WaitForSeconds(unitDisabledDisplayTime);
        unit.CharacterHealth().ActivateNameOnlyUI(false);
        EndAction();
        disablingStatusEffectCam.gameObject.SetActive(false);
    }

    public void PlayDamageTurnEndEvent(CharacterGridUnit unit)
    {
        BeginAction();

        //Setup Camera
        orbitPoint = unit.statusEffectCamTarget;
        orbitPoint.localRotation = Quaternion.Euler(Vector3.zero);

        turnEndStatusEffectCam.Follow = unit.statusEffectCamTarget;
        turnEndStatusEffectCam.LookAt = unit.statusEffectCamTarget;

        //Setup Units to show in case changed.
        List<GridUnit> unitsToShow = new List<GridUnit>
        {
            unit
        };

        FantasyCombatManager.Instance.SetUnitsToShow(unitsToShow);

        FantasyCombatManager.Instance.ActivateCurrentActiveCam(false);
        turnEndStatusEffectCam.gameObject.SetActive(true);

        StartCoroutine(TurnEndStatusEffectRoutine(unit));
    }

    public void HideTurnEndCam()
    {
        if (turnEndStatusEffectCam.gameObject.activeInHierarchy)
        {
            turnEndStatusEffectCam.gameObject.SetActive(false);
        }
    }

    IEnumerator TurnEndStatusEffectRoutine(CharacterGridUnit unit)
    {
        float waitTime = unit.IsAlreadyAtCorrectPos() ? 0.15f : turnEndEffectDelay;

        unit.CharacterHealth().DeactivateHealthVisualImmediate();
        yield return new WaitForSeconds(waitTime);
        Health.RaiseHealthChangeEvent(true);
        yield return new WaitUntil(() => unit.CharacterHealth().GetHealthUI().showingSkillFeedback == false);
        EndAction();
    }

    public void CureStatusEffect(CharacterGridUnit unit, StatusEffectData effectToCure)
    {
        if (combatUnitStatusEffects[unit].Count > 0)
        {
            StatusEffect effectClass = combatUnitStatusEffects[unit].FirstOrDefault((effect) => effect.GetType().ToString() == effectToCure.name);

            if (effectClass)
            {
                //Remove The Status Effect. 
                StatusEffectEnded(unit, effectClass);
            }
        }
    }

    public void CureStatusEffect(CharacterGridUnit unit, string effectToCureName)
    {
        if (combatUnitStatusEffects[unit].Count > 0)
        {
            StatusEffect effectClass = combatUnitStatusEffects[unit].FirstOrDefault((effect) => effect.GetType().ToString() == effectToCureName);

            if (effectClass)
            {
                //Remove The Status Effect. 
                StatusEffectEnded(unit, effectClass);
            }
        }
    }

    public void StatusEffectEnded(CharacterGridUnit unit, StatusEffect effectToRemove)
    {
        if (!combatUnitStatusEffects.ContainsKey(unit)) { return; }
        if (combatUnitStatusEffects[unit].Remove(effectToRemove)) //Remove was sucessful.
        {
            CancelStatusEffectTurnEndEvent(effectToRemove);
            Destroy(effectToRemove);
        }
    }

    public int TriggerNewlyAppliedEffects(CharacterGridUnit unit)
    {
        int appliedEffects = newlyAppliedEffects[unit].Count + newlyStackedEffects[unit].Count;
        ActivateEffects(unit);

        return appliedEffects;
    }

    private void ActivateEffects(CharacterGridUnit unit)
    {
        //Activate New Effects
        foreach (StatusEffect effect in newlyAppliedEffects[unit])
        {
            effect.OnEffectApplied();
            effect.hasEffectActivated = true;

            Unitafflicted?.Invoke(unit, effect.inflictor, effect.effectData);
        }
        
        //Stack already applied effects
        foreach (InflictedStatusEffectData effect in newlyStackedEffects[unit])
        {
            StatusEffect statusEffect = combatUnitStatusEffects[unit].FirstOrDefault((unitEffect) => unitEffect.GetType().ToString() == effect.effectData.name);

            if (statusEffect)
                statusEffect.IncreaseTurns(effect.numOfTurns, effect.buffChange);
        }

        newlyAppliedEffects[unit].Clear();
        newlyStackedEffects[unit].Clear();
    }

    public void IntializeUnitStatusEffects(CharacterGridUnit unit)
    {
        if(!combatUnitStatusEffects.ContainsKey(unit))
            combatUnitStatusEffects[unit] = new List<StatusEffect>();

        newlyAppliedEffects[unit] = new List<StatusEffect>();
        newlyStackedEffects[unit] = new List<InflictedStatusEffectData>();
    }

    public void StoreBattleStartData(CharacterGridUnit unit)
    {
        if (!unit) { return; }

        if (combatUnitStatusEffects.ContainsKey(unit) && combatUnitStatusEffects[unit].Count > 0)
        {
            statusEffectsAtBattleStart[unit] = new List<InflictedStatusEffectData>();

            foreach (var effect in combatUnitStatusEffects[unit])
            {
                InflictedStatusEffectData effectData = new InflictedStatusEffectData(effect.effectData, unit, effect.turnsRemaining, effect.currentBuff);
                statusEffectsAtBattleStart[unit].Add(effectData);
            }
        }
    }

    public void ClearBattleStartData()
    {
        statusEffectsAtBattleStart.Clear();
    }

    public void ResetUnitToBattleStartState(CharacterGridUnit unit)
    {
        if (!unit) { return; }

        IntializeUnitStatusEffects(unit);

        if (statusEffectsAtBattleStart.ContainsKey(unit) && statusEffectsAtBattleStart[unit].Count > 0)
        {
            foreach (var effect in statusEffectsAtBattleStart[unit])
            {
                ApplyStatusEffect(effect.effectData, unit, null, effect.numOfTurns, effect.buffChange);
            }

            ActivateEffects(unit);
        }
    }

    public void OnCombatEnd(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        foreach (KeyValuePair<CharacterGridUnit, List<StatusEffect>> item in combatUnitStatusEffects)
        {
            for (int i = combatUnitStatusEffects[item.Key].Count - 1; i >= 0; i--)
            {
                StatusEffect effect = combatUnitStatusEffects[item.Key][i];

                if (effect.effectData.canBeAppliedOutsideCombat){ continue; }
                
                //combatUnitStatusEffects[item.Key].RemoveAt(i);
                StatusEffectEnded(item.Key, effect);
            }
        }

        //if (battleResult == BattleResult.Defeat || battleResult == BattleResult.Restart) { return; }

        ClearData();
    }


    private void ClearData()
    {
        newlyAppliedEffects.Clear();
        newlyStackedEffects.Clear();

        //Clear Enemies from list. Leave all players because of outside Combat Status effects like Overburdened & Drunk.
        for (int i = combatUnitStatusEffects.Count - 1; i >= 0; i--)
        {
            var pair = combatUnitStatusEffects.ElementAt(i);

            if(!(pair.Key is PlayerGridUnit))
            {
                combatUnitStatusEffects.Remove(pair.Key);
            }
        }
    }

    public void OnUnitKO(GridUnit unit)
    {
        CharacterGridUnit character = unit as CharacterGridUnit;

        if (!character) { return; }

        //Remove All Status Efefcts From KO Unit.
        for (int i = combatUnitStatusEffects[character].Count - 1; i >= 0; i--)
        {
            StatusEffect effect = combatUnitStatusEffects[character][i];
            StatusEffectEnded(character, effect);
        }

        combatUnitStatusEffects[character].Clear();
    }
    
    private void CancelStatusEffectTurnEndEvent(StatusEffect effect)
    {
        if (effect.effectData.hasTurnEndEffect)
            FantasyCombatManager.Instance.CancelTurnEndEvent(effect as ITurnEndEvent);
    }

    private void OnDisable()
    {
        Health.UnitKOed -= OnUnitKO;
        Flee.UnitFled -= OnUnitKO;
        FantasyCombatManager.Instance.CombatEnded -= OnCombatEnd;
        PartyManager.Instance.PlayerPartyDataSet -= IntializePlayerStatusEffects;
    }
    //DOERS
    public GameObject SpawnStatusEffectVFX(CharacterGridUnit unit, StatusEffectData effectData)
    {
        if (!effectData.unitVFXPrefab) { return null; } //Do nothing if has no Unit VFX Prefab

        string effectName = effectData.name;
        GameObject vfx;

        if (vfxPoolDict.ContainsKey(effectName))
        {
            Transform poolHeader = vfxPool.GetChild(vfxPoolDict[effectName]);

            if(poolHeader.childCount > 0)
            {
                //Grab First Child.
                vfx = poolHeader.GetChild(0).gameObject;
            }
            else
            {
                //Spawn New VFX to pool
                vfx = Instantiate(effectData.unitVFXPrefab, poolHeader);
            }
        }
        else
        {
            //The Pool doesn't Exist so create it.
            GameObject poolGO = new GameObject(effectName);
            Transform poolHeader = poolGO.transform;
            poolHeader.parent = vfxPool;

            vfxPoolDict[effectName] = poolHeader.GetSiblingIndex();

            //Now Spawn VFX
            vfx = Instantiate(effectData.unitVFXPrefab, poolHeader);
        }

        //Now Move VFX To Unit
        vfx.transform.parent = unit.unitAnimator.GetStatusEffectVFXBodyTransform(effectData.unitVFXBodyPart);

        //Reset Pos & Rot
        vfx.transform.localPosition = Vector3.zero;
        vfx.transform.localEulerAngles = Vector3.zero;
        //Reset Scale
        vfx.transform.localScale = Vector3.one;

        return vfx;
    }

    public void RemoveStatusEffectVFX(GameObject spawnedVFX, StatusEffectData effectData)
    {
        if (!spawnedVFX) { return; }

        string effectName = effectData.name;
        Transform poolHeader = vfxPool.GetChild(vfxPoolDict[effectName]);

        spawnedVFX.transform.parent = poolHeader;
    }

    //GETTERS
    public float GetBuffMultiplier(int buffValue)
    {
        if(buffValue == 1)
        {
            return buffMultiplier;
        }
        else if(buffValue == 2)
        {
            return superBuffMultiplier;
        }
        else if (buffValue == -1)
        {
            return debuffMultiplier;
        }
        else if(buffValue == -2)
        {
            return superDebuffMultiplier;
        }

        return 1;
    }

    public Color GetBuffColor(int buffValue)
    {
        if (buffValue == 1)
        {
            return buffColor;
        }
        else if (buffValue == 2)
        {
            return superBuffColor;
        }
        else if (buffValue == -1)
        {
            return debuffColor;
        }
        else if (buffValue == -2)
        {
            return superDebuffColor;
        }

        return buffColor;
    }

    public bool IsUnitDisabled(CharacterGridUnit unit)
    {
        if (!unit) {  return false; }

        if (combatUnitStatusEffects.ContainsKey(unit) && combatUnitStatusEffects[unit].Any((effect) => effect.effectData.preventsUnitFromActing))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool CanApplyStatusEffect(GridUnit unit, StatusEffectData effectData)
    {
        CharacterGridUnit character = unit as CharacterGridUnit;

        if(!character) { return false; }

        //Check if unit is guarding
        if (character.CharacterHealth().isGuarding && effectData.canBeGuarded)
        {
            //Don't apply any effects because target guarded it.
            return false;
        }

        //Firstly check if unit immune to effect's element
        if (character.stats.IsImmuneToStatusEffect(effectData))
        {
            //Don't apply any effects because target is immue.
            return false;
        }

        if(effectData.associatedElement != Element.None)
        {
            Affinity affinityToSE = TheCalculator.Instance.GetAffinity(character, effectData.associatedElement, null);

            switch (affinityToSE)
            {
                case Affinity.Absorb:
                case Affinity.Immune:
                case Affinity.Reflect:
                    return false;
                default:
                    break;
            }
        }

        return true;
    }
    public bool IsAfflictedWithNegativeEffect(GridUnit unit)
    {
        if(unit is CharacterGridUnit character)
        {
            return combatUnitStatusEffects.ContainsKey(character) &&
                 combatUnitStatusEffects[character].Any((effect) => effect.effectData.effectType == StatusEffectData.EffectType.Negative);

        }

        return false;
    }

    public bool HasReducedMovementDueToStatusEffect(CharacterGridUnit unit)
    {
        return combatUnitStatusEffects.ContainsKey(unit) && combatUnitStatusEffects[unit].Any((effect) => effect.effectData == overburdened || effect.effectData == wounded);
    }

    public bool UnitHasStatusEffect(CharacterGridUnit unit, StatusEffectData statusEffect)
    {
        return combatUnitStatusEffects.ContainsKey(unit) && combatUnitStatusEffects[unit].Any((effect) => effect.effectData == statusEffect);
    }

    public bool IsUnitKnockedDown(CharacterGridUnit unit)
    {
        return combatUnitStatusEffects.ContainsKey(unit) && combatUnitStatusEffects[unit].Any((effect) => effect.effectData == knockdown);
    }

    public bool IsKnockdownHit(List<InflictedStatusEffectData> inflictedStatuses, bool isUnitGuarding)
    {
        return !isUnitGuarding && inflictedStatuses.Any((status) => status.effectData == knockdown);
    }

    public bool ProhibitUnitFPGain(CharacterGridUnit unit)
    {
        return combatUnitStatusEffects.ContainsKey(unit) && combatUnitStatusEffects[unit].Any((effect) => effect.effectData.banFPGain);
    }

    public bool UnitHasStatusEffectWithTurnEndEvent(CharacterGridUnit unit)
    {
        return combatUnitStatusEffects[unit].Any((effect) => effect.effectData.hasTurnEndEffect);
    }

    public bool WillMaxBuffBeExtended(CharacterGridUnit unit, StatusEffectData effectData, int buffChange)
    {
        StatusEffect statusEffectClass;
        statusEffectClass = combatUnitStatusEffects[unit].FirstOrDefault((se) => effectData == se.effectData);

        if (statusEffectClass)
        {
            return statusEffectClass.hasEffectActivated && Mathf.Abs(statusEffectClass.GetCurrentBuffValue()) == 2 && Mathf.Abs(statusEffectClass.GetCurrentBuffValue() + buffChange) > 2;
        }

        return false;
    }

    public StatusEffectData GetKnockdownEffectData()
    {
        return knockdown;
    }

    public StatusEffectData GetWoundedEffectData()
    {
        return wounded;
    }

    public void ShakeCam()
    {
        impulseSource.GenerateImpulse();
    }


}
