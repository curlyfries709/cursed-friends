using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

public class ProgressionManager : MonoBehaviour, ISaveable, IMultiWorldCombatContacter
{
    public static ProgressionManager Instance { get; private set; }

    [Title("Masteries")]
    [SerializeField] Transform masteryTrackerHeader;
    [Title("LEVEL CALCULATIONS")]
    [SerializeField] int maxLevel = 99;
    [Space(10)]
    [SerializeField] int levelTwoXPBenchmark = 100;
    [SerializeField] int levellingIncrement = 100;
    [Title("LEVEL BENCHMARKS")]
    [ListDrawerSettings(Expanded = true, ShowIndexLabels = true)]
    [ReadOnly]
    [SerializeField] List<int> levelBenchmarks;
    [Space(10)] //To add space between button

    //Saving Data
    [SerializeField, HideInInspector]
    private ProgressionState progressionState = new ProgressionState();
    private bool isDataRestored = false;

    //Combat Dicts
    Dictionary<PlayerGridUnit, List<CharacterGridUnit>> enemiesKOEDWhilePlayerKOEDDict = new Dictionary<PlayerGridUnit, List<CharacterGridUnit>>();

    //Progression Dicts
    Dictionary<string, int> currentExperienceDict = new Dictionary<string, int>();
    Dictionary<string, int> currentLevelDict = new Dictionary<string, int>();

    Dictionary<string, List<PlayerMasteryProgression>> masteryProgressionOnCombatBegin = new Dictionary<string, List<PlayerMasteryProgression>>();
    Dictionary<string, List<PlayerMasteryProgression>> playerCurrentMasteryProgression = new Dictionary<string, List<PlayerMasteryProgression>>();

    List<StrategicBonus> achievedStrategicBonuses = new List<StrategicBonus>();
    List<BaseMasteryTracker> masteryTrackers = new List<BaseMasteryTracker>();

    public class PlayerCurrentAttributeMastery
    {
        public Attribute masteryAttribute;
        public int currentIndex = 0;
        public int progressionCount = 0;
    }


    public class PlayerMasteryProgression
    {
        public MasteryProgression progression;
        public int count;

        public PlayerMasteryProgression(){}

        public PlayerMasteryProgression(PlayerMasteryProgression progressionToClone)
        {
            progression = progressionToClone.progression;
            count = progressionToClone.count;
        }
    }

    public struct PlayerMasteryData
    {
        public int currentMasteryCount;
        public int newMasteryCount;

        public MasteryProgression currentMasteryProgression;
        public bool currentMasteryComplete;
        public MasteryProgression nextMasteryProgression;

        public PlayerMasteryData(MasteryProgression currentMasteryProgression, bool isComplete, MasteryProgression nextMasteryProgression, int currentMasteryCount, int newMasteryCount)
        {
            currentMasteryComplete = isComplete;

            this.nextMasteryProgression = nextMasteryProgression;
            this.currentMasteryProgression = currentMasteryProgression;

            this.currentMasteryCount = currentMasteryCount;
            this.newMasteryCount = newMasteryCount;
        }
    }

    public struct PlayerXPData
    {
        public int currentExperience;
        public int currentLevel;
        public int experienceGained;
        public int newLevelBenchmark;
        public int levelsGained;
        public int newLevel;

        public bool levelledUp;

        public PlayerXPData(int currentExperience, int currentLevel, int experienceGained, int newLevelBenchmark, bool levelledUp, int levelsGained, int newLevel)
        {
            this.currentExperience = currentExperience;
            this.currentLevel = currentLevel;
            this.experienceGained = experienceGained;
            this.newLevelBenchmark = newLevelBenchmark;
            this.levelledUp = levelledUp;
            this.levelsGained = levelsGained;
            this.newLevel = newLevel;
        }
    }

    private void Awake()
    {
        if (!Instance)
            Instance = this;

        masteryTrackers = masteryTrackerHeader.GetComponentsInChildren<BaseMasteryTracker>().ToList();
        ListenForCombatManagerSet();
    }

    public void SubscribeToCombatManagerEvents(bool subscribe)
    {
        if (subscribe)
        {
            FantasyCombatManager.Instance.CombatBegun += OnCombatBegin;
            FantasyCombatManager.Instance.CombatEnded += OnCombatEnd;
        }
        else
        {
            FantasyCombatManager.Instance.CombatEnded -= OnCombatEnd;
            FantasyCombatManager.Instance.CombatBegun -= OnCombatBegin;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        //Subscribe to Combat Events.
        Health.UnitKOed += OnUnitKO;
    }


    private void NewGameSetup()
    {
        //Set Char EXP  
        foreach (PartyMemberData partyMember in PartyManager.Instance.GetAllPartyMembersData())
        {
            currentExperienceDict[partyMember.memberName] = 0;
            currentLevelDict[partyMember.memberName] = CalculateLevel(partyMember.memberName);

            //Mastery Dicts
            playerCurrentMasteryProgression[partyMember.memberName] = new List<PlayerMasteryProgression>();

            foreach (BaseMasteryTracker tracker in masteryTrackers)
            {
                PlayerMasteryProgression playerMasteryProgression = new PlayerMasteryProgression();

                playerMasteryProgression.progression = tracker.myMastery.sequencedProgressions[0];
                playerMasteryProgression.count = 0;

                playerCurrentMasteryProgression[partyMember.memberName].Add(playerMasteryProgression);
            }
        }

    }


    [Button("Generate Level Benchmarks")]
    private void SetLevelBenchMarks() //Called Via Editor Button
    {
        Debug.Log("SETTING LEVEL BENCHMARKS ");

        levelBenchmarks[0] = -5;
        levelBenchmarks[1] = -1;

        for (int level = 2; level <= maxLevel; level++)
        {
            if(level == 2)
            {
                levelBenchmarks[level] = levelTwoXPBenchmark;
                continue;
            }

            //Increment = 100[Difference between Lvl 1 & 2] + (100[Increment Constant] x (Current Lvl - 2))
            //Benchmark: Previous Level Benchmark + Increment

            int previousBenchmark = levelBenchmarks[level - 1];
            int increment = levelTwoXPBenchmark + (levellingIncrement * (level - 2));
            int benchmark = previousBenchmark + increment;

            if (level < levelBenchmarks.Count)
            {
                levelBenchmarks[level] = benchmark;
            }
            else
            {
                levelBenchmarks.Insert(level, benchmark);
            }
        }
    }

    void OnCombatBegin(BattleStarter.CombatAdvantage advantageType)
    {
        ResetDataAtCombatStart();

        foreach (BaseMasteryTracker tracker in masteryTrackers)
        {
            //Dictionary for current tracker attribute.
            Attribute trackedAttribute = tracker.myMastery.masteryAttribute;
            Dictionary<PlayerGridUnit, MasteryProgression> progressionForAttribute = new Dictionary<PlayerGridUnit, MasteryProgression>();

            foreach(PlayerGridUnit player in GetAllPlayers())
            {
                progressionForAttribute[player] = playerCurrentMasteryProgression[player.unitName].FirstOrDefault((playerProgression) => playerProgression.progression.rewardAttribute == trackedAttribute).progression;
            }

            tracker.OnCombatBegin(progressionForAttribute);
        }

        //Set Old Dict 
        masteryProgressionOnCombatBegin.Clear();

        foreach(var item in playerCurrentMasteryProgression)
        {
            masteryProgressionOnCombatBegin[item.Key] = new List<PlayerMasteryProgression>();

            foreach(PlayerMasteryProgression progression in item.Value)
            {
                masteryProgressionOnCombatBegin[item.Key].Add(new PlayerMasteryProgression(progression));
            }
        }
    }

    private void ResetDataAtCombatStart()
    {
        achievedStrategicBonuses.Clear();
        enemiesKOEDWhilePlayerKOEDDict.Clear();
    }

    void OnCombatEnd(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        foreach (BaseMasteryTracker tracker in masteryTrackers)
        {
            if (battleResult == BattleResult.Victory)
            {
                //Store New Count Data
                Attribute trackedAttribute = tracker.myMastery.masteryAttribute;
                Dictionary<PlayerGridUnit, int> playerCombatProgression = tracker.GetAllPlayersCombatProgression();

                foreach (var item in playerCombatProgression)
                {
                    PlayerMasteryProgression progression = playerCurrentMasteryProgression[item.Key.unitName].FirstOrDefault((playerProgression) => playerProgression.progression.rewardAttribute == trackedAttribute);
                    int newCount = progression.count + playerCombatProgression[item.Key];
                    progression.count = newCount;
                }
            }

            //Clear Data
            tracker.OnCombatEnd();
        }
    }

    private void OnDisable()
    {
        Health.UnitKOed -= OnUnitKO;
    }

    private void OnUnitKO(GridUnit unit)
    {
        PlayerGridUnit player = unit as PlayerGridUnit;

        if (player)
        {
            //Player KOED
            if (!enemiesKOEDWhilePlayerKOEDDict.ContainsKey(player))
            {
                enemiesKOEDWhilePlayerKOEDDict[player] = new List<CharacterGridUnit>();
            }
        }
        else if(unit is CharacterGridUnit enemy)
        {
            //Enemy KOED
            foreach (KeyValuePair<PlayerGridUnit, List<CharacterGridUnit>> item in enemiesKOEDWhilePlayerKOEDDict)
            {
                if(item.Key.Health().isKOed) //Ensure Player is KOED, Could have been revived
                    enemiesKOEDWhilePlayerKOEDDict[item.Key].Add(enemy);
            }
        }
    }


    //STRATEIC BONUSES

    public void OnBonusAchieved(StrategicBonus strategicBonus)
    {
        achievedStrategicBonuses.Add(strategicBonus);
    }

    public List<StrategicBonus> GetStrategicBonuses()
    {
        return achievedStrategicBonuses;
    }

 
    public PlayerXPData RewardExperience(string player)
    {
        PlayerXPData playerXPData = new PlayerXPData();

        int XPGain = CalculatedXPGained(player);
        int currentLevel = currentLevelDict[player];

        playerXPData.currentExperience = currentExperienceDict[player];
        playerXPData.currentLevel = currentLevel;
        playerXPData.experienceGained = XPGain;

        //Earn EXP
        currentExperienceDict[player] = currentExperienceDict[player] + XPGain;

        //Level UP
        bool levelUp = LevelUp(player);
        int newLevel = Mathf.Min(currentLevelDict[player], maxLevel);

        playerXPData.newLevelBenchmark = levelBenchmarks[newLevel - 1];
        playerXPData.levelledUp = levelUp;
        playerXPData.levelsGained = newLevel - currentLevel;
        playerXPData.newLevel = newLevel;

        return playerXPData;
    }

    private void RewardAttributes(PlayerGridUnit player, Attribute attribute, int points)
    {
        PlayerUnitStats playerStat = player.stats as PlayerUnitStats;
        playerStat.ImproveAttribute(attribute, points);
    }

    public float GetEXPMultiplier()
    {
        float multiplier = 1;

        foreach(StrategicBonus bonus in achievedStrategicBonuses)
        {
            multiplier = multiplier + ((float)bonus.expMultiplier / 100);
        }

        return multiplier;
    }

    private bool LevelUp(string player)
    {
        int currentLevel = currentLevelDict[player];
        int newLevel = CalculateLevel(player);

        bool levelUp = newLevel > currentLevel;

        if (levelUp)
        {
            currentLevelDict[player] = newLevel;

            //Set Player Level
            PlayerUnitStats playerStats = PartyManager.Instance.GetPlayerUnitViaName(player).stats as PlayerUnitStats;
            playerStats.SetLevel(newLevel);
        }

        return levelUp;
    }

    private int CalculateLevel(string player)
    {
        for(int i = 0; i < levelBenchmarks.Count; i++)
        {
            if (currentExperienceDict[player] < levelBenchmarks[i])
            {
                return i - 1;
            }
        }

        return levelBenchmarks[levelBenchmarks.Count - 1];
    }


    public int GetLevel(string player)
    {
        return currentLevelDict[player];
    }

    public int GetNextLevelBenchmark(int currentLevel)
    {
        return levelBenchmarks[currentLevel + 1];
    }

    public int GetCurrentXP(string player)
    {
        return currentExperienceDict[player];
    }

    public int CalculatedXPGained(string playerName)
    {
        int XPSum = 0;

        PlayerGridUnit player = PartyManager.Instance.GetPlayerUnitViaName(playerName);

        foreach(CharacterGridUnit enemy in FantasyCombatManager.Instance.GetEnemyCombatParticipants(true, true))
        {
            if (!enemiesKOEDWhilePlayerKOEDDict.ContainsKey(player) || (enemiesKOEDWhilePlayerKOEDDict.ContainsKey(player) && !enemiesKOEDWhilePlayerKOEDDict[player].Contains(enemy)))
            {
                EnemyUnitStats enemyStat = enemy.stats as EnemyUnitStats;
                XPSum = XPSum + enemyStat.expGainOnDefeat;
            }
        }

        return Mathf.RoundToInt(XPSum * GetEXPMultiplier());
    }


    //MASTERY PROGRESSIONS
    public List<PlayerMasteryData> GetMasteryProgressions(PlayerGridUnit player) // CALLED ON VICTORY
    {
        List<PlayerMasteryData> masteryDatas = new List<PlayerMasteryData>();
        string playerName = player.unitName;

        foreach (PlayerMasteryProgression playerProgression in playerCurrentMasteryProgression[playerName])
        {
            PlayerMasteryData masteryData = new PlayerMasteryData();
            Attribute trackedAttribute = playerProgression.progression.rewardAttribute;

            masteryData.currentMasteryProgression = playerProgression.progression;

            masteryData.currentMasteryCount = masteryProgressionOnCombatBegin[playerName].FirstOrDefault((item) => item.progression == playerProgression.progression).count;
            masteryData.newMasteryCount = playerProgression.count;

            bool isMasteryComplete = playerProgression.count >= playerProgression.progression.requiredCountToComplete;
            masteryData.currentMasteryComplete = isMasteryComplete;

            if (isMasteryComplete)
            {
                //Update Mastery
                Mastery currentMastey = masteryTrackers.FirstOrDefault((tracker) => tracker.myMastery.masteryAttribute == trackedAttribute).myMastery;
                int currentProgressionIndex = currentMastey.sequencedProgressions.IndexOf(playerProgression.progression);

                MasteryProgression newProgression = currentProgressionIndex + 1 < currentMastey.sequencedProgressions.Count ? currentMastey.sequencedProgressions[currentProgressionIndex + 1] : null;
                masteryData.nextMasteryProgression = newProgression;

                //GIFT PLAYER ATTRIBUTE
                RewardAttributes(player, trackedAttribute, playerProgression.progression.rewardPoints);

                //Update Progression.
                playerProgression.progression = newProgression;
                playerProgression.count = 0;
            }

            if(masteryData.newMasteryCount - masteryData.currentMasteryCount > 0 || isMasteryComplete)
                masteryDatas.Add(masteryData);
        }

        return masteryDatas;
    }

    public PlayerMasteryProgression GetPlayerCurrentAttributeProgression(PlayerGridUnit player, Attribute attribute)
    {
        return playerCurrentMasteryProgression[player.unitName].FirstOrDefault((progression) => progression.progression.rewardAttribute == attribute);
    }

    public Mastery GetAttributeMastery(Attribute attribute)
    {
        return masteryTrackers.FirstOrDefault((tracker) => tracker.myMastery.masteryAttribute == attribute).myMastery;
    }

    public void ListenForCombatManagerSet()
    {
        GameSystemsManager.Instance.ListenForCombatManagerInitialization(this);
    }

    private List<PlayerGridUnit> GetAllPlayers()
    {
        return PartyManager.Instance.GetAllPlayerMembersInWorld();
    }


    //Saving
    [System.Serializable]
    public class ProgressionState
    {
        //XP
        public Dictionary<string, int> experienceDict = new Dictionary<string, int>();

        public Dictionary<string, List<PlayerCurrentAttributeMastery>> currentMasteries = new Dictionary<string, List<PlayerCurrentAttributeMastery>>();
    }


    public object CaptureState()
    {
        progressionState.experienceDict = currentExperienceDict;

        //Clear List First
        progressionState.currentMasteries.Clear();

        foreach(var item in playerCurrentMasteryProgression)
        {
            progressionState.currentMasteries[item.Key] = new List<PlayerCurrentAttributeMastery>();

            foreach(PlayerMasteryProgression progression in item.Value)
            {
                BaseMasteryTracker baseTracker = masteryTrackers.First((tracker) => tracker.myMastery.masteryAttribute == progression.progression.rewardAttribute);
                PlayerCurrentAttributeMastery storedMastery = new PlayerCurrentAttributeMastery();

                //Store Attribute
                storedMastery.masteryAttribute = progression.progression.rewardAttribute;
                //Store Count
                storedMastery.progressionCount = progression.count;
                //Store Index
                storedMastery.currentIndex = baseTracker.myMastery.sequencedProgressions.IndexOf(progression.progression);

                //Add to List
                progressionState.currentMasteries[item.Key].Add(storedMastery);
            }
        }

        return SerializationUtility.SerializeValue(progressionState, DataFormat.Binary);
    }
    public void RestoreState(object state)
    {
        isDataRestored = true;

        if (state == null) //Null On New Game or when testing in editor
        {
            NewGameSetup();
            return; 
        }

        byte[] bytes = state as byte[];
        progressionState = SerializationUtility.DeserializeValue<ProgressionState>(bytes, DataFormat.Binary);

        currentExperienceDict = progressionState.experienceDict;
        
        foreach (PlayerGridUnit player in GetAllPlayers())
        {
            //Set Level Dict
            currentLevelDict[player.unitName] = CalculateLevel(player.unitName);

            //Set Player Levels
            PlayerUnitStats playerStats = player.stats as PlayerUnitStats;
            playerStats.SetLevel(currentLevelDict[player.unitName]);

            //Set Mastery
            List<PlayerCurrentAttributeMastery> currentAttributeMastery = progressionState.currentMasteries[player.unitName];
            playerCurrentMasteryProgression[player.unitName] = new List<PlayerMasteryProgression>();

            foreach (PlayerCurrentAttributeMastery mastery in currentAttributeMastery)
            {
                //Find Data
                PlayerMasteryProgression playerMasteryProgression = new PlayerMasteryProgression();
                BaseMasteryTracker baseTracker = masteryTrackers.First((tracker) => tracker.myMastery.masteryAttribute == mastery.masteryAttribute);

                //Restore Data.
                playerMasteryProgression.progression = baseTracker.myMastery.sequencedProgressions[mastery.currentIndex];
                playerMasteryProgression.count = mastery.progressionCount;

                //Add To List
                playerCurrentMasteryProgression[player.unitName].Add(playerMasteryProgression);
            }
        }
    }

    public bool IsDataRestored()
    {
        return isDataRestored;
    }
}
