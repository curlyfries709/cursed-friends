using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CombatSkillManager : StateMachine
{
    public static CombatSkillManager Instance { get; private set; }

    [SerializeField] Transform skillPoolHeader;

    //Storage
    Dictionary<CharacterGridUnit, List<SkillData>> characterSkillSetDict = new Dictionary<CharacterGridUnit,List<SkillData>>();
    Dictionary<SkillData, BaseSkill> skillPool = new Dictionary<SkillData, BaseSkill>();

    //States
    public MythicalSkillManagerState mythicalSkillManagerState { get; private set; }
    public FantasyHumanSkillManagerState fantasyHumanSkillManagerState { get; private set; } //STATE NOT CREATED
    public SkillManagerBaseState modernHumanSkillManagerState { get; private set; } //STATE NOT CREATED


    private void Awake()
    {
        if (!Instance)
        {
            Instance = this;
        }

        IntializeStates();
    }

    private void OnEnable()
    {
        SavingLoadingManager.Instance.NewRealmEntered += OnNewRealmEntered;
        SavingLoadingManager.Instance.EnteringNewTerritory += OnNewTerritoryEntered;

        FantasyCombatManager.Instance.OnNewTurn += OnNewTurn;
        FantasyCombatManager.Instance.CombatEnded += OnCombatEnded;
    }

    private void OnNewRealmEntered(RealmType newRealm)
    {
        SetState(newRealm);

        //Clear Data
        characterSkillSetDict.Clear();
        skillPool.Clear();
    }

    public void SpawnSkills(CharacterGridUnit combatCharacterUnit) //Called when Character unit is intialized
    {
        //Player skillset may have changed since last party so update that as well
        UnitStats characterStats = combatCharacterUnit.stats;
        EnemyUnitStats enemyStats = characterStats as EnemyUnitStats;

        if (enemyStats)
        {
            characterSkillSetDict[combatCharacterUnit] = enemyStats.GetEnemySkillSet();
        }
        else
        {
            PlayerGridUnit player = combatCharacterUnit as PlayerGridUnit;
            characterSkillSetDict[combatCharacterUnit] = GetPlayerSkillSetData(player.partyMemberData);
        }

        List<SkillData> skillSet = characterSkillSetDict[combatCharacterUnit];

        foreach (SkillData skillData in skillSet)
        {
            if (skillPool.ContainsKey(skillData)) { continue; } //Continue loop if skill already spawned
            GameObject spawnedSkill = Instantiate(skillData.skillPrefab, skillPoolHeader);
            skillPool[skillData] = spawnedSkill.GetComponent<BaseSkill>();
        }
    }

    public void OnNewTurn(CharacterGridUnit character, int turnNumber)//Called On New Turn before turn start events triggered
    {
        //Setup all the character's skills
        SkillPrefabSetter skillSetter = character.GetComponent<SkillPrefabSetter>();

        foreach (SkillData skillData in characterSkillSetDict[character])
        {
            skillPool[skillData].Setup(skillSetter, skillData);
        }
    }


    private void OnCombatEnded(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        if(battleResult == BattleResult.Restart) { return; }

        //Clean up Skillset Data
        for (int i = characterSkillSetDict.Count - 1; i >= 0; --i)
        {
            KeyValuePair<CharacterGridUnit, List<SkillData>> pair = characterSkillSetDict.ElementAt(i);

            if (!(pair.Key is PlayerGridUnit))
            {
                characterSkillSetDict.Remove(pair.Key);
            }
        }
    }

    private void OnNewTerritoryEntered()
    {
        //Destroy & Remove AI Skills on entering new territory
        for (int i = skillPool.Count - 1; i >= 0; --i)
        {
            KeyValuePair<SkillData, BaseSkill> pair = skillPool.ElementAt(i);

            if (pair.Key is PlayerSkillData)
            {
                Destroy(pair.Value.gameObject);
                skillPool.Remove(pair.Key);
            }
        }
    }

    private void SetState(RealmType newRealm)
    {
        if(newRealm == RealmType.Modern)
        {
            SwitchState(modernHumanSkillManagerState);
        }
        else if(newRealm == RealmType.Fantasy)
        {
            SwitchState(StoryManager.Instance.isCursed ? mythicalSkillManagerState : fantasyHumanSkillManagerState);
        }
    }

    private void OnDisable()
    {
        SavingLoadingManager.Instance.NewRealmEntered -= OnNewRealmEntered;
        SavingLoadingManager.Instance.EnteringNewTerritory -= OnNewTerritoryEntered;

        FantasyCombatManager.Instance.OnNewTurn -= OnNewTurn;
        FantasyCombatManager.Instance.CombatEnded -= OnCombatEnded;
    }

    //GETTERS
    public List<SkillData> GetPlayerSkillSetData(PartyMemberData partyMemberData)
    {
        return PartyManager.Instance.GetPartyMemberLearnedSkill(partyMemberData).GetActiveSkillSet();
    }

    public List<AIBaseSkill> GetAISpawnedSkills(CharacterGridUnit character)
    {
        List<AIBaseSkill> baseSkills = new List<AIBaseSkill>();
        List<SkillData> skillSet = characterSkillSetDict[character];

        foreach (SkillData skill in skillSet)
        {
            BaseSkill baseSkill = skillPool[skill];
            baseSkills.Add(baseSkill as AIBaseSkill);
        }

        return baseSkills;
    }

    public List<PlayerBaseSkill> GetPlayerSpawnedSkills(PlayerGridUnit player)
    {
        List<PlayerBaseSkill> baseSkills = new List<PlayerBaseSkill>();
        List<SkillData> skillSet = characterSkillSetDict[player];

        foreach (SkillData skill in skillSet)
        {
            BaseSkill baseSkill = skillPool[skill];
            baseSkills.Add(baseSkill as PlayerBaseSkill);
        }

        return baseSkills;
    }

    public SkillManagerBaseState GetActiveSkillManager()
    {
        return currentState as SkillManagerBaseState;
    }

    private void IntializeStates()
    {
        mythicalSkillManagerState = new MythicalSkillManagerState(this);

        fantasyHumanSkillManagerState = new FantasyHumanSkillManagerState(this);
    }

    public override void WarpToPosition(Vector3 newPosition, Quaternion newRotation)
    {
        //Do nothing
    }
}
