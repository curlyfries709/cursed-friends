using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Sirenix.OdinInspector;
using AnotherRealm;
using System;

public abstract class PlayerBaseSkill : BaseSkill
{
    [PropertyOrder(-8)]
    [Range(1, 99)]
    public int unlockLevel = 1;
    [PropertyOrder(-7)]
    [Title("Skill Description")]
    public string skillName;
    [PropertyOrder(-6)]
    public GameObject aoeDiagram;
    [Space(10)]
    [PropertyOrder(-5)]
    [TextArea(2, 10)]
    public string quickData;
    [PropertyOrder(-4)]
    [TextArea(4, 10)]
    public string description;
    [Title("Cost")]
    [PropertyOrder(-3)]
    [Tooltip("For Action Menu Skills, set type to 'Free' ")]
    public SkillCostType costType = SkillCostType.SP;
    [PropertyOrder(-2)]
    [ShowIf("costType", SkillCostType.SP)]
    [Tooltip("How much the skill costs to use in SP")]
    [SerializeField] protected int cost = 10;
    [PropertyOrder(-1)]
    [HideIf("@costType == SkillCostType.SP || costType == SkillCostType.Free")]
    [Tooltip("How much the skill costs to use in HP or FP")]
    [Range(1, 100)]
    [SerializeField] protected int percentageCost = 5;
    [Title("Player Base Skill")]
    [PropertyOrder(0)]
    [Space(10)]
    [Tooltip("Must the player select the area or unit to target?")]
    [SerializeField] bool isGridSelectionRequired;
    [Title("VISUALS")]
    [SerializeField] protected GameObject blendListCamera;
    [Space(10)]
    [ListDrawerSettings(Expanded = true)]
    [SerializeField] protected List<GameObject> otherVisualsToToggle;
    //[Tooltip("When the unit rotates, should the Shape remain the same as it would in the forward direction?")]
    //[SerializeField] bool shapeIndependentOfUnitDirection;

    //Cache
    protected FantasyCombatCollectionManager collectionManager; //Used By Orbs & Potions

    //Storage
    protected PlayerGridUnit player;
    protected List<GridPosition> validTargetGridPositions = new List<GridPosition>();

    //Auto Selection
    private GridUnit autoSelectedUnit = null; 
    private GridPosition autoSelectedGridposition = new GridPosition();
    private GridPosition selectedAreaIndexPos = new GridPosition();

    bool validUnitsInTargetArea = false;

    //Other Variables
    protected bool skillTriggered = false;
    bool deactivateCamOnActionComplete = false;

    float deactivateCamDelay = 0;
    //Events
    public static Action<PlayerGridUnit, BaseSkill> PlayerUsedSkill; //Base Skill argument so counterattack & bump attack can raise it. 

    public enum SkillCostType
    {
        HP,
        SP,
        FP,
        Free
    }

    protected override void Awake()
    {
        base.Awake();

        if (myUnit)
        {
            myUnitMoveTransform = myUnit.transform;
            moveTransformGridCollider = myUnit.gridCollider;
            player = myUnit as PlayerGridUnit;
            myCharacter = player;
        }    
    }

    public override void Setup(SkillPrefabSetter skillPrefabSetter, SkillData skillData)
    {
        base.Setup(skillPrefabSetter, skillData);
        player = myUnit as PlayerGridUnit;

        myUnitMoveTransform = skillPrefabSetter.characterGridUnitMoveTransform;
    }

    private void Start()
    {
        ValidateData();
    }

    public virtual bool TrySelectSkill()
    {
        //Check if have enough HP, SP, FP.
        if (CanAffordSkill())
        {
            HUDManager.Instance.UpdateSelectedSkill(skillName);

            if (!ShowInteractCanvasWhileSkillSelected())
                InteractionManager.Instance.ShowInteractCanvas?.Invoke(false);

            PlayTutorial();

            return true;
        }

        return false;
    }

    protected override void ResetData()
    {
        base.ResetData();

        skillTriggered = false;
        validTargetGridPositions.Clear();
        autoSelectedUnit = null;
    }

    public virtual void SkillSelected()
    {
        if (!skillTriggered)
        {
            GridVisual();
        }
    }

    public abstract bool TryTriggerSkill(); //Call TurnCompleteWhenSkillSuccesfullyTriggered.
    public virtual void SkillCancelled(bool showActionMenu = true)
    {
        HideSelectedSkillGridVisual();

        AudioManager.Instance.PlaySFX(SFXType.TabBack);

        if (!ShowInteractCanvasWhileSkillSelected())
            InteractionManager.Instance.ShowInteractCanvas?.Invoke(true);

        if(showActionMenu)
            FantasyCombatManager.Instance.ShowActionMenu(true);
    }

    protected void GridVisual()
    {
        if (IsUnitStandingInMoreCellsThanNeccesary())
        {
            HideSelectedSkillGridVisual();
            return;
        }

        CalculateSelectedGridPos();
        ShowSelectedSkillGridVisual();
    }

    //Selection Logic
    protected void ShowSelectedSkillGridVisual()
    {
        if (isGridSelectionRequired)
        {
            GridSystemVisual.Instance.ShowGridVisuals(validTargetGridPositions, GridSystemVisual.VisualType.SkillTargetArea);
        }

        GridSystemVisual.Instance.ShowGridVisuals(this, selectedGridPositions, highlightableData, GridSystemVisual.VisualType.SkillAOE);

        if(this is ITeamSkill teamSkill)
        {
            GridSystemVisual.Instance.ShowGridVisuals(this, teamSkill.GetAllyGridPositionsFromSkillOwnerCurrentPosition(), 
                highlightableData, GridSystemVisual.VisualType.TeamSkillAlly);
        }

        HUDManager.Instance.UpdateTurnOrderNames(selectedUnits);
    }

    protected void HideSelectedSkillGridVisual()
    {
        GridSystemVisual.Instance.HideGridVisualsOfType(GridSystemVisual.VisualType.ObjectAOE);

        if (this is ITeamSkill)
        {
            GridSystemVisual.Instance.HideGridVisualsOfType(GridSystemVisual.VisualType.TeamSkillAlly);
        }

        GridSystemVisual.Instance.HideGridVisualsOfType(GridSystemVisual.VisualType.SkillAOE);

        if (isGridSelectionRequired)
        {
            GridSystemVisual.Instance.HideGridVisualsOfType(GridSystemVisual.VisualType.SkillTargetArea);
        }

        HUDManager.Instance.UpdateTurnOrderNames(new List<GridUnit>());
    }

    //DOERS
    
    protected void BeginSkill(float returnToGridPosTime, float delayBeforeReturn, bool deactivateCamOnActionComplete, Orb orbData = null)
    {
        BeginAction();

        player.lastUsedSkill = this;

        myCharacter.unitAnimator.ResetMovementSpeed(); //Speed Set to 0

        //Warp Unit into Position & Rotation in an attempt to remove camera jitter.
        Vector3 desiredRotation = Quaternion.LookRotation(GetCardinalDirectionAsVector()).eulerAngles;
        myUnit.Warp(LevelGrid.Instance.gridSystem.GetWorldPosition(myUnit.GetCurrentGridPositions()[0]), Quaternion.Euler(new Vector3(0, desiredRotation.y, 0)));

        //Set Times
        myCharacter.returnToGridPosTime = returnToGridPosTime;
        myCharacter.delayBeforeReturn = delayBeforeReturn;
        deactivateCamDelay = delayBeforeReturn;

        skillTriggered = true;
        ControlsManager.Instance.DisableControls();
        HUDManager.Instance.UpdateSelectedSkill("");

        GridSystemVisual.Instance.HideAllGridVisuals();
        HUDManager.Instance.UpdateTurnOrderNames(new List<GridUnit>());

        SetUnitsToShow();

        SetActionTargets(selectedUnits);

        //UpdatePosition
        myUnit.MovedToNewGridPos();

        //Spend SP, HP or FP.
        SpendSkillCost(myCharacter);

        //Use Orb & Begin Charge, if an Orb.
        if (orbData)
        {
            IOrb orb = this as IOrb;
            orb?.UseOrb(orbData, player, collectionManager);
        }

        //Call Event
        PlayerUsedSkill?.Invoke(player, this);

        this.deactivateCamOnActionComplete = deactivateCamOnActionComplete;
    }

    protected virtual void SpendSkillCost(CharacterGridUnit character)
    {
        switch (costType)
        {
            case SkillCostType.SP:
                character.CharacterHealth().SpendSP(GetCost());
                break;
            case SkillCostType.FP:
                character.CharacterHealth().SpendFP(GetCost());
                break;
            case SkillCostType.HP:
                character.CharacterHealth().SpendHP(GetCost());
                break;
        }
    }

    public override void EndAction()
    {
        SkillComplete();

        if (deactivateCamOnActionComplete)
        {
            Invoke("DeactivateCam", deactivateCamDelay);
        }

        base.EndAction();
    }

    protected void DeactivateCam()
    {
        ActivateVisuals(false);
        EnableOtherVisuals(false);
    }

    protected void ActivateVisuals(bool activate)
    {
        if (blendListCamera)
        {
            FantasyCombatManager.Instance.ActivateCurrentActiveCam(!activate);
            blendListCamera.SetActive(activate);
        }

        EnableOtherVisuals(activate);
    }

    private void EnableOtherVisuals(bool show)
    {
        foreach(GameObject visual in otherVisualsToToggle)
        {
            visual.SetActive(show);
        }
    }

    //GRID SELECTION LOGIC
    protected override void CalculateSelectedGridPos()
    {
        if (isGridSelectionRequired)
        {
            validTargetGridPositions = GetFilteredGridPosList();
            //Set Selected Grid Pos
            selectedGridPositions = GetSelectedGridPosFromTargetArea();
        }
        else
        {
            //If Grid Selection is not required. We can assume the valid Target Area and Skill dimensions to be the same.
            selectedGridPositions = GetFilteredGridPosList();
        }

        SetSelectedUnits();
    }

    protected List<GridPosition> GetSelectedGridPosFromTargetArea()
    {
        if (validTargetGridPositions.Count == 0)
        {
            return new List<GridPosition>();
        }

        GridPosition startPos;

        //Debug.Log("Getting selected Grid Pos"); Called In update

        if ((autoSelectedUnit || targets.Contains(FantasyCombatTarget.Grid)) && validTargetGridPositions.Contains(autoSelectedGridposition))
        {
            startPos = autoSelectedGridposition;
        }
        else
        {
            startPos = GetAutoSelectGridPosition(validTargetGridPositions[0]);
        }

        if (!validUnitsInTargetArea)
        {
            return new List<GridPosition>();
        }

        int XOrigin = startPos.x;
        int ZOrigin = startPos.z;

        List<GridPosition> list;

        if (GetCardinalDirection() == Direction.North || GetCardinalDirection() == Direction.South)
        {
            int XEnd = startPos.x + ((int)skillDimensions.x - 1);

            int ZEnd = startPos.z + ((int)skillDimensions.y - 1);
            list = GetValidUnfilteredGridPositions(XOrigin, XEnd, ZOrigin, ZEnd);
        }
        else
        {
            int XEnd = startPos.x + ((int)skillDimensions.y - 1);

            int ZEnd = startPos.z + ((int)skillDimensions.x - 1);

            list = GetValidUnfilteredGridPositions(XOrigin, XEnd, ZOrigin, ZEnd);
        }


        if (list.Count > 0)
        {
            selectedAreaIndexPos = list[0];
        }

        return list;
    }

    public void UpdateGridSelection(Vector2 input, Transform mainCamTransform)
    {
        if (!isGridSelectionRequired || !validUnitsInTargetArea ||
            (!autoSelectedUnit && !targets.Contains(FantasyCombatTarget.Grid)) || validTargetGridPositions.Count == 0) { return; } //No Auto Selected unit means no units Target Area.

        GridPosition autoSelectedPos = autoSelectedGridposition;
        bool isGridCycle = targets.Contains(FantasyCombatTarget.Grid);

        if (!targets.Contains(FantasyCombatTarget.Grid))
        {

            autoSelectedPos = autoSelectedUnit.GetGridPositionsOnTurnStart()[0];
        }

        Vector2 camForward = new Vector2(mainCamTransform.forward.normalized.x, mainCamTransform.forward.normalized.z);
        Vector2 camRight = new Vector2(mainCamTransform.right.normalized.x, mainCamTransform.right.normalized.z);

        Vector2 camRelativeInput = camForward * input.y + camRight * input.x;
        Vector2 inputChange = new Vector2(Mathf.RoundToInt(camRelativeInput.x), Mathf.RoundToInt(camRelativeInput.y));

        List<GridPosition> filteredGridPos = new List<GridPosition>();

        int XOrigin = selectedAreaIndexPos.x;
        int ZOrigin = selectedAreaIndexPos.z;

        int ZEnd = ZOrigin + ((int)skillDimensions.y - 1);
        int XEnd = XOrigin + ((int)skillDimensions.x - 1);

        int maxTargetAreaX = 0;
        int maxTargetAreaY = 0;

        int minTargetAreaX = 1000;
        int minTargetAreaY = 1000;


        foreach (GridPosition pos in validTargetGridPositions)
        {
            if (pos.x > maxTargetAreaX)
            {
                maxTargetAreaX = pos.x;
            }

            if (pos.z > maxTargetAreaY)
            {
                maxTargetAreaY = pos.z;
            }

            if (pos.x < minTargetAreaX)
            {
                minTargetAreaX = pos.x;
            }

            if (pos.z < minTargetAreaY)
            {
                minTargetAreaY = pos.z;
            }
        }

        if (inputChange.x != 0)
        {
            if (inputChange.x > 0)
            {
                //Moving Right
                filteredGridPos = validTargetGridPositions.Where((pos) => isGridCycle ? (pos.x > autoSelectedPos.x && pos.z == autoSelectedPos.z) : (pos.x > autoSelectedPos.x)).ToList();
            }
            else
            {
                //Moving Left
                filteredGridPos = validTargetGridPositions.Where((pos) => isGridCycle ? (pos.x < autoSelectedPos.x && pos.z == autoSelectedPos.z) : (pos.x < autoSelectedPos.x)).Reverse().ToList();
            }
        }
        else
        {
            if (inputChange.y > 0)
            {
                //Moving Up
                filteredGridPos = validTargetGridPositions.Where((pos) => isGridCycle ? (pos.z > autoSelectedPos.z && pos.x == autoSelectedPos.x) : (pos.z > autoSelectedPos.z)).ToList();
            }
            else
            {
                //Moving Down
                filteredGridPos = validTargetGridPositions.Where((pos) => isGridCycle ? (pos.z < autoSelectedPos.z && pos.x == autoSelectedPos.x) : (pos.z < autoSelectedPos.z)).Reverse().ToList();
            }
        }

        foreach (GridPosition gridPosition in filteredGridPos)
        {
            bool conditionToUse = GetTargetingCondition(gridPosition);

            //Debug.Log("Position: " + gridPosition.ToString());

            if (conditionToUse || isGridCycle)
            {
                GridUnit unitAtPos = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);
                if (!CombatFunctions.IsUnitValidTarget(targets, myCharacter, unitAtPos)) { continue; }

                autoSelectedUnit = unitAtPos;

                int x = gridPosition.x;
                int z = gridPosition.z;

                if (originateFromUnitCentre)
                {
                    //THIS REQUIRES TESTING!
                    x = Mathf.Clamp(x, minTargetAreaX, maxTargetAreaX - (int)skillDimensions.x + 1);
                    z = Mathf.Clamp(z, minTargetAreaY, maxTargetAreaY - (int)skillDimensions.y + 1);
                }
                else
                {
                    if (GetCardinalDirection() == Direction.North || GetCardinalDirection() == Direction.South)
                    {
                        x = Mathf.Clamp(x, minTargetAreaX, maxTargetAreaX - (int)skillDimensions.x + 1);
                        z = Mathf.Clamp(z, minTargetAreaY, maxTargetAreaY - (int)skillDimensions.y + 1);
                    }
                    else
                    {
                        x = Mathf.Clamp(x, minTargetAreaX, maxTargetAreaX - (int)skillDimensions.y + 1);
                        z = Mathf.Clamp(z, minTargetAreaY, maxTargetAreaY - (int)skillDimensions.x + 1);
                    }
                }



                /* if (z <= minTargetAreaZ)
                 {
                     z = maxTargetAreaY + (int)skillDimensions.y - 1;

                 }
                 else if (z >= maxTargetAreaY)
                 {

                     z = maxTargetAreaY - (int)skillDimensions.y + 1;
                 }*/


                /*if (x >= maxTargetAreaX)
                {
                    x = maxTargetAreaX - (int)skillDimensions.x + 1;
                }
                else if (x <= minTargetAreaX)
                {
                    x = minTargetAreaX - (int)skillDimensions.x - 1;
                }*/

                autoSelectedGridposition = new GridPosition(x, z);
                return;
            }
        }
    }

    protected GridPosition GetAutoSelectGridPosition(GridPosition defaultPosition)
    {
        float closestDis = Mathf.Infinity;
        GridPosition newPos = defaultPosition;

        validUnitsInTargetArea = false;

        foreach (GridPosition gridPosition in validTargetGridPositions)
        {
            bool conditionToUse = GetTargetingCondition(gridPosition);
            GridUnit unitAtPos = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);

            if (conditionToUse && unitAtPos)
            {
                bool canTargetThisUnit = CombatFunctions.IsUnitValidTarget(targets, myCharacter, unitAtPos);

                if (canTargetThisUnit)
                {
                    autoSelectedUnit = unitAtPos;
                    autoSelectedGridposition = gridPosition;
                    validUnitsInTargetArea = true;
                    return gridPosition;
                }
            }
            else if (targets.Contains(FantasyCombatTarget.Grid))
            {
                Vector3 gridWorldPos = LevelGrid.Instance.gridSystem.GetWorldPosition(gridPosition);
                float newDistance = Vector3.Distance(gridWorldPos, myUnitMoveTransform.position);
                validUnitsInTargetArea = true;

                if (newDistance < closestDis)
                {
                    closestDis = newDistance;
                    newPos = gridPosition;
                }
            }
        }

        autoSelectedUnit = null;
        autoSelectedGridposition = newPos;

        return newPos;
    }
    //GETTERS
    protected virtual bool CanTriggerSkill(bool requiresUnitSelection)
    {
        //Something like Guarding which is just an action without needing to select a Unit doesn't Require Unit Selection.
        bool canTriggerSkill = requiresUnitSelection ? CanTriggerTargetSelectionSkill() : CanTriggerSelfSkill();

        if(myCharacter.CanTriggerSkill == null)
        {
            return canTriggerSkill;
        }

        return myCharacter.CanTriggerSkill() && canTriggerSkill;
    }

    public virtual bool CanAffordSkill()
    {
        return CanPlayerAffordSkill(myCharacter);
    }

    protected bool CanPlayerAffordSkill(CharacterGridUnit character)
    {
        switch (costType)
        {
            case SkillCostType.SP:
                return GetCost() <= character.CharacterHealth().currentSP;
            case SkillCostType.FP:
                return GetCost() <= character.CharacterHealth().currentFP;
            case SkillCostType.HP:
                return GetCost() < character.CharacterHealth().currentHealth;
            default:
                return true;
        }
    }

    private bool CanTriggerTargetSelectionSkill()
    {
        //Check if current is not gridPosition Occupied and that a unit is actually selected.
        return IsValidActionGridPosition(myUnit.GetCurrentGridPositions()) && IsValidGridSelected();
    }

    private bool CanTriggerSelfSkill()
    {
        return IsValidActionGridPosition(myUnit.GetCurrentGridPositions());
    }

    private bool IsValidGridSelected()
    {
        if (targets.Contains(FantasyCombatTarget.Grid))
        {
            //Ensure no Obstacle at this position...I Don't think obstacles can even be selected so don't worry.
            return true;
        }

        return selectedUnits.Count > 0;
    }

    protected bool IsValidActionGridPosition(List<GridPosition> unitGridPositions)
    {
        if (IsUnitStandingInMoreCellsThanNeccesary())
        {
            //If they're standing on edges or invalid Grid Pos. 
            return false;
        }

        //Player cannot activate action on GridPosition that is occupied by another unit.
        foreach (GridPosition gridPosition in unitGridPositions)
        {
            if (IsCurrentGridPositionOccupiedByAnotherUnit(gridPosition, true))
            {
                return false;
            }
        }

        return true;
    }

    protected virtual bool ShowInteractCanvasWhileSkillSelected()
    {
        return false;
    }

    protected bool IsUnitStandingInMoreCellsThanNeccesary()
    {
        return CombatFunctions.IsUnitStandingInMoreCellsThanNeccesary(myCharacter);
    }

    public PlayerGridUnit GetSkillOwner()
    {
        return player;
    }

    protected void ValidateData()
    {
        if(myUnit == null) { return; }

        if (validTargetArea != Vector2.zero)
        {
            if (validTargetArea.x % 2 != CombatFunctions.GetHorizontalCellsOccupied(GetSkillOwnerMoveGridCollider()) % 2)
            {
                //Parity: (of a number) the fact of being even or odd.
                Debug.LogError(skillName + " IS NOT OF THE SAME PARITY AS UNIT WIDTH!");
            }
        }

        if(originateFromUnitCentre && validTargetArea != Vector2.zero)
        {
            if (validTargetArea.x != validTargetArea.y)
            {
                Debug.LogError(skillName + " ORIGINATES FROM CENTER BUT THE VALID TARGET AREA IS NOT A SQUARE");
            }
        }
    }


    public bool RequiresGridSelection()
    {
        return isGridSelectionRequired;
    }

    private void PlayTutorial()
    {
        //Trigger Activating action Tutorial
        if (StoryManager.Instance.PlayTutorial(3))
        {
            if (RequiresGridSelection())
            {
                //If Require Grid Selection, play tutorial after activation tut complete.
                StoryManager.Instance.AddTutorialToQueue(8);
            }
        }
        else if(RequiresGridSelection()) //If Activation Tutorial already played & skill requires grid selection
        {
            //Trigger Grid Selection Tutorial
            StoryManager.Instance.PlayTutorial(8);
        }
    }

    public virtual int GetSkillIndex(){ return 0; }

    public int GetCost()
    {
        if(costType == SkillCostType.SP)
        {
            return cost;
        }
        else if (costType == SkillCostType.HP)
        {
            return Mathf.RoundToInt(((float)percentageCost / 100) * myCharacter.stats.GetVitalityWithoutBonus());
        }
        else
        {
            //Must Be FP
            return Mathf.RoundToInt(((float)percentageCost / 100) * myCharacter.CharacterHealth().MaxFP()); 
        }
    }

    //Setters
    public void ExternalSetup(PlayerGridUnit newSkillOwner, string newSkillName, FantasyCombatCollectionManager collectionManager)
    {
        myUnit = newSkillOwner;
        myCharacter = newSkillOwner;
        player = newSkillOwner;

        myUnitMoveTransform = newSkillOwner.transform;
        moveTransformGridCollider = myUnit.gridCollider;

        this.collectionManager = collectionManager;

        //Update Skill Name
        if(newSkillName != "")
            skillName = newSkillName;
    }
}
