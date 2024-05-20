using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TutorialBattle : StoryBattle, ITurnStartEvent
{
    [Space(10)]
    [SerializeField] CinematicTrigger postBattleCinematic;
    [Header("Dialogue")]
    [SerializeField] List<TurnNumberEvent> dialogueToPlay;

    TurnNumberEvent currentTurnNumberEvent;


    public int turnStartEventOrder { get; set; } = 0;

    protected override void OnBattleStart()
    {
        base.OnBattleStart();
        FantasyCombatManager.Instance.OnNewTurn += OnAnyUnitTurnStart;
    }

    //EVENTS
    private void OnAnyUnitTurnStart(CharacterGridUnit turnOwner, int turnNumber)
    {
        currentTurnNumberEvent = dialogueToPlay.FirstOrDefault((e) => e.turnNumber == turnNumber);

        if (currentTurnNumberEvent != null)
            FantasyCombatManager.Instance.AddTurnStartEventToQueue(this);
    }

    public void PlayTurnStartEvent()
    {
        DialogueManager.Instance.PlayDialogue(currentTurnNumberEvent.dialogue, false);
        DialogueManager.Instance.DialogueEnded += PlayTutorial;
    }

    private void PlayTutorial()
    {
        DialogueManager.Instance.DialogueEnded -= PlayTutorial;

        if (StoryManager.Instance.PlayTutorial(currentTurnNumberEvent.tutorialIndex))
        {
            HUDManager.Instance.HideHUDs();
            StoryManager.Instance.TutorialComplete += TutorialComplete;
        }
        else
        {
            HUDManager.Instance.ShowActiveHud();
            FantasyCombatManager.Instance.ActionComplete();
        }
    }

    private void TutorialComplete()
    {
        StoryManager.Instance.TutorialComplete -= TutorialComplete;
        FantasyCombatManager.Instance.ActionComplete();
        HUDManager.Instance.ShowActiveHud();
    }

    protected override void OnCombatEnd(BattleResult result, IBattleTrigger battleTrigger)
    {
        base.OnCombatEnd(result, battleTrigger);

        if (result != BattleResult.Victory) { return; }

        FantasyCombatManager.Instance.OnNewTurn -= OnAnyUnitTurnStart;
    }

    public override void TriggerVictoryEvent(GameObject spawnedLoot, float victoryFaderFadeOutTime)
    {
        PlayPostBattleCinematic(postBattleCinematic, spawnedLoot);
    }

    //IBattle Trigger Definitions
    public override bool CanPlayDefeatScene(){ return true; }

    public override bool CanPlayStoryVictoryScene(){ return false; }

    public override void TriggerDefeatEvent(List<CharacterGridUnit> survivingEnemies, float defeatFaderFadeOutTime){ }


}
