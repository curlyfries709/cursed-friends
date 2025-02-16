using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseTool : MonoBehaviour
{
    [SerializeField] protected Tool toolData;

    protected bool isActivated = false;
    bool subscribedToEvent = false;

    public abstract void Use();

    public abstract void CancelUse();

    protected virtual void OnUseComplete(bool deactivateSelf)
    {
        RoamToolsManager.Instance.OnToolUseComplete(toolData);

        if (deactivateSelf)
            Deactivate();
    }

    public virtual void ToggleState()
    {
        if (isActivated)
        {
            Deactivate();
        }
        else
        {
           Activate();
        }
    }

    public virtual void Activate()
    {
        isActivated = true;
        gameObject.SetActive(true);
        
        if(FantasyCombatManager.Instance && !subscribedToEvent)
        {
            subscribedToEvent = true;
            FantasyCombatManager.Instance.BattleTriggered += OnBattleTriggered;
        }
    }

    public virtual void TriggerAction() { }
    
    protected void OnBattleTriggered(BattleStarter.CombatAdvantage advantageType)
    {
        CancelUse();
    }

    public virtual void Deactivate()
    {
        isActivated = false;
        gameObject.SetActive(false);

        if (FantasyCombatManager.Instance && subscribedToEvent)
        {
            subscribedToEvent = false;
            FantasyCombatManager.Instance.BattleTriggered -= OnBattleTriggered;
        }
    }

    public Tool GetData()
    {
        return toolData;
    }

    public bool IsActivated()
    {
        return isActivated;
    }
}
