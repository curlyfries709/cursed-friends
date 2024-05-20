using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicStoryBattle : StoryBattle
{
    [Header("Conditions")]
    [SerializeField] bool playStoryBattleVictoryScene = false;
    [Header("Cinematics")]
    [SerializeField] CinematicTrigger postBattleCinematic;

    public override void TriggerVictoryEvent(GameObject spawnedLoot, float victoryFaderFadeOutTime)
    {
        PlayPostBattleCinematic(postBattleCinematic, spawnedLoot);
    }

    //IBattle Trigger Definitions
    public override bool CanPlayDefeatScene() { return true; }

    public override bool CanPlayStoryVictoryScene() { return playStoryBattleVictoryScene; }

    public override void TriggerDefeatEvent(List<CharacterGridUnit> survivingEnemies, float defeatFaderFadeOutTime) { }

}
