using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TalentProgressionManager : MonoBehaviour, ISaveable
{
    public static TalentProgressionManager Instance { get; private set; }

    [Title("Talents")]
    [SerializeField] int maxLevel = 12;
    [Space(10)]
    [SerializeField] int levelTwoTalentBenchmark = 3;
    [SerializeField] int levellingIncrement = 3;
    [Space(10)]
    [SerializeField] int rollChanceIncreasePerLevel = 5;
    [Space(10)]
    [SerializeField] int talentPointsToGainOnSuccessfulUse = 1;
    [Space(10)]
    [SerializeField] List<Talent> allTalents;
    [Header("TEMPORARY")]
    [SerializeField] bool unlockAllTalentsOnNewGame = false;
    [Title("BENCHMARKS")]
    [ListDrawerSettings(Expanded = true, ShowIndexLabels = true)]
    [SerializeField] List<int> talentLevelBenchmarks;
    [Space(10)] //To add space between button

    Dictionary<string, int> talentProgression = new Dictionary<string, int>();
    Dictionary<string, TalentStat> unlockedTalentCurrentStats = new Dictionary<string, TalentStat>();

    //Event 
    public Action<Talent> talentSuccessfullyUsed;

    //Saving Data
    private bool isDataRestored = false;

    public class TalentStat
    {
        public int rollboostPercent = 0;
        public int levelBoost = 0;
        
        public void ApplyRollBoost(int boost)
        {
            rollboostPercent += boost;
        }

        public void ApplyLevelBoost(int boost) 
        {
            levelBoost += boost;
        }

        public void RemoveRollBoost(int boost)
        {
            rollboostPercent -= boost;
        }

        public void RemoveLevelBoost(int boost)
        {
            levelBoost -= boost;
        }
    }

    private void Awake()
    {
        if (!Instance)
        {
            Instance = this;
        }
    }

    private void OnEnable()
    {
        talentSuccessfullyUsed += OnTalentSuccesfullyUsed;
    }

    private void OnTalentSuccesfullyUsed(Talent talent)
    {
        //Called when passing a roll or level check during exploration or dialouge
        if(GainTalentPoints(talent, talentPointsToGainOnSuccessfulUse))
        {
            /* 
               Here tell UI to display notification.
                If cinematic playing, display cinematic UI, so it doesn't interrupt, where UI is small banner of side of screen saying Talent levelled up.
                If Cinematic or dialogue not playing, display normal notification. 
             */
            throw new NotImplementedException();

        }
    }

    //Returns if Levelled up
    public bool GainTalentPoints(Talent talent, int points)
    {
        int currentLevel = GetBaseTalentLevel(talent);

        //Add Points
        talentProgression[talent.talentName] = talentProgression[talent.talentName] + points;

        int newLevel = GetBaseTalentLevel(talent);

        if(newLevel > currentLevel)
        {
            //LEVEL UP
            return true;
        }

        return false;
    }

    public void UnlockTalent(Talent talent)
    {
        //Set the talent to be level 1 which is unlocked
        talentProgression[talent.talentName] = 0;
        unlockedTalentCurrentStats[talent.talentName] = new TalentStat();   
    }

    public bool SucceedRollCheck(Talent talent, int SuccessChanceAtLevelOne)
    {
        int successChance = GetChanceToSucceedRollCheck(talent, SuccessChanceAtLevelOne);
        int randNum = UnityEngine.Random.Range(0, 101);

        bool didSucceed = successChance >= randNum;

        if (didSucceed)
        {
            talentSuccessfullyUsed?.Invoke(talent);
        }

        return didSucceed;
    }

    public bool SucceedLevelCheck(Talent talent, int levelRequiredToPass)
    {
        bool didSucceed = GetTalentLevelWithBoost(talent) >= levelRequiredToPass;

        if (didSucceed)
        {
            talentSuccessfullyUsed?.Invoke(talent);
        }

        return didSucceed;
    }

    private void OnDisable()
    {
        talentSuccessfullyUsed -= OnTalentSuccesfullyUsed;
    }

    public int GetChanceToSucceedRollCheck(Talent talent, int SuccessChanceAtLevelOne)
    {
        TalentStat talentStat = unlockedTalentCurrentStats[talent.talentName];
        int currentTalentLevel = GetTalentLevelWithBoost(talent);

        int chanceToSucceedAtCurrentLevel = SuccessChanceAtLevelOne + ((currentTalentLevel - 1) * rollChanceIncreasePerLevel);

        //Add Roll Bonuses
        int successChance = chanceToSucceedAtCurrentLevel;

        if (talentStat != null)
        {
            successChance = successChance + talentStat.rollboostPercent;
        }

        return Mathf.Min(successChance, 100);
    }
    
    public int GetTalentLevelWithBoost(Talent talent)
    {
        TalentStat talentStat = unlockedTalentCurrentStats[talent.talentName];
        int baseTalentLevel = GetBaseTalentLevel(talent);
        int currentTalentLevel = baseTalentLevel;

        if(talentStat != null)
        {
            currentTalentLevel = currentTalentLevel + talentStat.levelBoost;
        }

        return Mathf.Min(currentTalentLevel, maxLevel);
    }

    //GETTERS
    public Talent GetTalentByName(string talentName)
    {
        foreach(Talent talent in allTalents)
        {
            if (talentName == talent.talentName)
            {
                return talent;
            }
        }

        return null;
    }

    public int GetBaseTalentLevel(Talent talent)
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

    [Button("Generate TALENT Benchmarks")]
    private void SetLevelBenchMarks() //Called Via Editor Button
    {
        Debug.Log("SETTING TALENT BENCHMARKS ");

        talentLevelBenchmarks[0] = -5;
        talentLevelBenchmarks[1] = 0;

        for (int level = 2; level <= maxLevel; level++)
        {
            if (level == 2)
            {
                talentLevelBenchmarks[level] = levelTwoTalentBenchmark;
                continue;
            }

            //Increment = 100[Difference between Lvl 1 & 2] + (100[Increment Constant] x (Current Lvl - 2))
            //Benchmark: Previous Level Benchmark + Increment

            int previousBenchmark = talentLevelBenchmarks[level - 1];
            int increment = levelTwoTalentBenchmark + (levellingIncrement * (level - 2));
            int benchmark = previousBenchmark + increment;

            if (level < talentLevelBenchmarks.Count)
            {
                talentLevelBenchmarks[level] = benchmark;
            }
            else
            {
                talentLevelBenchmarks.Insert(level, benchmark);
            }
        }
    }


    //SAVING
    private void NewGameSetup()
    {
        foreach (Talent talent in allTalents)
        {
            //Set all Talents to be level 0 which is locked
            talentProgression[talent.talentName] = -5;

            if(unlockAllTalentsOnNewGame)
            {
                UnlockTalent(talent);
            }
        }
    }

    public object CaptureState()
    {
        Debug.Log("STORE TALENT XP & CurrentTalentStats");
        throw new System.NotImplementedException();
    }

    public void RestoreState(object state)
    {
        isDataRestored = true;

        if (state == null) //Null On New Game or when testing in editor
        {
            NewGameSetup();
            return;
        }

        Debug.Log("RESTORE TALENT XP & CURRENTTALENTSTATS");
        //UPDATE THE TALENT PROGRESSION
        //UPDATE THE TALENT STAT
        /* 
         ISSUE: Since Talent stat data is stored, when game is loaded how will classes that applied the boost know when to remove the boost. 
        These classes would also have to store that they applied a boost. 
        Maybe Could make a manager to handle applying talent boosts which would store data of how much boost is applied and conditions for boost to be removed 
        E.G Exiting Fantasy realm, On day end, KO in Combat, after next talent roll check, etc.
         */
    }


    public bool IsDataRestored()
    {
        return isDataRestored;
    }

}
