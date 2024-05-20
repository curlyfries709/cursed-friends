using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

public class ProgressionManager : MonoBehaviour, ISaveable
{
    public static ProgressionManager Instance { get; private set; }

    [Title("Talents")]
    [SerializeField] List<Talent> allTalents;
    [Title("Masteries")]
    [SerializeField] Transform masteryTrackerHeader;
    [Title("TALENT BENCHMARKS")]
    [ListDrawerSettings(Expanded = true, ShowIndexLabels = true)]
    [SerializeField] List<int> talentLevelBenchmarks;
    [Title("LEVEL CALCULATIONS")]
    [SerializeField] int maxLevel = 99;
    [Space(10)]
    [SerializeField] int levelTwoXPBenchmark = 100;
    [SerializeField] int levellingIncrement = 100;
    [Title("LEVEL BENCHMARKS")]
    [ListDrawerSettings(Expanded = true, ShowIndexLabels = true)]
    [ReadOnly]
    [SerializeField] List<int> levelBenchmarks;
    [Space(10)]

    //Saving Data
    [SerializeField, HideInInspector]
    private ProgressionState progressionState = new ProgressionState();
    public bool AutoRestoreOnNewTerritoryEntry { get; set; } = false;

    //Combat Dicts
    List<PlayerGridUnit> allPlayers = new List<PlayerGridUnit>();
    Dictionary<PlayerGridUnit, List<CharacterGridUnit>> enemiesKOEDWhilePlayerKOEDDict = new Dictionary<PlayerGridUnit, List<CharacterGridUnit>>();

    //Progression Dicts
    Dictionary<string, int> currentExperienceDict = new Dictionary<string, int>();
    Dictionary<string, int> currentLevelDict = new Dictionary<string, int>();
    Dictionary<string, int> talentProgression = new Dictionary<string, int>();

    Dictionary<string, List<PlayerMasteryProgression>> masteryProgressionOnCombatBegin = new Dictionary<string, List<PlayerMasteryProgression>>();
    Dictionary<string, List<PlayerMasteryProgression>> playerCurrentMasteryProgression = new Dictionary<string, List<PlayerMasteryProgression>>();

    List<StrategicBonus> achievedStrategicBonuses = new List<StrategicBonus>();
    List<BaseMasteryTracker> masteryTrackers = new List<BaseMasteryTracker>();

    public Action<int> LeaderLevelledUp;

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

        public bool levelledUp;

        public PlayerXPData(int currentExperience, int currentLevel, int experienceGained, int newLevelBenchmark, bool levelledUp, int levelsGained)
        {
            this.currentExperience = currentExperience;
            this.currentLevel = currentLevel;
            this.experienceGained = experienceGained;
            this.newLevelBenchmark = newLevelBenchmark;
            this.levelledUp = levelledUp;
            this.levelsGained = levelsGained;
        }
    }

    private void Awake()
    {
        Instance = this;
        masteryTrackers = masteryTrackerHeader.GetComponentsInChildren<BaseMasteryTracker>().ToList();

        SetPlayers();
    }

    private void OnEnable()
    {
        FantasyCombatManager.Instance.CombatBegun += OnCombatBegin;
        FantasyCombatManager.Instance.CombatEnded += OnCombatEnd;
    }

    // Start is called before the first frame update
    void Start()
    {
        //Subscribe to Combat Events.
        FantasyHealth.CharacterUnitKOed += OnUnitKO;
    }

    private void SetPlayers()
    {
        allPlayers = PartyData.Instance.GetAllPlayerMembersInWorld();
    }

    private void NewGameSetup()
    {
        //Set Char EXP  
        foreach (PlayerGridUnit player in allPlayers)
        {
            currentExperienceDict[player.unitName] = 0;
            currentLevelDict[player.unitName] = CalculateLevel(player.unitName);

            //Mastery Dicts
            playerCurrentMasteryProgression[player.unitName] = new List<PlayerMasteryProgression>();

            foreach (BaseMasteryTracker tracker in masteryTrackers)
            {
                PlayerMasteryProgression playerMasteryProgression = new PlayerMasteryProgression();

                playerMasteryProgression.progression = tracker.myMastery.sequencedProgressions[0];
                playerMasteryProgression.count = 0;

                playerCurrentMasteryProgression[player.unitName].Add(playerMasteryProgression);
            }
        }

        // Set talents
        foreach(Talent talent in allTalents)
        {
            talentProgression[talent.talentName] = 0;
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

        foreach(BaseMasteryTracker tracker in masteryTrackers)
        {
            //Dictionary for current tracker attribute.
            Attribute trackedAttribute = tracker.myMastery.masteryAttribute;
            Dictionary<PlayerGridUnit, MasteryProgression> progressionForAttribute = new Dictionary<PlayerGridUnit, MasteryProgression>();

            foreach(PlayerGridUnit player in allPlayers)
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
        FantasyHealth.CharacterUnitKOed -= OnUnitKO;

        FantasyCombatManager.Instance.CombatEnded -= OnCombatEnd;
        FantasyCombatManager.Instance.CombatBegun -= OnCombatBegin;
    }

    private void OnUnitKO(CharacterGridUnit unit)
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
        else
        {
            //Enemy KOED
            foreach (KeyValuePair<PlayerGridUnit, List<CharacterGridUnit>> item in enemiesKOEDWhilePlayerKOEDDict)
            {
                if(item.Key.Health().isKOed) //Ensure Player is KOED, Could have been revived
                    enemiesKOEDWhilePlayerKOEDDict[item.Key].Add(unit);
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
        int newLevel = currentLevelDict[player];

        playerXPData.newLevelBenchmark = levelBenchmarks[newLevel - 1];
        playerXPData.levelledUp = levelUp;
        playerXPData.levelsGained = newLevel - currentLevel;

        if(player == PartyData.Instance.GetLeaderName() && levelUp)
        {
            LeaderLevelledUp?.Invoke(newLevel);
        }

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
        int curretLevel = currentLevelDict[player];
        int newLevel = CalculateLevel(player);

        bool levelUp = newLevel > curretLevel;

        if (levelUp)
        {
            currentLevelDict[player] = newLevel;

            //Set Player Level
            PlayerUnitStats playerStats = PartyData.Instance.GetPlayerUnitViaName(player).stats as PlayerUnitStats;
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

    public int GetTalentLevel(Talent talent)
    {
        for (int i = 0; i < talentLevelBenchmarks.Count; i++)
        {
            if (talentProgression[talent.talentName] < talentLevelBenchmarks[i])
            {
                return i - 1;
            }
        }

        return talentLevelBenchmarks[talentLevelBenchmarks.Count - 1];
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

        PlayerGridUnit player = PartyData.Instance.GetPlayerUnitViaName(playerName);

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

    //Saving
    [System.Serializable]
    public class ProgressionState
    {
        //XP
        public Dictionary<string, int> experienceDict = new Dictionary<string, int>();
        public Dictionary<string, int> talentProgression = new Dictionary<string, int>();

        public Dictionary<string, List<PlayerCurrentAttributeMastery>> currentMasteries = new Dictionary<string, List<PlayerCurrentAttributeMastery>>();
    }


    public object CaptureState()
    {
        progressionState.experienceDict = currentExperienceDict;
        progressionState.talentProgression = talentProgression;

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
        if (state == null) //Null On New Game or when testing in editor
        {
            NewGameSetup();
            return; 
        }

        byte[] bytes = state as byte[];
        progressionState = SerializationUtility.DeserializeValue<ProgressionState>(bytes, DataFormat.Binary);

        currentExperienceDict = progressionState.experienceDict;
        talentProgression = progressionState.talentProgression;
        
        foreach (PlayerGridUnit player in allPlayers)
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
}
