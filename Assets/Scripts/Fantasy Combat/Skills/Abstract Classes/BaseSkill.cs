using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Cinemachine;
using AnotherRealm;
using System.Linq;
using System;
using Sirenix.Utilities;

public abstract class BaseSkill : MonoBehaviour, ICombatAction
{
    //IMPORTANT VARIABLES
    [PropertyOrder(-9)]
    [Title("Unit")]
    [SerializeField] protected GridUnit myUnit;
    [Title("Target Behaviour")]
    [Tooltip("Can this skill only target one unit? Leave false to target multiple")]
    [SerializeField] protected bool isSingleTarget; //Target Area is automatically 1X1 for this.
    [Space(10)]
    [ListDrawerSettings(Expanded = true)]
    [LabelText("Who can the Skill target?")]
    [Tooltip("Who does the skill target? Allies, Enemies, interactables or multiple.")]
    [SerializeField] protected List<FantasyCombatTarget> targets;
    [Title("Skill Dimensions Data")]
    [Tooltip("The Shape of the skill. Is it a simple Line. A cross? A Rectangle? Diagonal? A Compass Star (A Cross combined with all diagonals)?")]
    [SerializeField] protected SkillShape skillShape;
    [Tooltip("The dimensions of the skill in Grid Units. 2x1 means 2 cells on the x and 1 cell on the z. Leave as 1X1 for single target melees")]
    [SerializeField] protected Vector2 skillDimensions;
    [Space(10)]
    [Header("Skill Target Area Data")]
    [Tooltip("Valid Area that skill can target within. Use Range if you prefer an int value")]
    [SerializeField] protected Vector2 validTargetArea; //Vector 2. Valid Target Area. For Melee attacks. Leave at 1,1. FKA: Range.
    [HideIf("skillShape", SkillShape.Rectangular)]
    [Tooltip("If the targetArea can't be defined. E.g, Diagonals, Crosses. Set validTargetArea to 0,0 to use range instead.")]
    [SerializeField] protected int range;
    [Title("Skill Behaviour")]
    [Tooltip("Target Area or Range is based on player’s current position. This also means show all valid positions regardless of direction.")]
    [SerializeField] protected bool originateFromUnitCentre;
    [Tooltip("Can skill hit a unit then again hit the unit behind. Or is it blocked once hit during path")]
    [SerializeField] bool canPenetrate;
    [Space(10)]
    [ShowIf("skillShape", SkillShape.Rectangular)]
    [Tooltip("Whether the calculation should include diagonal grids N unit away or use the Manhattan distance to filter them out.")]
    [SerializeField] bool includeDiagonals;
    [HideIf("includeDiagonals")]
    [Tooltip("if not including diagonals, use this to define which cells should be removed. (Used when calculating ManhattenDistance)")]
    [SerializeField] int maxNumCellsFromUnit = 1;
    [Title("Attachers")]
    [SerializeField] protected Transform unitCameraRootTransform;
    [SerializeField] protected Transform unitTransformAttacher;
    [Title("Skill Force")]
    [SerializeField] protected SkillForceType forceTypeToApply = SkillForceType.None;
    [Tooltip("Unit Forward: Apply force in direction related to the acting unit's forward direction. PositionDirection: Apply force in direction of (Target.GridPosition - Attacker.GridPosition)")]
    [HideIf("forceTypeToApply", SkillForceType.None)]
    [SerializeField] protected SkillForceDirectionType forceDirection = SkillForceDirectionType.PositionDirection;
    [Range(0, 9)]
    [HideIf("forceTypeToApply", SkillForceType.None)]
    [SerializeField] protected int forceDistance = 0;

    //Event
    protected CharacterGridUnit myCharacter;
    public Action<CharacterGridUnit> SkillOwnerSet;

    //State Variables
    public bool isActive { get; set; } = false;

    //Cache
    protected Transform myUnitMoveTransform;
    protected BoxCollider moveTransformGridCollider;

    protected LevelGrid levelGrid;
    protected CinemachineImpulseSource impulseSource;

    protected SkillData mySkillData;

    //Storage
    protected List<GridPosition> selectedGridPositions = new List<GridPosition>();

    protected List<GridUnit> selectedUnits = new List<GridUnit>();
    protected Dictionary<GridPosition, IHighlightable> highlightableData = new Dictionary<GridPosition, IHighlightable>(); //For Grid Visual

    protected List<GridUnit> skillTargets { get; private set; } = new List<GridUnit>();

    protected bool canTargetKOEDUnits = false;
    protected bool canTargetSelf;
    protected bool isTargetSelfOnlySkill;

    //Counters
    protected int numOfHealthUIDisplay = 0;
    protected int healthUIDisplayedCounter = 0;

    protected bool anyTargetsWithReflectAffinity = false;

    protected enum SkillShape
    {
        Rectangular,
        Cross,
        Diagonal,
        CompassStar
    }

    protected virtual void Awake()
    {
        levelGrid = LevelGrid.Instance;
        impulseSource = GetComponent<CinemachineImpulseSource>();
        canTargetSelf = targets.Contains(FantasyCombatTarget.Self);
        isTargetSelfOnlySkill = canTargetSelf && targets.Count == 1;

        if (myUnit)
        {
            if (this is IOffensiveSkill offensiveSkill)
            {
                offensiveSkill.GetOffensiveSkillData().SetupData(this, myUnit);
            }

            myCharacter = myUnit as CharacterGridUnit;
            moveTransformGridCollider = myUnit.gridCollider;
            myUnitMoveTransform = myUnit.transform;
        }
    }

    //ABSTRACT FUNCS
    public abstract void OnSkillInterrupted(BattleResult battleResult, IBattleTrigger battleTrigger);

    //END ABSTRACT

    public virtual void Setup(SkillPrefabSetter skillPrefabSetter, SkillData skillData)
    {
        mySkillData = skillData;
        myUnit = skillPrefabSetter.characterGridUnit;
        myCharacter = skillPrefabSetter.characterGridUnit;
        moveTransformGridCollider = myUnit.gridCollider;

        unitCameraRootTransform.parent = skillPrefabSetter.cameraRootTransform;
        HandyFunctions.ResetTransform(unitCameraRootTransform, true);

        unitTransformAttacher.parent = myUnit.transform;
        HandyFunctions.ResetTransform(unitTransformAttacher, true);

        transform.parent = skillPrefabSetter.skillHeader;
        HandyFunctions.ResetTransform(transform, true);

        SkillOwnerSet?.Invoke(myCharacter);
    }

    public virtual void BeginAction()
    {
        FantasyCombatManager.Instance.CombatEnded += OnSkillInterrupted;
        FantasyCombatManager.Instance.SetCurrentAction(GetSkillAction(), false);
    }

    public void DisplayUnitHealthUIComplete()
    {
        if (!isActive) { return; }

        int totalToCheck = anyTargetsWithReflectAffinity ? numOfHealthUIDisplay + 1 : numOfHealthUIDisplay;

        healthUIDisplayedCounter++;

        Debug.Log("Health UI Complete Count: " + healthUIDisplayedCounter + " Total: " + totalToCheck);

        if (healthUIDisplayedCounter >= totalToCheck)
        {
            OnAllHealthUIComplete();

            //Reset health Data
            ResetHealthUIData();
        }
    }

    protected virtual void OnAllHealthUIComplete()
    {
        EndAction();
    }

    //CALLED VIA FEEDBACKS
    public virtual void Attack()
    {
        if (this is IOffensiveSkill offensiveSkill)
        {
            offensiveSkill.Attack(skillTargets, ref anyTargetsWithReflectAffinity);
        }    
    }

    public void RaiseHealthChangeEvent(int eventType) //Helpful event for feedbacks to raise. 
    {
        ((ICombatAction)this).ActionAnimEventRaised((GridUnitAnimNotifies.EventType)eventType);
    }

    //END OF CALLED VIA FEEDBACKS

    public virtual void EndAction()
    {
        if (this is IOffensiveSkill offensiveSkill)
        {
            offensiveSkill.GetOffensiveSkillData().ReturnVFXToPool();
        }

        ResetData();

        FantasyCombatManager.Instance.SetCurrentAction(this, true);
        FantasyCombatManager.Instance.ActionComplete?.Invoke();
    }

    protected virtual void ResetData()
    {
        ResetHealthUIData();
        skillTargets.Clear();
        selectedUnits.Clear();
        selectedGridPositions.Clear();
    }

    protected virtual void SkillComplete()
    {
        FantasyCombatManager.Instance.CombatEnded -= OnSkillInterrupted;
    }

    protected virtual void SetUnitsToShow()
    {
        List<GridUnit> targetedUnits;

        if (this is IOffensiveSkill)
        {
            targetedUnits = CombatFunctions.SetOffensiveSkillUnitsToShow(myUnit, selectedUnits, forceDistance);
        }
        else
        {
            targetedUnits = new List<GridUnit>(selectedUnits)
            {
                myUnit
            };
        }

        FantasyCombatManager.Instance.SetUnitsToShow(targetedUnits);
    }

    protected void SetSkillTargets()
    {
        skillTargets = new List<GridUnit>(selectedUnits);
        numOfHealthUIDisplay = AffectTargetsIndividually() ? 1 : skillTargets.Count;
    }

    private void ResetHealthUIData()
    {
        numOfHealthUIDisplay = 0;
        healthUIDisplayedCounter = 0;
        anyTargetsWithReflectAffinity = false;
    }

    //SKILL CALCULATION

    protected virtual void CalculateSelectedGridPos()
    {
        //If Grid Selection is not required. We can assume the valid Target Area and Skill dimensions to be the same.
        selectedGridPositions = GetFilteredGridPosList();

        SetSelectedUnits();
    }

    protected virtual void SetSelectedUnits()
    {
        selectedUnits.Clear();
        highlightableData.Clear();

        if (isTargetSelfOnlySkill)
        {
            return;
        }

        foreach (GridPosition gridPosition in selectedGridPositions)
        {
            GridUnit foundUnit = null;

            if (GetTargetingCondition(gridPosition))
            {
                GridUnit selectedUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);

                if (!selectedUnits.Contains(selectedUnit) && IsUnitValidTarget(selectedUnit))
                {
                    selectedUnits.Add(selectedUnit);
                    foundUnit = selectedUnit;
                }
            }

            highlightableData[gridPosition] = foundUnit?.GetHighlightable();
        }
    }

    protected List<GridPosition> GetFilteredGridPosList()
    {
        //If a self Only Skill
        if (isTargetSelfOnlySkill)
        {
            return myUnit.GetCurrentGridPositions();
        }

        List<GridPosition>  listToReturn = FilterByShape();

        //Debug.Log("Filter By SHape Count: " + listToReturn.Count);

        //This is an unfiltered list of a valid Grid Pos within Range or Target Are
        if (!includeDiagonals && skillShape == SkillShape.Rectangular)
        {
            //Filter with manhantten distance.
            listToReturn = RemoveDiagonalGridPosFromList(listToReturn);
            //Debug.Log("Removed Diagonal Count: " + listToReturn.Count);
        }

        if (!canTargetSelf)
        {
            //remove all Grid Pos of self from list.
            listToReturn = RemoveSelfFromGridPosList(listToReturn);
            //Debug.Log("Remove Self from Grid Count: " + listToReturn.Count);
        }

        if (!canPenetrate && skillShape != SkillShape.Diagonal) //Diagonal has already done removal of pass through logic
        {
            listToReturn = RemovePassThroughLogic(listToReturn, skillShape == SkillShape.CompassStar);
        }

        return listToReturn;
    }

    protected List<GridPosition> GetValidUnfilteredGridPositionsFromCentre()
    {
        Transform skillOwnerMoveTransform = GetSkillOwnerMoveTransform();

        if (Vector3.Angle(skillOwnerMoveTransform.forward, Vector3.forward) < 45 || Vector3.Angle(skillOwnerMoveTransform.forward, Vector3.back) < 45)
        {
            int XOffset = validTargetArea == Vector2.zero ? range : Mathf.FloorToInt(validTargetArea.x / 2);
            int ZOffset = validTargetArea == Vector2.zero ? range : Mathf.FloorToInt(validTargetArea.y / 2);

            int XOrigin = -XOffset + levelGrid.GetColliderBoundMinInGridPos(GetSkillOwnerMoveGridCollider()).x;
            int ZOrigin = -ZOffset + levelGrid.GetColliderBoundMinInGridPos(GetSkillOwnerMoveGridCollider()).z;

            int XEnd = XOffset + levelGrid.GetColliderBoundMaxInGridPos(GetSkillOwnerMoveGridCollider()).x;
            int ZEnd = ZOffset + levelGrid.GetColliderBoundMaxInGridPos(GetSkillOwnerMoveGridCollider()).z;

            return GetValidUnfilteredGridPositions(XOrigin, XEnd, ZOrigin, ZEnd);
        }
        else if (Vector3.Angle(skillOwnerMoveTransform.forward, Vector3.right) < 45 || Vector3.Angle(skillOwnerMoveTransform.forward, Vector3.left) < 45)
        {
            int XOffset = validTargetArea == Vector2.zero ? range : Mathf.FloorToInt(validTargetArea.y / 2);
            int ZOffset = validTargetArea == Vector2.zero ? range : Mathf.FloorToInt(validTargetArea.x / 2);

            int XOrigin = -XOffset + levelGrid.GetColliderBoundMinInGridPos(GetSkillOwnerMoveGridCollider()).x;
            int ZOrigin = -ZOffset + levelGrid.GetColliderBoundMinInGridPos(GetSkillOwnerMoveGridCollider()).z;

            int XEnd = XOffset + levelGrid.GetColliderBoundMaxInGridPos(GetSkillOwnerMoveGridCollider()).x;
            int ZEnd = ZOffset + levelGrid.GetColliderBoundMaxInGridPos(GetSkillOwnerMoveGridCollider()).z;

            return GetValidUnfilteredGridPositions(XOrigin, XEnd, ZOrigin, ZEnd);
        }
        else
        {
            Debug.Log("UNIT FACING INVALID DIRECTION");
            return new List<GridPosition>();
        }
    }

    protected List<GridPosition> GetValidUnfilteredGridPositionsBasedOnDirection()
    {
        BoxCollider skillOwnerMoveGridCollider = GetSkillOwnerMoveGridCollider();
        int horizontalCellsOccupied = CombatFunctions.GetHorizontalCellsOccupied(GetSkillOwnerMoveGridCollider());

        if (GetCardinalDirection() == Direction.North)
        {
            //Facing Vector3.Forward
            int skillWidth = validTargetArea == Vector2.zero ? range : Mathf.FloorToInt(validTargetArea.x);
            int offset = Mathf.FloorToInt((skillWidth - horizontalCellsOccupied) / 2);

            int XOrigin = levelGrid.GetColliderBoundMinInGridPos(skillOwnerMoveGridCollider).x - offset;
            int XEnd = levelGrid.GetColliderBoundMaxInGridPos(skillOwnerMoveGridCollider).x + offset;

            int ZOrigin = levelGrid.GetColliderBoundMaxInGridPos(skillOwnerMoveGridCollider).z + 1;
            int ZEnd = validTargetArea == Vector2.zero ? levelGrid.GetColliderBoundMaxInGridPos(skillOwnerMoveGridCollider).z + range : levelGrid.GetColliderBoundMaxInGridPos(skillOwnerMoveGridCollider).z + Mathf.FloorToInt(validTargetArea.y);

           /*Debug.Log("XOrigin: " + XOrigin);
            Debug.Log("XEnd: " + XEnd);
            Debug.Log("ZOrigin: " + ZOrigin);
            Debug.Log("ZEnd: " + ZEnd);*/

            return GetValidUnfilteredGridPositions(XOrigin, XEnd, ZOrigin, ZEnd);
        }
        else if (GetCardinalDirection() == Direction.East)
        {
            //Facing Vector3.Right
            Vector2 targetArea = new Vector2(validTargetArea.y, validTargetArea.x);
            int skillWidth = validTargetArea == Vector2.zero ? range : Mathf.FloorToInt(validTargetArea.x);
            int offset = Mathf.FloorToInt((skillWidth - horizontalCellsOccupied) / 2);

            int ZOrigin = levelGrid.GetColliderBoundMinInGridPos(skillOwnerMoveGridCollider).z - offset;
            int ZEnd = levelGrid.GetColliderBoundMaxInGridPos(skillOwnerMoveGridCollider).z + offset;

            int XOrigin = levelGrid.GetColliderBoundMaxInGridPos(skillOwnerMoveGridCollider).x + 1;
            int XEnd = validTargetArea == Vector2.zero ? levelGrid.GetColliderBoundMaxInGridPos(skillOwnerMoveGridCollider).x + range : levelGrid.GetColliderBoundMaxInGridPos(skillOwnerMoveGridCollider).x + Mathf.FloorToInt(targetArea.x);

            return GetValidUnfilteredGridPositions(XOrigin, XEnd, ZOrigin, ZEnd);
        }
        else if (GetCardinalDirection() == Direction.West)
        {
            //Facing Vector3.Left
            Vector2 targetArea = new Vector2(validTargetArea.y, validTargetArea.x);
            int skillWidth = validTargetArea == Vector2.zero ? range : Mathf.FloorToInt(validTargetArea.x);
            int offset = Mathf.FloorToInt((skillWidth - horizontalCellsOccupied) / 2);

            int ZOrigin = levelGrid.GetColliderBoundMinInGridPos(skillOwnerMoveGridCollider).z - offset;
            int ZEnd = levelGrid.GetColliderBoundMaxInGridPos(skillOwnerMoveGridCollider).z + offset;

            int XOrigin = validTargetArea == Vector2.zero ? levelGrid.GetColliderBoundMinInGridPos(skillOwnerMoveGridCollider).x - range : levelGrid.GetColliderBoundMinInGridPos(skillOwnerMoveGridCollider).x - Mathf.FloorToInt(targetArea.x);
            int XEnd = levelGrid.GetColliderBoundMaxInGridPos(skillOwnerMoveGridCollider).x - 1;

            return GetValidUnfilteredGridPositions(XOrigin, XEnd, ZOrigin, ZEnd);
        }
        else if (GetCardinalDirection() == Direction.South)
        {
            //Facing Vector3.Back
            int skillWidth = validTargetArea == Vector2.zero ? range : Mathf.FloorToInt(validTargetArea.x);
            int offset = Mathf.FloorToInt((skillWidth - horizontalCellsOccupied) / 2);

            int XOrigin = levelGrid.GetColliderBoundMinInGridPos(skillOwnerMoveGridCollider).x - offset;
            int XEnd = levelGrid.GetColliderBoundMaxInGridPos(skillOwnerMoveGridCollider).x + offset;

            int ZOrigin = validTargetArea == Vector2.zero ? levelGrid.GetColliderBoundMinInGridPos(skillOwnerMoveGridCollider).z - range : levelGrid.GetColliderBoundMinInGridPos(skillOwnerMoveGridCollider).z - Mathf.FloorToInt(validTargetArea.y);
            int ZEnd = levelGrid.GetColliderBoundMaxInGridPos(skillOwnerMoveGridCollider).z - 1;

            return GetValidUnfilteredGridPositions(XOrigin, XEnd, ZOrigin, ZEnd);
        }
        else
        {
            Debug.Log("UNIT FACING INVALID DIRECTION");
            return new List<GridPosition>();
        }
    }

    protected List<GridPosition> GetValidUnfilteredGridPositions(int XOrigin, int XEnd, int ZOrigin, int ZEnd)
    {
        List<GridPosition> validGridPositionsList = new List<GridPosition>();

        for (int x = XOrigin; x <= XEnd; x++)
        {
            for (int z = ZOrigin; z <= ZEnd; z++)
            {
                GridPosition gridPosition = new GridPosition(x, z);

                if (!IsGridPositionValid(gridPosition))
                {
                    continue;
                }

                validGridPositionsList.Add(gridPosition);
            }
        }
        return validGridPositionsList;
    }

    //Filters
    protected List<GridPosition> RemoveDiagonalGridPosFromList(List<GridPosition> gridPositions)
    {
        List<GridPosition> listToReturn = new List<GridPosition>();

        foreach (GridPosition gridPosition in gridPositions)
        {
            foreach (GridPosition unitGridPosition in GetGridPositionsAtHypotheticalPos(GetSkillOwnerMoveTransform().position))
            {
                //Calculate Manhattan distance && Remove from List if not in range.
                //abs(x1 - x2) + abs(y1 - y2)

                if (Mathf.Abs(unitGridPosition.x - gridPosition.x) + Mathf.Abs(unitGridPosition.z - gridPosition.z) <= maxNumCellsFromUnit)
                {
                    listToReturn.Add(gridPosition);
                }
            }
        }

        return listToReturn;
    }

    protected List<GridPosition> RemoveSelfFromGridPosList(List<GridPosition> gridPositions)
    {
        List<GridPosition> listToReturn = new List<GridPosition>();

        foreach (GridPosition gridPosition in gridPositions)
        {
            if (GetGridPositionsAtHypotheticalPos(GetSkillOwnerMoveTransform().position).Contains(gridPosition))
            {
                continue;
            }

            listToReturn.Add(gridPosition);
        }

        return listToReturn;
    }

    protected List<GridPosition> RemovePassThroughLogic(List<GridPosition> gridPositions, bool checkDiagonals)
    {
        BoxCollider skillOwnerMoveGridCollider = GetSkillOwnerMoveGridCollider();

        //Bound GridPos
        GridPosition unitTopRight = levelGrid.GetColliderBoundMaxInGridPos(skillOwnerMoveGridCollider);
        GridPosition unitBottomLeft = levelGrid.GetColliderBoundMinInGridPos(skillOwnerMoveGridCollider);
        GridPosition unitTopLeft = new GridPosition(unitBottomLeft.x, unitTopRight.z);
        GridPosition unitBottomRight = new GridPosition(unitTopRight.x, unitBottomLeft.z);

        //Lists
        List<GridPosition> listToReturn = gridPositions;
        List<GridPosition> unitsHitAtPos = new List<GridPosition>();

        foreach (GridPosition gridPosition in gridPositions)
        {
            if (IsCurrentGridPositionOccupiedByAnotherUnit(gridPosition, canTargetKOEDUnits))
            {
                unitsHitAtPos.Add(gridPosition);
            }
        }

        foreach (GridPosition gridPosition in unitsHitAtPos)
        {
            bool isToMyUnitRight = gridPosition.x > unitTopRight.x;
            bool isAboveMyUnit = gridPosition.z > unitTopRight.z;
            bool isToMyUnitLeft = gridPosition.x < unitTopLeft.x;
            bool isBelowMyUnit = gridPosition.z < unitBottomRight.z;

            List<GridPosition> gridPosBehind = new List<GridPosition>();

            if (checkDiagonals)
            {
                bool isToWorldNorthEast = isAboveMyUnit && isToMyUnitRight && CombatFunctions.IsGridPositionOnDiagonalAxis(gridPosition, unitTopRight);
                bool isToWorldNorthWest = isAboveMyUnit && isToMyUnitLeft && CombatFunctions.IsGridPositionOnDiagonalAxis(gridPosition, unitTopLeft);
                bool isToWorldSouthEast = isBelowMyUnit && isToMyUnitRight && CombatFunctions.IsGridPositionOnDiagonalAxis(gridPosition, unitBottomRight);
                bool isToWorldSouthWest = isBelowMyUnit && isToMyUnitLeft && CombatFunctions.IsGridPositionOnDiagonalAxis(gridPosition, unitBottomLeft);

                if (isToWorldNorthEast)
                {
                    //Check if there are units behind
                    gridPosBehind = listToReturn.Where((hitGridPos) => 
                    CombatFunctions.IsGridPositionOnDiagonalAxis(hitGridPos, gridPosition) && gridPosition.x < hitGridPos.x && gridPosition.z < hitGridPos.z).ToList();
                }
                else if (isToWorldNorthWest)
                {
                    gridPosBehind = listToReturn.Where((hitGridPos) =>
                    CombatFunctions.IsGridPositionOnDiagonalAxis(hitGridPos, gridPosition) && gridPosition.x > hitGridPos.x && gridPosition.z < hitGridPos.z).ToList();
                }
                else if (isToWorldSouthEast)
                {
                    gridPosBehind = listToReturn.Where((hitGridPos) =>
                    CombatFunctions.IsGridPositionOnDiagonalAxis(hitGridPos, gridPosition) && gridPosition.x < hitGridPos.x && gridPosition.z > hitGridPos.z).ToList();
                }
                else if (isToWorldSouthWest)
                {
                    gridPosBehind = listToReturn.Where((hitGridPos) => 
                    CombatFunctions.IsGridPositionOnDiagonalAxis(hitGridPos, gridPosition) && gridPosition.x > hitGridPos.x && gridPosition.z > hitGridPos.z).ToList();
                }

                listToReturn = listToReturn.Except(gridPosBehind).ToList();

                if(!gridPosBehind.IsNullOrEmpty()) //Means it was filled in by one of the above so don't do the below.
                    continue;
            }

            if (isToMyUnitRight && (GetCardinalDirection() == Direction.East || originateFromUnitCentre))
            {
                //Check if there are units behind
                gridPosBehind = listToReturn.Where((hitGridPos) => hitGridPos.z == gridPosition.z && hitGridPos.x > gridPosition.x).ToList();
            }
            else if (isToMyUnitLeft && (GetCardinalDirection() == Direction.West || originateFromUnitCentre))
            {
                gridPosBehind = listToReturn.Where((hitGridPos) => hitGridPos.z == gridPosition.z && hitGridPos.x < gridPosition.x).ToList();
            }
            else if (isAboveMyUnit && (GetCardinalDirection() == Direction.North || originateFromUnitCentre))
            {
                gridPosBehind = listToReturn.Where((hitGridPos) => hitGridPos.x == gridPosition.x && hitGridPos.z > gridPosition.z).ToList();
            }
            else if (isBelowMyUnit && (GetCardinalDirection() == Direction.South || originateFromUnitCentre))
            {
                gridPosBehind = listToReturn.Where((hitGridPos) => hitGridPos.x == gridPosition.x && hitGridPos.z < gridPosition.z).ToList();
            }

            listToReturn = listToReturn.Except(gridPosBehind).ToList();
        }

        return listToReturn;
    }

    protected List<GridPosition> FilterByShape()
    {
        switch (skillShape)
        {
            case SkillShape.Cross:
                return FilterIntoCross(GetValidUnfilteredGridPositions(), false);
            case SkillShape.Diagonal:
                return FilterIntoDiagonal();
            case SkillShape.CompassStar:
                List<GridPosition> filteredStar = FilterIntoCross(GetValidUnfilteredGridPositions(), true);
                AddMissingDiagonalsToStar(ref filteredStar);
                return filteredStar;
            default:
                return GetValidUnfilteredGridPositions();
        }
    }

    private List<GridPosition> GetValidUnfilteredGridPositions()
    {
        return originateFromUnitCentre ? GetValidUnfilteredGridPositionsFromCentre() : GetValidUnfilteredGridPositionsBasedOnDirection();
    }

    //Shape Filter
    protected List<GridPosition> FilterIntoCross(List<GridPosition> gridPositions, bool includeDiagonals)
    {
        List<GridPosition> listToReturn = new List<GridPosition>();

        foreach (GridPosition gridPosition in gridPositions)
        {
            foreach (GridPosition unitGridPosition in GetGridPositionsAtHypotheticalPos(GetSkillOwnerMoveTransform().position))
            {
                bool isCrossGridPos = gridPosition.x == unitGridPosition.x || gridPosition.z == unitGridPosition.z;
                bool isDiagonalGridPos = CombatFunctions.IsGridPositionOnDiagonalAxis(gridPosition, unitGridPosition);

                if (isCrossGridPos || (includeDiagonals && isDiagonalGridPos))
                {
                    if (!listToReturn.Contains(gridPosition))
                        listToReturn.Add(gridPosition);
                }
            }
        }

        return listToReturn;
    }

    private void AddMissingDiagonalsToStar(ref List<GridPosition> gridPositions)
    {
        if(originateFromUnitCentre) { return; } //These Pos are only missing when using range

        BoxCollider skillOwnerMoveGridCollider = GetSkillOwnerMoveGridCollider();

        //Bound GridPos
        GridPosition unitTopRight = levelGrid.GetColliderBoundMaxInGridPos(skillOwnerMoveGridCollider);
        GridPosition unitBottomLeft = levelGrid.GetColliderBoundMinInGridPos(skillOwnerMoveGridCollider);
        GridPosition unitTopLeft = new GridPosition(unitBottomLeft.x, unitTopRight.z);
        GridPosition unitBottomRight = new GridPosition(unitTopRight.x, unitBottomLeft.z);

        for(int i = 1; i <= range; ++i)
        {
            GridPosition NEGridPos = new GridPosition(unitTopRight.x + i, unitTopRight.z + i);
            GridPosition SWGridPos = new GridPosition(unitBottomLeft.x - i, unitBottomLeft.z - i);
            GridPosition NWGridPos = new GridPosition(unitTopLeft.x - i, unitTopLeft.z + i);
            GridPosition SEGridPos = new GridPosition(unitBottomRight.x + i, unitBottomRight.z -  i);

            List<GridPosition> diagonalPosList = new List<GridPosition>();

            if (GetCardinalDirection() == Direction.North)
            {
                //North, Add NE & NW
                diagonalPosList = new List<GridPosition> {NEGridPos, NWGridPos };
            }
            else if (GetCardinalDirection() == Direction.South)
            {
                //South, Add SW & SE
                diagonalPosList = new List<GridPosition> { SEGridPos, SWGridPos };
            }
            else if (GetCardinalDirection() == Direction.East)
            {
                //East, add SE & NE
                diagonalPosList = new List<GridPosition> { SEGridPos, NEGridPos };
            }
            else if (GetCardinalDirection() == Direction.West)
            {
                //West, add SW & NW
                diagonalPosList = new List<GridPosition> { SWGridPos, NWGridPos };
            }
            else
            {
                Debug.Log("UNIT FACING INVALID DIRECTION");
            }

            foreach (GridPosition diagonalPos in diagonalPosList)
            {
                if (gridPositions.Contains(diagonalPos) || !IsGridPositionValid(diagonalPos))
                {
                    continue;
                }

                gridPositions.Add(diagonalPos);
            }
        }
    }

    protected List<GridPosition> FilterIntoDiagonal()
    {
        BoxCollider skillOwnerMoveGridCollider = GetSkillOwnerMoveGridCollider();

        //Bound GridPos
        GridPosition unitTopRight = levelGrid.GetColliderBoundMaxInGridPos(skillOwnerMoveGridCollider);
        GridPosition unitBottomLeft = levelGrid.GetColliderBoundMinInGridPos(skillOwnerMoveGridCollider);
        GridPosition unitTopLeft = new GridPosition(unitBottomLeft.x, unitTopRight.z);
        GridPosition unitBottomRight = new GridPosition(unitTopRight.x, unitBottomLeft.z);

        //List
        List<GridPosition> listToReturn = new List<GridPosition>();
        GridPosition startingGridPos;

        //Counters
        int XAddition;
        int ZAddition;

        if (GetDiagonalDirection() == Direction.NorthEast)
        {
            //Facing NorthEast
            XAddition = 1;
            ZAddition = 1;

            startingGridPos = unitTopRight;

        }
        else if (GetDiagonalDirection() == Direction.SouthEast)
        {
            //Facing SouthEast
            XAddition = -1;
            ZAddition = 1;

            startingGridPos = unitBottomRight;

        }
        else if (GetDiagonalDirection() == Direction.SouthWest)
        {
            //Facing SouthWest
            XAddition = -1;
            ZAddition = -1;

            startingGridPos = unitBottomLeft;

        }
        else if (GetDiagonalDirection() == Direction.NorthWest)
        {
            //Facing NorthWest
            XAddition = 1;
            ZAddition = -1;

            startingGridPos = unitTopLeft;
        }
        else
        {
            XAddition = 1;
            ZAddition = 1;

            startingGridPos = unitTopRight;
        }

        listToReturn.Add(startingGridPos);

        for (int i = 0; i < range; i++)
        {
            GridPosition lastGridPosInList = listToReturn[listToReturn.Count - 1];
            GridPosition newGridPos = new GridPosition(lastGridPosInList.x + XAddition, lastGridPosInList.z + ZAddition);

            if (!LevelGrid.Instance.gridSystem.IsValidGridPosition(newGridPos)) //Check if grid pos within grid.
            {
                continue;
            }

            if (!canPenetrate)
            {
                if (IsCurrentGridPositionOccupiedByAnotherUnit(newGridPos, canTargetKOEDUnits))
                {
                    listToReturn.Add(newGridPos);
                    break;
                }
            }

            listToReturn.Add(newGridPos);
        }

        return listToReturn;
    }

    //Direction Methods
    protected virtual Direction GetCardinalDirection()
    {
        return CombatFunctions.GetCardinalDirection(GetDirectionTransform());
    }

    protected virtual Direction GetDiagonalDirection()
    {
        return CombatFunctions.GetDiagonalDirection(GetDirectionTransform());
    }

    protected Vector3 GetCardinalDirectionAsVector()
    {
        return CombatFunctions.GetCardinalDirectionAsVector(GetDirectionTransform());
    }

    //Getters
    protected virtual Transform GetDirectionTransform()
    {
        return myUnitMoveTransform;
    }

    protected virtual Transform GetSkillOwnerMoveTransform()
    {
        return myUnitMoveTransform;
    }

    protected virtual BoxCollider GetSkillOwnerMoveGridCollider()
    {
        return moveTransformGridCollider;
    }
    protected bool IsUnitValidTarget(GridUnit target)
    {
        return CombatFunctions.IsUnitValidTarget(targets, myUnit, target);
    }

    protected List<GridPosition> GetGridPositionsAtHypotheticalPos(Vector3 newWorldPosition)
    {
        return CombatFunctions.GetGridPositionsAtHypotheticalPos(newWorldPosition, GetSkillOwnerMoveTransform(), GetSkillOwnerMoveGridCollider());
    }

    protected virtual bool IsGridPositionValid(GridPosition gridPosition)
    {
        return LevelGrid.Instance.gridSystem.IsValidGridPosition(gridPosition) && !LevelGrid.Instance.TryGetObstacleAtPosition(gridPosition, out Collider obstacleData);
    }

    protected bool GetTargetingCondition(GridPosition gridPosition)
    {
        return canTargetSelf ? LevelGrid.Instance.IsGridPositionOccupiedByUnit(gridPosition, false) : LevelGrid.Instance.IsGridPositionOccupiedByDifferentUnit(myUnit, gridPosition, canTargetKOEDUnits);
    }

    protected bool IsCurrentGridPositionOccupiedByAnotherUnit(GridPosition gridPosition, bool includeKOedUnits)
    {
        return LevelGrid.Instance.IsGridPositionOccupiedByDifferentUnit(myUnit, gridPosition, includeKOedUnits);
    }

    public bool IsDiagonal()
    {
        //It's possible skillShape was accidentally set to diagonal, so check range is greater than 0.
        return skillShape == SkillShape.Diagonal && range > 0;
    }

    public SkillData GetSkillData()
    {
        return mySkillData;
    }

    protected virtual ICombatAction GetSkillAction()
    {
        return this;
    }

    public SkillForceData? GetSkillForceData(GridUnit target)
    {
        if(forceTypeToApply == SkillForceType.None)
        {
            return null;
        }

        return new SkillForceData(GetForceToApplyToUnit(target), forceDirection, forceDistance);
    }

    public virtual bool IsMultiActionSkill()
    {
        return false;
    }

    public virtual bool AffectTargetsIndividually()
    {
        return false;
    }

    private SkillForceType GetForceToApplyToUnit(GridUnit target)
    {
        if (forceTypeToApply != SkillForceType.KnockbackEnemiesSuctionAllies)
        {
            return forceTypeToApply;
        }

        switch (CombatFunctions.GetRelationWithTarget(myCharacter, target))
        {
            case FantasyCombatTarget.Ally:
                return SkillForceType.SuctionAll;
            case FantasyCombatTarget.Enemy: 
            case FantasyCombatTarget.Object:
                return SkillForceType.KnockbackAll;
            default:
                return SkillForceType.None;
        }
    }
}
