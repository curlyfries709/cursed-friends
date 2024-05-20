using System.Collections.Generic;
using UnityEngine;

public interface IBattleTrigger 
{
    public BattleType battleType { get; set; }

    public MusicType battleMusicType { get; set; }

    public bool CanPlayStoryVictoryScene();
    public bool CanPlayDefeatScene();

    public void TriggerVictoryEvent(GameObject spawnedLoot, float victoryFaderFadeOutTime);

    public void TriggerDefeatEvent(List<CharacterGridUnit> survivingEnemies, float defeatFaderFadeOutTime);

    public bool CanPlayerReturnToFreeRoam()
    {
        switch (battleType)
        {
            case BattleType.Story:
                return false;
            case BattleType.Trial:
                return false;
            default:
                return true;
        }
    }

}
