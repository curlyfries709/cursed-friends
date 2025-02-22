using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

public class GridUnitAnimNotifies : MonoBehaviour
{
    //CACHE
    GridUnitAnimator unitAnimator = null;
    CharacterGridUnit myUnit = null;
    EnemyIntiateCombat freeRoamAttackObject = null;

    public enum EventType
    {
        Damage,
        Evade,
        EvadeAndDamage, 
        Heal
    }

    public void Setup(CharacterGridUnit myUnit, EnemyIntiateCombat freeRoamAttackObject)
    {
        this.myUnit = myUnit;
        this.freeRoamAttackObject = freeRoamAttackObject;
    }

    //Animation Events
    public void TriggerActionEvent(EventType eventType)
    {
        FantasyCombatManager.Instance.currentCombatAction.ActionAnimEventRaised(eventType);
    }

    /*public void ShowDamageFeedback(int disableSlowMo)
    {
        if (cancelSkillFeedbackDisplay)
        {
            cancelSkillFeedbackDisplay = false;
            return;
        }

        IDamageable.unitAttackComplete?.Invoke(beginHealthCountdown);

        if (disableSlowMo == 0)
        {
            ActivateSlowmo();
        }
    }
    */

    public void AmbushAttackComplete()
    {
        BattleStarter.Instance.PlayerStartCombatAttackComplete?.Invoke(myUnit.GetComponent<EnemyStateMachine>());
    }

    public void AmbushTargetHit()
    {
        BattleStarter.Instance.TargetHit();
    }

    public void EnableHitbox()
    {
        freeRoamAttackObject.ActivateHitBox(true);
    }

    public void DisableHitbox()
    {
        freeRoamAttackObject.ActivateHitBox(false);
    }

    public void PreparePOFKnockout()
    {
        POFDirector.Instance.PrepareEnemyKO();
    }

    public void POFPose()
    {
        POFDirector.Instance.ShowIntiatorUI();
    }

    public void ShowWeapon()
    {
        unitAnimator.ShowWeapon(true);
    }

    public void HideWeapon()
    {
        unitAnimator.ShowWeapon(false);
    }

    public void PlayEnjoyPotionSFX()
    {
        unitAnimator.GetPotionFeedback()?.PlayFeedbacks();
    }
}
