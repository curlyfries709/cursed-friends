using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public abstract class StatusEffect : MonoBehaviour
{
    //UI Variables
    GameObject healthUISEPrefab = null;
    List<GameObject> HUDSEPrefabs = new List<GameObject>();

    int turnNumberIndex = 1;
    int buffArrowsIndex = 2;

    //Caches
    public StatusEffectData effectData;
    protected CharacterGridUnit myUnit;
    public CharacterGridUnit inflictor;

    protected bool firstTurn = true;
    public bool hasEffectActivated = false;
    bool isFiredUpBuff;

    protected GameObject spawnedVFX = null;

    //For Buffs Or Debuffs.
    public int turnsRemaining;
    public int currentBuff = 0;

    private void Awake()
    {
        //Enabled on Awake So On Enable isn't called before setup method.
        enabled = false;
    }

    public void Setup(CharacterGridUnit unit, CharacterGridUnit inflictor, StatusEffectData effectData, int turns, int buffChange, bool isFiredUpBuff)
    {
        myUnit = unit;
        turnsRemaining = turns;

        this.inflictor = inflictor;
        this.effectData = effectData;
        currentBuff = buffChange;

        this.enabled = true;
        this.isFiredUpBuff = isFiredUpBuff;
    }

    private void OnEnable()
    {
        myUnit.EndTurn += OnTurnEnd;

        if (effectData.hasTurnEndEffect)
        {
            myUnit.BeginTurn += OnTurnStart;
        }
    }

    public abstract void OnEffectApplied();
    protected abstract void OnTurnStart();
    protected abstract void OnTurnEnd();
    protected abstract void EffectEnded();
    protected abstract void OnStatusStacked();
    protected abstract void CalculateNewStatValue(bool resetValues);


    protected void DecreaseTurnsRemaining()
    {
        if(inflictor == myUnit && firstTurn && !effectData.hasTurnEndEffect && !IsSEAppliedOnTurnStart())
        {

        }
        else
        {
            turnsRemaining = turnsRemaining - 1;
        }

        firstTurn = false;

        if (effectData.loseFPEachTurn)
        {
            myUnit.Health().LoseFP();
        }
        
        if(turnsRemaining == 0)
        {
            EffectEnded();
            RemoveStatusEffect();
            return;
        }

        UpdateUI();
    }



    public virtual void IncreaseTurns(int numOfTurns, int buffChange)
    {
        if (effectData.isStatBuffOrDebuff)
        {
            int oldBuff = currentBuff;

            if (!StatBuffChanged(buffChange)) //If the buff is gonna cancel out, don't bother updating UI below.
            {
                return;
            }

            /*if ((oldBuff == -2 && currentBuff == -1)|| (oldBuff == 2 && currentBuff == 1) || currentBuff == 0)
            {
                //Current Super AG Down + AG up = AG Down -> Don't increase -2 + 1 = -1 
                //Current Super AG UP + Ag Down = AG Up -> Don't Increase 2 + -1  = 1

                //DO NOT INCREASE TURNS WHEN DOWNGRADING
                //Super AG Down -> AG Down
                //Super AG Up -> AG Up 

                //DO NOT INCREASE TURNS WHEN BUFF CANCELS OUT
            }*/
            if (currentBuff == 0)
            {
                //DO NOTHING IF BUFF CANCELS OUT
            }
            else if (oldBuff == currentBuff)
            {
                //INCREASE TURNS WHEN BUFFS ARE THE SAME
                turnsRemaining = turnsRemaining + numOfTurns;
            }
            else
            {
                //OTHERWISE OVERWRITE NUMBER OF TURNS.
                //Curent AG Down + Super AG Up = AG Up -> Set turns -1 + 2 = 1
                //Current AG Up + Super AG Down = AG Down -> Set turns 1 + -2 = -1

                turnsRemaining = numOfTurns;
            }
        }
        else
        {
            turnsRemaining = turnsRemaining +  numOfTurns;
        }

        UpdateUI();
        OnStatusStacked();
    }

    private bool StatBuffChanged(int buffChange)
    {
        //Reset Values First
        CalculateNewStatValue(true);
        currentBuff = Mathf.Clamp(currentBuff + buffChange, -2, 2);

        if (currentBuff == 0)
        {
            //Means it cancels Out. So Remove. 
            RemoveStatusEffect();
            return false;
        }
        else
        {
            CalculateNewStatValue(false);
            return true;
        }
    }

    public void RemoveStatusEffect()
    {
        StatusEffectManager.Instance.StatusEffectEnded(myUnit, this);
    }

    protected void ApplyStatusEffectDamageToUnit(int healthPercentToLost)
    {
        AttackData attackData = new AttackData(inflictor, 0);
        attackData.canCrit = false;
        attackData.canEvade = false;

        myUnit.Health().TakeStatusEffectDamage(attackData, healthPercentToLost);
    }

    //VISUAL & UI
    protected void SpawnVisual()
    {
        //Spawn VFX
        spawnedVFX = StatusEffectManager.Instance.SpawnStatusEffectVFX(myUnit, effectData);

        if (!effectData.effectVisualPrefab) { return; }

        //Spawn UI
        healthUISEPrefab = Instantiate(effectData.effectVisualPrefab, myUnit.Health().GetStatusEffectUIHeader());

        PlayerGridUnit playerGridUnit = myUnit as PlayerGridUnit;

        if(playerGridUnit)
        {
            HUDSEPrefabs = HUDManager.Instance.SpawnStatusEffectUI(effectData, playerGridUnit);
        }

        UpdateUI();
    }

    protected void UpdateUI()
    {
        if (healthUISEPrefab)
        {
            healthUISEPrefab.transform.GetChild(turnNumberIndex).GetComponent<TextMeshProUGUI>().text = turnsRemaining.ToString();
            UpdateBuffArrow(healthUISEPrefab);
        }

        foreach (GameObject HUDSEPrefab in HUDSEPrefabs)
        {
            HUDSEPrefab.transform.GetChild(turnNumberIndex).GetComponent<TextMeshProUGUI>().text = turnsRemaining.ToString();
            UpdateBuffArrow(HUDSEPrefab);
        }
      
    }

    private void UpdateBuffArrow(GameObject healthPrefab)
    {
        if (!effectData.isStatBuffOrDebuff) { return; }

        Transform header = healthPrefab.transform.GetChild(buffArrowsIndex);

        foreach (Transform child in header)
        {
            child.gameObject.SetActive(child.GetSiblingIndex() == GetBuffArrowIndex());
        }
    }

    protected void RemoveVisual()
    {
        //Remove VFX
        StatusEffectManager.Instance.RemoveStatusEffectVFX(spawnedVFX, effectData);

        if (!healthUISEPrefab) { return; }

        //Destroy UI
        Destroy(healthUISEPrefab);

        foreach (GameObject HUDSEPrefab in HUDSEPrefabs)
        {
            Destroy(HUDSEPrefab);
        }
    }

    private int GetBuffArrowIndex()
    {
        if (currentBuff == 1)
        {
            return 2;
        }
        else if (currentBuff == 2)
        {
            return 3;
        }
        else if (currentBuff == -1)
        {
            return 1;
        }
        else if (currentBuff == -2)
        {
            return 0;
        }

        return 2;
    }



    private void OnDestroy()
    {
        myUnit.EndTurn -= OnTurnEnd;

        if(turnsRemaining > 0) //To Avoid Calling this twice
            EffectEnded();

        RemoveVisual();

        if (effectData.hasTurnEndEffect)
        {
            myUnit.BeginTurn -= OnTurnStart;
        }
    }

    public int GetCurrentBuffValue()
    {
        return currentBuff;
    }

    private bool IsSEAppliedOnTurnStart() //This Applies for SEs like Fired Up! which is applied before Unit Acts
    {
        return this is FiredUp || isFiredUpBuff;
    }
}
