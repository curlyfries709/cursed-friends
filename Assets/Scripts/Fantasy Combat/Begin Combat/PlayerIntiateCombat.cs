using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIntiateCombat : Interact, IBattleTrigger
{
    [Header("Combat Interaction")]
    [SerializeField] GameObject attackText;
    [SerializeField] GameObject ambushText;
    [Range(1, 45)]
    [SerializeField] float maxRotationAngleForAmbush = 45f;
    [Range(1, 89)]
    [SerializeField] float maxDirectionAngleForAmbush = 60f;
    [Header("Components")]
    [SerializeField] EnemyStateMachine myStatemachine;
    [SerializeField] Transform ambushAttackTransform;

    public BattleType battleType { get; set; }
    public MusicType battleMusicType { get; set; } = MusicType.Battle;

    bool externalBattleTriggerSet = false;
    IBattleTrigger myBattleTrigger;

    bool facingBack = false;
    bool sleepMode = false;

    private void Awake()
    {
        SetBattleTrigger(null);
    }

    public override void HandleInteraction(bool inCombat)
    {
        if (inCombat) { return; }

        if (CanAmbush())
        {
            BattleStarter.Instance.PlayerAdvantageTriggered(myStatemachine, ambushAttackTransform, myBattleTrigger);
        }
        else
        {
            BattleStarter.Instance.NeutralBattleTriggered(myStatemachine, myBattleTrigger);
        }

        gameObject.SetActive(false);
    }

    protected override bool IsInteractorCorrectRotation(Transform interactor)
    {
        Vector3 direction = (transform.position - interactor.position).normalized;

        if (sleepMode)
        {
            return base.IsInteractorCorrectRotation(interactor);
        }

        Vector3 angleDirToTarget = (interactor.position - transform.position).normalized;

        float defaultRotationAngle = Vector3.Angle(interactor.forward, direction);
        float rotationAngleBetween = Vector3.Angle(-interactor.forward, -transform.forward); //Between My Back & Interactor's forward

        float directionAngleBetweenBack = Vector3.Angle(-transform.forward, angleDirToTarget);
        float directionAngleBetweenFront = Vector3.Angle(transform.forward, angleDirToTarget);

        if (rotationAngleBetween <= maxRotationAngleForAmbush && directionAngleBetweenBack <= maxDirectionAngleForAmbush) 
        {
            facingBack = true;
            UpdateAmbushCanvas();
            return true;
        }
        else if(defaultRotationAngle <= InteractionManager.Instance.maxAngleBetweenInteractable && directionAngleBetweenFront <= 100f)
        {
            facingBack = false;
            UpdateAmbushCanvas();
            return true;
        }

        return false;
    }

    public void ActivateSleepMode(bool activate)
    {
        interactorMustFaceMyForward = !activate;
        sleepMode = activate;
        UpdateAmbushCanvas();
    }

    private void UpdateAmbushCanvas()
    {
        bool canAmbush = CanAmbush();

        ambushText.SetActive(canAmbush);
        attackText.SetActive(!canAmbush);
    }

    private bool CanAmbush()
    {
        return facingBack || sleepMode;
    }

    //IBATTLE TRIGGER
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

    public bool CanPlayStoryVictoryScene()
    {
        if (externalBattleTriggerSet)
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
