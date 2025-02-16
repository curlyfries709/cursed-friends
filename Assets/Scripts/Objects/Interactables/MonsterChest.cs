using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

public class MonsterChest : TreasureChest, IBattleTrigger, ISaveable
{
    [Title("Monster Chest")]
    [SerializeField] CharacterGridUnit battleStarterMonster;
    [Tooltip("Suitable distance for monster to be for attack to connect")]
    [SerializeField] float offsetBetweenMonsterAndPlayer = 1;
    [Title("Transforms")]
    [SerializeField] Transform playerWarpTransform;
    [SerializeField] Transform droppedLootTransform;
    [Title("Components")]
    [SerializeField] GameObject model;
    [Space(5)]
    [SerializeField] BoxCollider gridBoxCollider;
    [SerializeField] Collider chestModelCollider;

    [SerializeField, HideInInspector]
    private MonsterChestState monsterChestState = new MonsterChestState();

    bool isBattleCompleted = false;
    public BattleType battleType { get; set; } = BattleType.MonsterChest;
    public MusicType battleMusicType { get; set; } = MusicType.Battle;

    EnemyStateMachine contactedEnemy;

    protected override void Start()
    {
        base.Start();

        battleStarterMonster.unitAnimator.GetEnemyBattleTrigger().SetBattleTrigger(this);
        contactedEnemy = battleStarterMonster.GetComponent<EnemyStateMachine>();
    }

    protected override bool TryUnlockChest()
    {
        if (!isBattleCompleted)
        {
            TriggerBattle();
            return false;
        }

        if (StoryManager.Instance.TriggerFirstTimeEvent("MonsterChest")) { return false; }

        return true;
    }

    private void TriggerBattle()
    {
        ControlsManager.Instance.DisableControls();

        RepositionEnemy();

        EnableComponents(false);
        battleStarterMonster.ActivateUnit(true);
    }

    private void RepositionEnemy()
    {
        //Position to guarantee attack hit
        Vector3 direction = (battleStarterMonster.transform.position - GetPlayerSM().transform.position).normalized;
        battleStarterMonster.transform.position = GetPlayerSM().transform.position + (direction * offsetBetweenMonsterAndPlayer);

        //Rotate Enemy to Guarantee Player in Line of Sight
        Vector3 LookDirection = (GetPlayerSM().transform.position - battleStarterMonster.transform.position).normalized;
        battleStarterMonster.transform.rotation = Quaternion.LookRotation(LookDirection);
    }

    //BATTLE TRIGGERS

    public void TriggerVictoryEvent(GameObject spawnedLoot, float victoryFaderFadeOutTime)
    {
        //Set Bools
        isBattleCompleted = true;
        locked = false;

        EnableComponents(true);

        //Warp Player & Companions in front of chest. 
        GetPlayerSM().WarpPlayer(playerWarpTransform, GetPlayerSM().fantasyRoamState, true);

        //Warp Loot
        if(spawnedLoot)
            spawnedLoot.transform.position = droppedLootTransform.position;

        //Enable Controls once Victory Fade out Complete
        StartCoroutine(EnableControlsRoutine(victoryFaderFadeOutTime));
    }

    private void EnableComponents(bool enable)
    {
        InteractionManager.Instance.OnRadiusExit(this);
        model.SetActive(enable);
        interactionCollider.enabled = enable;
        lootVFX.SetActive(enable);
    }

    IEnumerator EnableControlsRoutine(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        ControlsManager.Instance.SwitchCurrentActionMap("Player");
    }


    public bool CanPlayDefeatScene(){return true;}

    public bool CanPlayStoryVictoryScene(){ return false;}

    public void TriggerDefeatEvent(List<CharacterGridUnit> survivingEnemies, float defeatFaderFadeOutTime) { }

    //Saving
    //Saving
    [System.Serializable]
    public class MonsterChestState
    {
        public bool locked = false;
        public bool isBattleCompleted = false;

        public List<int> takenItemIndices = new List<int>();
    }


    public override object CaptureState()
    {
        monsterChestState.locked = locked;
        monsterChestState.isBattleCompleted = isBattleCompleted;
        monsterChestState.takenItemIndices = takenItemIndices;

        return SerializationUtility.SerializeValue(monsterChestState, DataFormat.Binary);
    }


    public override void RestoreState(object state)
    {
        if (state == null) { return; }

        byte[] bytes = state as byte[];
        monsterChestState = SerializationUtility.DeserializeValue<MonsterChestState>(bytes, DataFormat.Binary);

        //Restore Date
        locked = monsterChestState.locked;
        takenItemIndices = monsterChestState.takenItemIndices;
        isBattleCompleted = monsterChestState.isBattleCompleted;

        RestoreChestState();

        if(!model.activeInHierarchy)
            EnableComponents(true);
    }

    private PlayerStateMachine GetPlayerSM()
    {
        return PlayerSpawnerManager.Instance.GetPlayerStateMachine();
    }
}
