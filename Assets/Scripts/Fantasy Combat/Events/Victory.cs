using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Sirenix.Serialization;
using TMPro;
using UnityEngine.InputSystem;
using System.Linq;
using System;

public class Victory : MonoBehaviour, IControls, ISaveable
{
    [Header("Timers")]
    [SerializeField] float canvasDelay = 0.2f;
    [SerializeField] float victoryCanvasDisplayTime = 1f;
    [SerializeField] float fadeToVictorySceneDelay = 0.25f;
    [Space(10)]
    [SerializeField] float fadeInTime = 0.25f;
    [SerializeField] float fadeOutTime = 0.25f;
    [Header("Animation Timers")]
    [SerializeField] float fallSpeed = 5f;
    [SerializeField] float heightToStopFall = 0.5f;
    [SerializeField] float landTime = 0.5335f;
    [Header("VICTORY SCENE")]
    [SerializeField] Transform actorsHeader;
    [SerializeField] Transform newSkillSetupHeader;
    [Space(10)]
    [SerializeField] Transform singleVictorPostionHeader;
    [SerializeField] Transform doubleVictorPositionHeader;
    [Space(10)]
    [SerializeField] GameObject singleVictorCam;
    [SerializeField] GameObject doubleVictorCam;
    [Header("UI")]
    [SerializeField] CanvasGroup victoryCanvas;
    [SerializeField] CanvasGroup newSkillCanvas;
    [Space(10)]
    [SerializeField] CanvasGroup newMasteryCanvas;
    [SerializeField] CanvasGroup fader;
    [Header("UI Components")]
    [SerializeField] List<CanvasGroup> progressionCanvasGroups;
    [SerializeField] List<CanvasGroup> masteryCanvasGroups;
    [SerializeField] CanvasGroup controlsCanvasGroup;
    [Space(10)]
    [SerializeField] TextMeshProUGUI battleTimeText;
    [SerializeField] List<TextMeshProUGUI> expMultplierTexts;
    [Space(10)]
    [SerializeField] Transform bonusHeader;
    [SerializeField] Transform experienceHeader;
    [SerializeField] List<MasteryUIHeader> masteryUIHeaders;
    [Header("New Skill UI")]
    [SerializeField] TextMeshProUGUI newSkillName;
    [SerializeField] TextMeshProUGUI newSkillQuickData;
    [SerializeField] TextMeshProUGUI newSkillDescription;
    [Space(10)]
    [SerializeField] Transform skillIconHeader;
    [SerializeField] Transform skillAOEDiagramHeader;
    [Header("Mastery UI")]
    [SerializeField] Transform attributeBadgeHeader;
    [SerializeField] TextMeshProUGUI attributeChange;
    [Space(10)]
    [SerializeField] TextMeshProUGUI currentMasteryName;
    [SerializeField] TextMeshProUGUI currentMasteryReward;
    [Space(5)]
    [SerializeField] TextMeshProUGUI newMasteryName;
    [SerializeField] TextMeshProUGUI newMasteryReward;
    [Header("Prefabs")]
    [SerializeField] GameObject masteryPrefab;
    [SerializeField] GameObject bonusPrefab;
    [SerializeField] GameObject expGainPrefab;
    [Space(10)]
    [SerializeField] GameObject lootablePrefab;
    [SerializeField] float lootSpawnOffset = 0.5f;

    //Saving Data
    [SerializeField, HideInInspector]
    List<DroppedLootState> spawnedLootStates = new List<DroppedLootState>();
    private bool isDataRestored;

    //Storage
    List<LevelUpResult> levelUpRewardsDataToDisplay = new List<LevelUpResult>();
    List<GameObject> lootPool = new List<GameObject>();

    //Event
    public static Func<PartyMemberData, int, LevelUpResult> PlayerLevelledUp; //Params: Player; newLevel


    [System.Serializable]
    public class MasteryUIHeader
    {
        public TextMeshProUGUI playerName;
        public Transform masteryHeader;
    }

    public class NewMastery
    {
        public PlayerGridUnit player;

        public MasteryProgression currentProgression;
        public MasteryProgression newProgression;

        public NewMastery(PlayerGridUnit player, MasteryProgression currentProgression, MasteryProgression newProgression)
        {
            this.player = player;
            this.currentProgression = currentProgression;
            this.newProgression = newProgression;
        }
    }

    //Character Variables
    CharacterGridUnit KOEDUnit;
    GameObject KOCam;

    PlayerStateMachine playerStateMachine = null;
    PlayerGridUnit playerWhoDealtFinalBlow;
    Animator starActorAnimator;

    string activeModelName = "";

    //Data
    float battleTime;
    int currentScene = 0;

    bool triggerSkillForgetting = false;
    int currentNewMasteryIndex = 0;

    //New Skills
    List<PlayerBaseSkill> newSkills = new List<PlayerBaseSkill>();
    List<NewMastery> newMasteries = new List<NewMastery>();

    //Loot
    List<Item> droppedItems = new List<Item>();

    //Cache
    Transform leader;
    

    private void OnEnable()
    {
        ControlsManager.Instance.SubscribeToPlayerInput("Victory", this);
        SavingLoadingManager.Instance.EnteringNewTerritory += OnEnterNewTerritory;
        SavingLoadingManager.Instance.NewSceneLoadComplete += OnNewSceneLoaded;
    }

    public void OnVictory(CharacterGridUnit KOEDUnit, float battleTime)
    {
        ControlsManager.Instance.DisableControls();

        currentScene = 0;
        activeModelName = "";

        levelUpRewardsDataToDisplay.Clear();

        this.KOEDUnit = KOEDUnit;
        this.battleTime = battleTime;
        KOCam = KOEDUnit.koCam;

        playerWhoDealtFinalBlow = KOEDUnit.Health().attacker ? KOEDUnit.Health().attacker as PlayerGridUnit : FantasyCombatManager.Instance.TeamAttackInitiator as PlayerGridUnit;

        FantasyCombatManager.Instance.ShowHUD(false);

        playerWhoDealtFinalBlow.unitAnimator.ShowModel(false);

        ActivateFinalKOCam(true);

        StartCoroutine(VictoryRoutine());          
    }

    private void OnNewSceneLoaded(SceneData newSceneData)
    {
        //Needs to update position every scene, so do this on every new scene load.
        FantasySceneData fantasySceneData = newSceneData as FantasySceneData;

        if (!fantasySceneData)
        {
            return;
        }

        leader = PartyManager.Instance.GetLeader().transform;
        playerStateMachine = leader.GetComponent<PlayerStateMachine>();

        Transform victoryPos = fantasySceneData.victoryTransform;
        transform.position = victoryPos.position;
        transform.rotation = victoryPos.rotation;
    }

    private void OnEnterNewTerritory()
    {
        //When Entering New Territory Discard All Loot
        foreach (GameObject obj in lootPool)
        {
            obj.GetComponent<DroppedLoot>().DiscardLoot();
            obj.SetActive(false);
        }
    }

    private void NextScene()
    {
        AudioManager.Instance.PlaySFX(SFXType.CombatMenuSelect);

        progressionCanvasGroups[currentScene].DOFade(0, fadeOutTime);

        if(currentScene + 1 < progressionCanvasGroups.Count)
        {
            currentScene++;
            FadeInCurrentUIScene();
        }
        else if (triggerSkillForgetting)
        {
            //Trigger Skill FOrgetting
        }
        else if (newSkills.Count > 0)
        {
            ShowNewSkill();
        }
        else if (newMasteries.Count > 0)
        {
            newSkillCanvas.DOFade(0, fadeOutTime);
            ShowNewMastery();
        }
        else
        {
            StartCoroutine(ReturnToGameRoutine());
        }
        
    }

    IEnumerator VictoryRoutine()
    {
        yield return new WaitForSeconds(FantasyCombatManager.Instance.GetSkillFeedbackDisplayTime() + canvasDelay);
        KOEDUnit.unitAnimator.ActivateSlowmo();
        victoryCanvas.alpha = 1;
        victoryCanvas.gameObject.SetActive(true);
        yield return new WaitForSeconds(victoryCanvasDisplayTime);
        KOEDUnit.unitAnimator.ReturnToNormalSpeed();
        yield return new WaitForSeconds(fadeToVictorySceneDelay);
        FadeToBlack(true);
        yield return new WaitForSeconds(fadeInTime);
        ActivateVictoryScene(true);
        SetupVictoryData();
        FadeToBlack(false);
        yield return new WaitForSeconds(fadeOutTime);
        BeginVictoryData();
    }


    IEnumerator ReturnToGameRoutine()
    {
        ControlsManager.Instance.DisableControls();
        ShowControls(false);
        progressionCanvasGroups[currentScene].DOFade(0, fadeOutTime);
        newSkillCanvas.DOFade(0, fadeOutTime);
        newMasteryCanvas.DOFade(0, fadeOutTime);
        fader.DOFade(1, fadeInTime);
        yield return new WaitForSeconds(fadeInTime);
        ActivateVictoryScene(false);
        fader.DOFade(0, fadeOutTime);

        BattleType battleType = FantasyCombatManager.Instance.battleTrigger.battleType;

        //Play Roam Music
        if (battleType == BattleType.Normal || battleType == BattleType.MonsterChest)
            AudioManager.Instance.PlayMusic(MusicType.Roam);

        yield return new WaitForSeconds(fadeOutTime);
        fader.gameObject.SetActive(false);

        if(battleType == BattleType.Normal)
            ControlsManager.Instance.SwitchCurrentActionMap("Player");
            
        foreach(CanvasGroup canvas in progressionCanvasGroups)
        {
            canvas.gameObject.SetActive(false);
        }

        foreach (CanvasGroup canvas in masteryCanvasGroups)
        {
            canvas.gameObject.SetActive(false);
        }

        victoryCanvas.gameObject.SetActive(false);
        newMasteryCanvas.gameObject.SetActive(false);
        newSkillCanvas.gameObject.SetActive(false);
    }

    private void BeginVictoryData()
    {
        ControlsManager.Instance.SwitchCurrentActionMap("Victory");
        victoryCanvas.DOFade(0, fadeOutTime);

        ShowControls(true);
        FadeInCurrentUIScene();
    }

    private void FadeInCurrentUIScene()
    {
        progressionCanvasGroups[currentScene].alpha = 0;
        progressionCanvasGroups[currentScene].gameObject.SetActive(true);
        progressionCanvasGroups[currentScene].DOFade(1, fadeInTime).OnComplete(() => PlayUIAnimation());
    }

    private void ShowControls(bool fadeIn)
    {
        if (fadeIn)
        {
            controlsCanvasGroup.alpha = 0;
            controlsCanvasGroup.gameObject.SetActive(true);
        }

        float fadeEndValue = fadeIn ? 1 : 0;
        float fadeTime = fadeIn ? fadeInTime : fadeOutTime;
        
        controlsCanvasGroup.DOFade(fadeEndValue, fadeTime);
    }




    private void SetupVictoryData()
    {
        ClearUI();

        List<PlayerGridUnit> allPlayers = FantasyCombatManager.Instance.GetPlayerCombatParticipants(true, true); //Fled Units will not receive XP

        //Set Items.
        foreach(CharacterGridUnit enemy in FantasyCombatManager.Instance.GetEnemyCombatParticipants(true, true))
        {
            EnemyUnitStats enemyStats = enemy.stats as EnemyUnitStats;
            List<ItemDropChance> dropItems = enemyStats.dropItems.Concat(enemyStats.data.droppedIngridients).ToList();

            foreach (ItemDropChance droppedItemChance in dropItems)
            {
                int randNum = UnityEngine.Random.Range(0, 101);

                if(randNum <= droppedItemChance.dropChance)
                {
                    droppedItems.Add(droppedItemChance.item);

                    //If Item in Being Data then add to database
                    if (enemyStats.data.droppedIngridients.Contains(droppedItemChance))
                    {
                        EnemyDatabase.Instance.UpdateEnemyDrops(enemyStats.data, droppedItemChance.item);
                    }
                }
            }
        }

        //Set Battle Time
        TimeSpan timeSpan = TimeSpan.FromSeconds(battleTime);
        battleTimeText.text = string.Format("{0:D2}:{1:D2}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);

        //Set EXP Multiplier
        foreach (TextMeshProUGUI text in expMultplierTexts)
        {
            text.text = ProgressionManager.Instance.GetEXPMultiplier().ToString("F1");
        }

        List<string> countedBonuses = new List<string>();

        //Set Bonuses
        foreach (StrategicBonus bonus in ProgressionManager.Instance.GetStrategicBonuses())
        {
            if (countedBonuses.Contains(bonus.bonusName))
            {
                continue;
            }

            GameObject bonusInstance = Instantiate(bonusPrefab, bonusHeader);
            StrategicBonusUI bonusUI = bonusInstance.GetComponent<StrategicBonusUI>();
            int bonusCount = ProgressionManager.Instance.GetStrategicBonuses().Where((item) => item.bonusName == bonus.bonusName).Count();

            string bonusAmount = "+" + bonus.expMultiplier.ToString() + "% * " + bonusCount.ToString();

            bonusUI.Setup(bonus.bonusName, bonusAmount);

            countedBonuses.Add(bonus.bonusName);
        }

        foreach (MasteryUIHeader currentHeader in masteryUIHeaders)
        {
            currentHeader.masteryHeader.parent.gameObject.SetActive(false);
        }

        int masteryCounter = 0;

        //Set Masteries
        foreach (PlayerGridUnit player in allPlayers)
        {
            MasteryUIHeader currentHeader = masteryUIHeaders[masteryCounter];

            List<ProgressionManager.PlayerMasteryData> masteryDatas = ProgressionManager.Instance.GetMasteryProgressions(player);

            if (masteryDatas.Count > 0)
            {
                currentHeader.playerName.text = player.unitName;
                currentHeader.masteryHeader.parent.gameObject.SetActive(true);

                foreach(ProgressionManager.PlayerMasteryData masteryData in masteryDatas)
                {
                    GameObject masteryInstance = Instantiate(masteryPrefab, currentHeader.masteryHeader);
                    MasteryProgressionUI masteryProgressionUI = masteryInstance.GetComponent<MasteryProgressionUI>();

                    masteryProgressionUI.Setup(masteryData.currentMasteryProgression.progressionName, masteryData.currentMasteryProgression.requiredCountToComplete, masteryData.currentMasteryCount, masteryData.newMasteryCount);

                    //Update Mastery
                    if (masteryData.currentMasteryComplete && masteryData.nextMasteryProgression)
                        newMasteries.Add(new NewMastery(player, masteryData.currentMasteryProgression, masteryData.nextMasteryProgression));
                
                }

                masteryCounter++;
            }
        }

        //Set XP
        foreach(PlayerGridUnit player in allPlayers)
        {
            GameObject XPGainObj = Instantiate(expGainPrefab, experienceHeader);
            XPGainUI xPGainUI = XPGainObj.GetComponent<XPGainUI>();

            ProgressionManager.PlayerXPData playerXPData = ProgressionManager.Instance.RewardExperience(player.unitName);

            //Call Level up Event
            if (playerXPData.levelledUp)
            {
                InvokeLevelUpEvent(player.partyMemberData, playerXPData);
            }

            float barStartValue = (float)playerXPData.currentExperience / ProgressionManager.Instance.GetNextLevelBenchmark(playerXPData.currentLevel);
            float barEndValue = (float)(playerXPData.currentExperience + playerXPData.experienceGained) / ProgressionManager.Instance.GetNextLevelBenchmark(playerXPData.currentLevel + playerXPData.levelsGained);

            xPGainUI.Setup(player.unitName, playerXPData.experienceGained, barStartValue, barEndValue, playerXPData.currentLevel, playerXPData.levelledUp, playerXPData.levelsGained);
        }
    }

    private void InvokeLevelUpEvent(PartyMemberData player, ProgressionManager.PlayerXPData XPData)
    {
        foreach (Func<PartyMemberData, int, LevelUpResult> listener in PlayerLevelledUp.GetInvocationList())
        {
            LevelUpResult result = listener.Invoke(player, XPData.newLevel);

            if(result != null)
            {
                levelUpRewardsDataToDisplay.Add(result);
            }
        }

        foreach (LevelUpResult result in levelUpRewardsDataToDisplay)
        {
            if(result is SkillEarned)
            {
                //DO SOMETHING
            }
            else if(result is SkillPointEarned)
            {
                //DO SOMETHING
            }
        }
    }

    private GameObject SpawnLoot()
    {
        if(droppedItems.Count == 0) { return null; }

        GameObject spawnedLoot;
        GameObject fetchedLoot = lootPool.FirstOrDefault((loot) => !loot.activeInHierarchy);

        if (fetchedLoot)
        {
            spawnedLoot = fetchedLoot;
            spawnedLoot.transform.position = GetLootPos();
        }
        else
        {
            spawnedLoot = Instantiate(lootablePrefab, GetLootPos() , Quaternion.identity);
            lootPool.Add(spawnedLoot);
        }

        DroppedLoot lootData = spawnedLoot.GetComponent<DroppedLoot>();
        lootData.Setup(droppedItems);

        return spawnedLoot;
    }

    public void SPAWNTESTLOOT()
    {
        int randNum = UnityEngine.Random.Range(0, 11);
        droppedItems = TheCache.Instance.ListOfRandomItems(randNum);
        SpawnLoot();
    }


    private void ShowNewSkill()
    {
        AudioManager.Instance.PlaySFX(SFXType.TaDa);

        DeactivateCams();
        HideActorsUnderHeader(actorsHeader);

        PlayerBaseSkill newSkill = newSkills[0];
        PlayerGridUnit skillOwner = newSkill.GetSkillOwner();

        triggerSkillForgetting = !skillOwner.playerSkillset.HasAvailableMemory();

        newSkillCanvas.alpha = 0;
        newSkillCanvas.gameObject.SetActive(true);

        TriggerNewSkillAnim(skillOwner.unitName);

        UpdateNewSkillUI(newSkill);

        newSkills.RemoveAt(0);
        newSkillCanvas.DOFade(1, fadeInTime);
    }

    private void ShowNewMastery()
    {
        DeactivateCams();
        HideActorsUnderHeader(actorsHeader);

        NewMastery mastery = newMasteries[0];
        PlayerGridUnit player = mastery.player;

        TriggerNewSkillAnim(player.unitName);

        if (currentNewMasteryIndex == 0)
        {
            AudioManager.Instance.PlaySFX(SFXType.TaDa);

            UpdateNewMasteryUI(mastery);

            newMasteryCanvas.alpha = 0;
            newMasteryCanvas.gameObject.SetActive(true);

            masteryCanvasGroups[0].alpha = 1;
            masteryCanvasGroups[1].alpha = 0;

            masteryCanvasGroups[0].gameObject.SetActive(true);
            newMasteryCanvas.DOFade(1, fadeInTime);

            currentNewMasteryIndex++;
        }
        else
        {
            newMasteries.RemoveAt(0);
            masteryCanvasGroups[1].gameObject.SetActive(true);

            masteryCanvasGroups[0].DOFade(0, fadeOutTime);
            masteryCanvasGroups[1].DOFade(1, fadeInTime);

            currentNewMasteryIndex = 0;
        }
    }

    private void TriggerNewSkillAnim(string playerName)
    {
        foreach (Transform setup in newSkillSetupHeader)
        {
            bool activate = setup.name == playerName;
            setup.gameObject.SetActive(activate);

            if (activate)
            {
                Animator animator = setup.GetComponentInChildren<Animator>();

                //Only Trigger If Anim not already playing
                if(!animator.GetCurrentAnimatorStateInfo(0).IsTag("Skill"))
                    animator.SetTrigger("NewSkill");
            }
        }
    }

    private void ActivateVictoryScene(bool activate)
    {
        ActivateFinalKOCam(false);

        if (activate)
        {
            //Play Victory Music
            AudioManager.Instance.PlayMusic(MusicType.Victory);

            //Deactive Enemies.
            foreach (CharacterGridUnit character in FantasyCombatManager.Instance.GetEnemyCombatParticipants(true, true))
            {
                character.ActivateUnit(false);
            }
        }
        else
        {
            HideActorsUnderHeader(actorsHeader);
            HideActorsUnderHeader(newSkillSetupHeader);
        }

        //Show/Hide Player Models.
        foreach (CharacterGridUnit character in PartyManager.Instance.GetActivePlayerParty())
        {
            character.unitAnimator.ResetAnimatorToRoamState();
            character.unitAnimator.ShowModel(!activate);

            if (activate)
            {
                character.ActivateUnit(true);
                character.Health().BattleComplete();
            }
        }

        if (activate)
        {
            //Switch Players To Default State.
            FantasyCombatManager.Instance.CombatEnded?.Invoke(BattleResult.Victory, FantasyCombatManager.Instance.battleTrigger);

            //Place characters at Position.
            if (FantasyCombatManager.Instance.battleTrigger.CanPlayStoryVictoryScene())
            {
                Debug.Log("STORY VICTORY SCENE NOT IMPLEMENTED");
                throw new System.NotImplementedException();
            }
            else
            {
                SingleVictorSetup();
            }
        }
        else
        {
            FantasyCombatManager.Instance.WarpPlayerToPostCombatPos(playerStateMachine);

            //Activate Loot
            GameObject spawnedLoot = SpawnLoot();
            DeactivateCams();

            FantasyCombatManager.Instance.battleTrigger.TriggerVictoryEvent(spawnedLoot, fadeOutTime);
        }
    }

    private void PlayUIAnimation()
    {
        foreach (MasteryUIHeader header in masteryUIHeaders)
        {
            foreach (Transform child in header.masteryHeader)
            {
                if (child.gameObject.activeInHierarchy)
                {
                    child.GetComponent<MasteryProgressionUI>().PlayAnimation();
                }
            }
        }

        foreach (Transform child in experienceHeader)
        {
            if (child.gameObject.activeInHierarchy)
            {
                child.GetComponent<XPGainUI>().PlayAnimation();
            }
        }
    }



    private void SingleVictorSetup()
    {
        singleVictorCam.SetActive(true);

        int counter = 1;
        //Set Victor
        foreach (CharacterGridUnit player in FantasyCombatManager.Instance.GetPlayerCombatParticipants(true, true))
        {
            GameObject actor = actorsHeader.Find(player.unitName).gameObject;

            if (player == playerWhoDealtFinalBlow)
            {
                actor.transform.position = singleVictorPostionHeader.GetChild(0).position;
                actor.transform.rotation = singleVictorPostionHeader.GetChild(0).rotation;
                actor.SetActive(true);
                starActorAnimator = actor.GetComponent<Animator>();
                continue;
            }

            actor.transform.position = singleVictorPostionHeader.GetChild(counter).position;
            actor.transform.rotation = singleVictorPostionHeader.GetChild(counter).rotation;
            actor.SetActive(true);
            counter++;
        }

        starActorAnimator.SetTrigger("Celebrate");
        StartFall();
    }



    private void StartFall()
    {
        foreach (Transform actor in actorsHeader)
        {
            if (actor.gameObject.activeInHierarchy && actor.name != playerWhoDealtFinalBlow.unitName)
            {
                StartCoroutine(FallRoutine(actor));
            }
        }
    }

    IEnumerator FallRoutine(Transform fallingTransform)
    {
        Animator animator = fallingTransform.GetComponent<Animator>();
        animator.SetTrigger("Fall");

        float fallTime = (fallingTransform.position.y) / fallSpeed;
        float timeToLand = (fallingTransform.position.y - heightToStopFall) / fallSpeed;
        fallingTransform.DOMoveY(0, fallTime).SetEase(Ease.InQuad);

        yield return new WaitForSeconds(timeToLand);
        
        animator.SetTrigger("Land");
    }

    private void ActivateFinalKOCam(bool show)
    {
        KOCam.SetActive(show);
    }

    private void FadeToBlack(bool fadeIn)
    {
        int faderStartValue = fadeIn ? 0 : 1;
        fader.alpha = faderStartValue;
        fader.gameObject.SetActive(true);
        int faderEndValue = fadeIn ? 1 : 0;
        float fadeTime = fadeIn ? fadeInTime : fadeOutTime;
        fader.DOFade(faderEndValue, fadeTime);
    }

    //Helper Methods
    private void UpdateNewSkillUI(PlayerBaseSkill newSkill)
    {
        //Clean AOE Header
        foreach(Transform child in skillAOEDiagramHeader)
        {
            Destroy(child.gameObject);
        }

        newSkillName.text = newSkill.skillName;
        newSkillQuickData.text = newSkill.quickData;
        newSkillDescription.text = newSkill.description;

        Instantiate(newSkill.aoeDiagram, skillAOEDiagramHeader);

        //Activate Icon
        foreach(Transform icon in skillIconHeader)
        {
            icon.gameObject.SetActive(icon.GetSiblingIndex() == newSkill.GetSkillIndex());
        }
    }

    private void UpdateNewMasteryUI(NewMastery masteryData)
    {
        foreach(Transform child in attributeBadgeHeader)
        {
            int indexToActivate = (int)masteryData.currentProgression.rewardAttribute;
            child.gameObject.SetActive(child.GetSiblingIndex() == indexToActivate);
        }

        UnitStats playerStats = masteryData.player.stats;

        int newAttribute = playerStats.GetAttributeValue(masteryData.currentProgression.rewardAttribute);
        int oldAttribute = newAttribute - masteryData.currentProgression.rewardPoints;

        attributeChange.text = oldAttribute + "<color=#D39C2F>  >></color> <color=white> " + newAttribute + "</color>";

        currentMasteryName.text = masteryData.currentProgression.description;
        currentMasteryReward.text = "+" + masteryData.currentProgression.rewardPoints.ToString() + " " + masteryData.currentProgression.rewardAttribute.ToString();

        newMasteryName.text = masteryData.newProgression.description;
        newMasteryReward.text = "+" + masteryData.newProgression.rewardPoints.ToString() + " " + masteryData.newProgression.rewardAttribute.ToString();
    }

    private void HideActorsUnderHeader(Transform header)
    {
        foreach (Transform actor in header)
        {
            actor.gameObject.SetActive(false);
        }
    }

    private void DeactivateCams()
    {
        singleVictorCam.SetActive(false);
        doubleVictorCam.SetActive(false);
    }

    public Vector3 GetLootPos()
    {
        return leader.transform.position + leader.transform.forward * lootSpawnOffset;
    }

    private void ClearUI()
    {
        droppedItems.Clear();
        newSkills.Clear();
        newMasteries.Clear();

        currentNewMasteryIndex = 0;

        foreach (Transform bonus in bonusHeader)
        {
            Destroy(bonus.gameObject);
        }

        foreach (MasteryUIHeader header in masteryUIHeaders)
        {
            foreach (Transform child in header.masteryHeader)
            {
                Destroy(child.gameObject);
            }
        }

        foreach (Transform child in experienceHeader)
        {
            Destroy(child.gameObject);
        }
    }

    private void OnDisable()
    {
        SavingLoadingManager.Instance.EnteringNewTerritory -= OnEnterNewTerritory;
        SavingLoadingManager.Instance.NewSceneLoadComplete -= OnNewSceneLoaded;
    }

    //Input

    private void OnNext(InputAction.CallbackContext context)
    {
        if (context.action.name != "Next") { return; }

        if (context.performed)
        {
            NextScene();
            
        }
    }

    public void ListenToInput(bool listen)
    {
        if (listen)
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnNext;
        }
        else
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnNext;
        }
    }

    private void OnDestroy()
    {
        ControlsManager.Instance.RemoveIControls(this);
    }

    //Saving
    [System.Serializable]
    public class DroppedLootState
    {
        public Vector3 postion = Vector3.zero;
        public Quaternion rotation = Quaternion.identity;

        public List<string> itemIDs = new List<string>();
    }
    public object CaptureState()
    {
        spawnedLootStates.Clear();

        foreach (GameObject obj in lootPool)
        {
            if (!obj.activeInHierarchy) { continue; } //Inactive means Empty

            List<Item> lootItems = new List<Item>(obj.GetComponent<DroppedLoot>().GetItems());

            if(lootItems.Count == 0) { continue; }

            DroppedLootState lootState = new DroppedLootState();

            lootState.postion = obj.transform.position;
            lootState.rotation = obj.transform.rotation;

            lootState.itemIDs = lootItems.ConvertAll((item) => item.GetID());

            spawnedLootStates.Add(lootState);
        }
        
        return SerializationUtility.SerializeValue(spawnedLootStates, DataFormat.Binary);
    }

    public void RestoreState(object state)
    {
        isDataRestored = true;

        if (state == null)
        {
            return;
        }

        byte[] bytes = state as byte[];
        spawnedLootStates = SerializationUtility.DeserializeValue<List<DroppedLootState>>(bytes, DataFormat.Binary);

        //Deactivate all Loot so it can be marked as unused.
        foreach (GameObject obj in lootPool)
        {
            obj.SetActive(false);
        }

        foreach (DroppedLootState lootState in spawnedLootStates)
        {
            droppedItems = TheCache.Instance.GetItemsById(lootState.itemIDs);
            GameObject spawnedLoot = SpawnLoot();

            spawnedLoot.transform.position = lootState.postion;
            spawnedLoot.transform.rotation = lootState.rotation;
        }
    }

    public bool IsDataRestored()
    {
        return isDataRestored;
    }
}
