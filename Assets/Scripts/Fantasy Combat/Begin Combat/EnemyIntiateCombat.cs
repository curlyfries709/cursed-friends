using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class EnemyIntiateCombat : MonoBehaviour, IBattleTrigger
{
    [SerializeField] EnemyStateMachine myStateMachine;
    Collider myCollider;
    public BattleType battleType { get; set; }
    public MusicType battleMusicType { get; set; } = MusicType.Battle;

    bool externalBattleTriggerSet = false;
    IBattleTrigger myBattleTrigger;

    private void Awake()
    {
        myCollider = GetComponent<Collider>();
        myCollider.enabled = false;
        myCollider.isTrigger = true;

        SetBattleTrigger(null);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            BattleStarter.Instance.EnemyAdvantageTriggered(myStateMachine, myBattleTrigger);
            myCollider.enabled = false;
        }
    }

    public void ActivateHitBox(bool activate)
    {
        myCollider.enabled = activate;
    }

    public void SetBattleTrigger(IBattleTrigger newBattleTrigger)
    {
        if (newBattleTrigger == null)
        {
            battleType = BattleType.Normal;
            externalBattleTriggerSet = false;
            myBattleTrigger = this;
        }
        else
        {
            battleType = newBattleTrigger.battleType;
            externalBattleTriggerSet = true;
            myBattleTrigger = newBattleTrigger;
        }
    }

    //Battle Triggers
    public bool CanPlayStoryVictoryScene()
    {
        if(externalBattleTriggerSet)
        {
            return myBattleTrigger.CanPlayStoryVictoryScene();
        }

        return false;
    }

    public bool CanPlayDefeatScene()
    {
        if (externalBattleTriggerSet)
        {
            return myBattleTrigger.CanPlayDefeatScene();
        }

        return true;
    }

    public void TriggerVictoryEvent(GameObject spawnedLoot, float victoryFaderFadeOutTime)
    {
        if (externalBattleTriggerSet)
        {
            myBattleTrigger.TriggerVictoryEvent(spawnedLoot, victoryFaderFadeOutTime);
        }
    }

    public void TriggerDefeatEvent(List<CharacterGridUnit> survivingEnemies, float defeatFaderFadeOutTime)
    {
        if (externalBattleTriggerSet)
        {
            myBattleTrigger.TriggerDefeatEvent(survivingEnemies, defeatFaderFadeOutTime);
        }
    }
}
