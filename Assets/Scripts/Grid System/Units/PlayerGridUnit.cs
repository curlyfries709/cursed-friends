using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Cinemachine;

public class PlayerGridUnit : CharacterGridUnit
{
    [Header("Player Data")]
    public PartyMemberData partyMemberData;
    [Header("Player Specific UI")]
    public Sprite transparentBackgroundPotrait;
    [Header("Player Unit Components")]
    public Transform raycastPoint;
    public CharacterController unitController;
    [Space(10)]
    [SerializeField] EquipmentTransforms equipmentTransforms;
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
    [SerializeField] PlayerInteractSkill interactSkill;
    [Space(10)]
    [SerializeField] ChainSelectionEvent chainSelectionEvent;

    //Cache
    public PlayerSkillset playerSkillset { get; private set; }
    [HideInInspector] public PlayerBaseSkill lastUsedSkill = null;

    protected override void Awake()
    {
        base.Awake();

        portrait = partyMemberData.portrait;
        unitName = partyMemberData.memberName;
    }

    protected override void OnBeginTurn()
    {   
        base.OnBeginTurn();
        ShowActionMenu(true, false);
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

    public void SetSkillData(PlayerSkillset skillData)
    {
        playerSkillset = skillData;
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

    public PlayerBaseSkill GetInteractSkill()
    {
        return interactSkill;
    }

    public ActionMenu GetActionMenu()
    {
        return actionMenu;
    }


    //SETTERS
    public void SetPlayerUnitStats(PlayerUnitStats statsComp)
    {
        stats = statsComp;
        statsComp.Equipment().SetEquipmentTransforms(equipmentTransforms);
        unitAnimator.SetEquipment(stats.Equipment());
    }
}
