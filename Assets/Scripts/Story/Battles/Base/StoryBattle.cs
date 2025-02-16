using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class StoryBattle : MonoBehaviour, IBattleTrigger
{
    [Header("Music")]
    [SerializeField] AudioClip customBattleMusic;
    [Header("Transforms")]
    [Tooltip("Rotation matters! The Rotation will use to determine the starting direction of the battle.")]
    [SerializeField] protected Transform centreGridPosition;
    [Header("Battle Setup")]
    [SerializeField] protected EnemyGroup enemyGroup;
    [SerializeField] protected BattleStarter.CombatAdvantage advantageType;
    [Space(10)]
    [SerializeField] GameObject battleBordersHeader;

    public MusicType battleMusicType { get; set; }

    protected bool hasBattleStarted = false;
    private GameObject spawnedLoot;

    PlayerGridUnit leader;

    [System.Serializable]
    public class TurnNumberEvent
    {
        public int turnNumber;
        public Dialogue dialogue;
        public int tutorialIndex;
    }

    private void Awake()
    {
        battleMusicType = customBattleMusic ? MusicType.BossBattle: MusicType.Battle;

        if (battleBordersHeader)
            battleBordersHeader.SetActive(false);

        leader = PartyManager.Instance.GetLeader();
    }


    //Helper Methods
    public void PrepareCombatants()
    {
        if(customBattleMusic)
            AudioManager.Instance.SetBossBattleMusic(customBattleMusic);

        Debug.Log("Preparing Combatants");
        BattleStarter.Instance.StoryBattleTriggered(enemyGroup, advantageType,  centreGridPosition, this);

        leader.ActivateFollowCam(true);
    }

    public void BeginBattle()
    {
        hasBattleStarted = true;
        FantasyCombatManager.Instance.CombatEnded += OnCombatEnd;

        OnBattleStart();

        BattleStarter.Instance.StartBattle();

        if (battleBordersHeader)
            battleBordersHeader.SetActive(true);

        if (FantasyCombatManager.Instance.GetActiveUnit() != leader)
            leader.ActivateFollowCam(false);
    }

    protected virtual void OnCombatEnd(BattleResult result, IBattleTrigger battleTrigger)
    {
        if (result != BattleResult.Victory) { return; }

        FantasyCombatManager.Instance.CombatEnded -= OnCombatEnd;

        if (battleBordersHeader)
            battleBordersHeader.SetActive(false);
    }

    protected void PlayPostBattleCinematic(CinematicTrigger cinematicTrigger, GameObject spawnedLoot)
    {
        if (cinematicTrigger.PlayCinematic() && spawnedLoot)
        {
            spawnedLoot.SetActive(false);

            //Subscribe to Cinematic End
            this.spawnedLoot = spawnedLoot;
            CinematicManager.Instance.CinematicEnded += WarpBattleLoot;
        }
    }

    private void WarpBattleLoot()
    {
        CinematicManager.Instance.CinematicEnded -= WarpBattleLoot;
        FantasyCombatManager.Instance.SetLootPos(spawnedLoot);
        spawnedLoot.SetActive(true);
    }

    public void PlayBattleMusic()
    {
        if (!customBattleMusic) { return; }

        AudioManager.Instance.SetBossBattleMusic(customBattleMusic);
        AudioManager.Instance.PlayBattleMusic(this);
    }

    protected virtual void OnBattleStart() { }

    //IBattle Trigger definitions
    public BattleType battleType { get; set; } = BattleType.Story;

    public abstract bool CanPlayDefeatScene();
    public abstract bool CanPlayStoryVictoryScene();
    public abstract void TriggerDefeatEvent(List<CharacterGridUnit> survivingEnemies, float defeatFaderFadeOutTime);
    public abstract void TriggerVictoryEvent(GameObject spawnedLoot, float victoryFaderFadeOutTime);
}
