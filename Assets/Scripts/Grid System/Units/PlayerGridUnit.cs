using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Cinemachine;

public class PlayerGridUnit : CharacterGridUnit
{
    [Header("Player Specific UI")]
    public Sprite transparentBackgroundPotrait;
    [Header("Player Unit Components")]
    public Transform raycastPoint;
    public CharacterController unitController;
    [Space(10)]
    public Transform skillHeader;
    [Header("Player Menu Cams")]
    public GameObject collectionThinkCam;
    [Space(5)]
    public GameObject weaponSwitchCam;
    public GameObject tacticsMenuCam;
    [Space(5)]
    public GameObject fleeCam;
    [SerializeField] CinemachineFreeLook combatFreeLookComponent;
    [Header("Player Unit Actions")]
    [SerializeField] ActionMenu actionMenu;
    [SerializeField] PlayerBaseSkill basicAttack;
    [SerializeField] Guard guard;
    [Space(10)]
    [SerializeField] ChainSelectionEvent chainSelectionEvent;

    //Cache
    [HideInInspector] public PlayerBaseSkill lastUsedSkill = null;

    //Skills
    List<PlayerBaseSkill> learnedSkills = new List<PlayerBaseSkill>();

    private int activeSkillHeaderIndex = 0;

    protected override void OnEnable()
    {
        base.OnEnable();
        SavingLoadingManager.Instance.DataAndSceneLoadComplete += SetLearnedSkills;
    }

    protected override void OnBeginTurn()
    {   
        base.OnBeginTurn();
        ShowActionMenu(true, false);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        SavingLoadingManager.Instance.DataAndSceneLoadComplete -= SetLearnedSkills;
    }

    public override void Warp(Vector3 destination, Quaternion rotation)
    {
        //gridCollider.enabled = false;
        unitController.enabled = false;
        transform.position = destination;
        transform.rotation = rotation;
        unitController.enabled = true;
        //gridCollider.enabled = true;
    }

    public void ActivateGridCollider(bool activate)
    {
        gridCollider.enabled = activate;

        if(FantasyCombatManager.Instance.InCombat())
            unitController.enabled = activate;
    }

    public void ShowActionMenu(bool show, bool enableTacticUsedMode)
    {
        if (!StatusEffectManager.Instance.IsUnitDisabled(this))
        {
            actionMenu.Enable(show);
            actionMenu.SwitchToTacticUsedMode(enableTacticUsedMode);
        } 
    }

    public void ResetOrbitCam()
    {
        //camFollowTarget.rotation = Quaternion.Euler(Vector3.zero);
        //freeLookPlayerCam.m_YAxis.Value = 0;
        //freeLookPlayerCam.m_XAxis.Value = 0;
    }

    public void SetFollowCamInheritPosition(bool inheritPosition)
    {
        combatFreeLookComponent.m_Transitions.m_InheritPosition = inheritPosition;
    }

    public ChainSelectionEvent Chain()
    {
        return chainSelectionEvent;
    }


    //Quick Combat Actions

    public PlayerBaseSkill GetBasicAttack()
    {
        return basicAttack;
    }

    public Guard Guard()
    {
        return guard;
    }

    //SKills
    public List<PlayerBaseSkill> GetActiveLearnedSkills()
    {
        if(activeSkillHeaderIndex <= 1) 
        {
            return learnedSkills;
        }
        else
        {
            //Means it must be a form change skill.
            Transform activeSkillHeader = skillHeader.GetChild(activeSkillHeaderIndex);
            return activeSkillHeader.GetComponentsInChildren<PlayerBaseSkill>().ToList();
        }
    }

    public void LearnNewSkill(PlayerBaseSkill newSkill)
    {
        if (learnedSkills.Contains(newSkill)) { return; }
        learnedSkills.Add(newSkill);
    }

    public void ForgetSkill(PlayerBaseSkill skill)
    {
        learnedSkills.Remove(skill);
    }

    public List<PlayerBaseSkill> GetSkillsWithinLevelRange(int currentLevel, int levelsGained, int skillHeaderIndex)
    {
        Transform activeSkillHeader = skillHeader.GetChild(skillHeaderIndex);
        int endLevel = currentLevel + levelsGained;

        return activeSkillHeader.GetComponentsInChildren<PlayerBaseSkill>().ToList().Where((skill) => skill.unlockLevel > currentLevel && skill.unlockLevel <= endLevel).ToList();
    }

    private void SetLearnedSkills()
    {
        Transform activeSkillHeader = skillHeader.GetChild(activeSkillHeaderIndex);
        learnedSkills = activeSkillHeader.GetComponentsInChildren<PlayerBaseSkill>().ToList().Where((skill) => stats.level >= skill.unlockLevel).ToList();
    }
}
