using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Cinemachine;
using AnotherRealm;
using System.Linq;
using System;

public abstract class BaseSkill : MonoBehaviour
{
    //IMPORTANT VARIABLES
    [PropertyOrder(-9)]
    [Title("Unit")]
    [SerializeField] protected CharacterGridUnit myUnit;
    [Title("Target Behaviour")]
    [Tooltip("Can this skill only target one unit? Leave false to target multiple")]
    [SerializeField] protected bool isSingleTarget; //Target Area is automatically 1X1 for this.
    [Space(10)]
    [ListDrawerSettings(Expanded = true)]
    [LabelText("Who can the Skill target?")]
    [Tooltip("Who does the skill target? Allies, Enemies, interactables or multiple.")]
    [SerializeField] protected List<FantasyCombatTarget> targets;
    [Title("Skill Dimensions Data")]
    [Tooltip("The Shape of the skill. Is it a simple Line. A cross? A Rectangle? Diagonal?")]
    [SerializeField] protected SkillShape skillShape;
    [Tooltip("The dimensions of the skill in Grid Units. 2x1 means 2 cells on the x and 1 cell on the z. Leave as 1X1 for single target melees")]
    [SerializeField] protected Vector2 skillDimensions;
    [Space(10)]
    [Header("Skill Target Area Data")]
    [ShowIf("skillShape", SkillShape.Rectangular)]
    [Tooltip("Valid Area that skill can target within. Use Range if you prefer an int value")]
    [SerializeField] protected Vector2 validTargetArea; //Vector 2. Valid Target Area. For Melee attacks. Leave at 1,1. FKA: Range.
    [HideIf("skillShape", SkillShape.Rectangular)]
    [Tooltip("If the targetArea can't be defined. E.g, Diagonals, Crosses. Use this. ")]
    [SerializeField] protected int range;
    [Title("Skill Behaviour")]
    [Tooltip("Target Area or Range is based on player’s current position. This also means show all valid positions regardless of direction.")]
    [SerializeField] protected bool originateFromUnitCentre;
    [Tooltip("Can skill hit a unit then again hit the unit behind. Or is it blocked once hit during path")]
    [SerializeField] bool canPenetrate;
    [Space(10)]
    [Tooltip("Whether the calculation should include diagonal grids N unit away or use the Manhattan distance.")]
    [SerializeField] bool includeDiagonals;
    [HideIf("includeDiagonals")]
    [Tooltip("if not including diagonals, use this to define which cells should be removed. (Used when calculating ManhattenDistance)")]
    [SerializeField] int maxNumCellsFromUnit = 1;
    [Header("Base Components")]
    [SerializeField] protected Transform unitCameraRootTransform;

    //Event
    public Action<CharacterGridUnit> SkillOwnerSet;

    //Cache
    protected Transform myUnitMoveTransform;
    protected BoxCollider moveTransformGridCollider;

    protected LevelGrid levelGrid;
    protected CinemachineImpulseSource impulseSource;

    protected SkillData mySkillData;

    //Storage
    protected List<GridPosition> selectedGridPositions = new List<GridPosition>();
    protected List<GridUnit> selectedUnits = new List<GridUnit>();


    protected bool canTargetKOEDUnits = false;
    protected bool canTargetSelf;
    protected bool isTargetSelfOnlySkill;

    protected enum SkillShape
    {
        Rectangular,
        Cross,
        Diagonal
    }

    protected virtual void Awake()
    {
        levelGrid = LevelGrid.Instance;
        impulseSource = GetComponent<CinemachineImpulseSource>();
        canTargetSelf = targets.Contains(FantasyCombatTarget.Self);
        isTargetSelfOnlySkill = canTargetSelf && targets.Count == 1;
    }

    public virtual void Setup(SkillPrefabSetter skillPrefabSetter, SkillData skillData)
    {
        mySkillData = skillData;
        myUnit = skillPrefabSetter.characterGridUnit;
        moveTransformGridCollider = myUnit.gridCollider;

        unitCameraRootTransform.parent = skillPrefabSetter.cameraRootTransform;
        HandyFunctions.ResetTransform(unitCameraRootTransform, true);

        transform.parent = skillPrefabSetter.skillHeader;
        HandyFunctions.ResetTransform(transform, true);

        SkillOwnerSet?.Invoke(myUnit);
    }

    protected void OnSkillTriggered()
    {
        FantasyCombatManager.Instance.CombatEnded += OnSkillInterrupted;
    }
    public abstract void OnSkillInterrupted(BattleResult battleResult, IBattleTrigger battleTrigger);

    protected virtual void SkillComplete()
    {
        FantasyCombatManager.Instance.CombatEnded -= OnSkillInterrupted;
    }

    protected virtual void CalculateSelectedGridPos()
    {
        //If Grid Selection is not required. We can assume the valid Target Area and Skill dimensions to be the same.
        selectedGridPositions = GetFilteredGridPosList();

        SetSelectedUnits();
    }


    protected void SetSelectedUnits()
    {
        selectedUnits.Clear();

        if (isTargetSelfOnlySkill)
        {
            return;
        }

        foreach (GridPosition gridPosition in selectedGridPositions)
        {
            if (GetTargetingCondition(gridPosition))
            {
                GridUnit selectedUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);

                if (!selectedUnits.Contains(selectedUnit) && CombatFunctions.IsUnitValidTarget(targets, myUnit, selectedUnit))
                {
                    selectedUnits.Add(selectedUnit);
                }
            }
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
        if (!includeDiagonals)
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

        if (!canPenetrate && skillShape != SkillShape.Diagonal)
        {
            listToReturn = RemovePassThroughLogic(listToReturn);
            //Debug.Log("Remove Pass Through Logic Count: " + listToReturn.Count);
        }

        return listToReturn;
    }

    protected List<GridPosition> GetValidUnfilteredGridPositionsFromCentre()
    {
        if (Vector3.Angle(myUnitMoveTransform.forward, Vector3.forward) < 45 || Vector3.Angle(myUnitMoveTransform.forward, Vector3.back) < 45)
        {
            int XOffset = validTargetArea == Vector2.zero ? range : Mathf.FloorToInt(validTargetArea.x / 2);
            int ZOffset = validTargetArea == Vector2.zero ? range : Mathf.FloorToInt(validTargetArea.y / 2);

            int XOrigin = -XOffset + levelGrid.GetColliderBoundMinInGridPos(moveTransformGridCollider).x;
            int ZOrigin = -ZOffset + levelGrid.GetColliderBoundMinInGridPos(moveTransformGridCollider).z;

            int XEnd = XOffset + levelGrid.GetColliderBoundMaxInGridPos(moveTransformGridCollider).x;
            int ZEnd = ZOffset + levelGrid.GetColliderBoundMaxInGridPos(moveTransformGridCollider).z;

            return GetValidUnfilteredGridPositions(XOrigin, XEnd, ZOrigin, ZEnd);
        }
        else if (Vector3.Angle(myUnitMoveTransform.forward, Vector3.right) < 45 || Vector3.Angle(myUnitMoveTransform.forward, Vector3.left) < 45)
        {
            int XOffset = validTargetArea == Vector2.zero ? range : Mathf.FloorToInt(validTargetArea.y / 2);
            int ZOffset = validTargetArea == Vector2.zero ? range : Mathf.FloorToInt(validTargetArea.x / 2);

            int XOrigin = -XOffset + levelGrid.GetColliderBoundMinInGridPos(moveTransformGridCollider).x;
            int ZOrigin = -ZOffset + levelGrid.GetColliderBoundMinInGridPos(moveTransformGridCollider).z;

            int XEnd = XOffset + levelGrid.GetColliderBoundMaxInGridPos(moveTransformGridCollider).x;
            int ZEnd = ZOffset + levelGrid.GetColliderBoundMaxInGridPos(moveTransformGridCollider).z;

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
        if (GetDirection() == Direction.North)
        {
            //Facing Vector3.Forward
            int skillWidth = validTargetArea == Vector2.zero ? range : Mathf.FloorToInt(validTargetArea.x);
            int offset = Mathf.FloorToInt((skillWidth - myUnit.GetHorizontalCellsOccupied()) / 2);

            int XOrigin = levelGrid.GetColliderBoundMinInGridPos(moveTransformGridCollider).x - offset;
            int XEnd = levelGrid.GetColliderBoundMaxInGridPos(moveTransformGridCollider).x + offset;

            int ZOrigin = levelGrid.GetColliderBoundMaxInGridPos(moveTransformGridCollider).z + 1;
            int ZEnd = validTargetArea == Vector2.zero ? levelGrid.GetColliderBoundMaxInGridPos(moveTransformGridCollider).z + range : levelGrid.GetColliderBoundMaxInGridPos(moveTransformGridCollider).z + Mathf.FloorToInt(validTargetArea.y);

           /*Debug.Log("XOrigin: " + XOrigin);
            Debug.Log("XEnd: " + XEnd);
            Debug.Log("ZOrigin: " + ZOrigin);
            Debug.Log("ZEnd: " + ZEnd);*/

            return GetValidUnfilteredGridPositions(XOrigin, XEnd, ZOrigin, ZEnd);
        }
        else if (GetDirection() == Direction.East)
        {
            //Facing Vector3.Right
            Vector2 targetArea = new Vector2(validTargetArea.y, validTargetArea.x);
            int skillWidth = validTargetArea == Vector2.zero ? range : Mathf.FloorToInt(validTargetArea.x);
            int offset = Mathf.FloorToInt((skillWidth - myUnit.GetHorizontalCellsOccupied()) / 2);

            int ZOrigin = levelGrid.GetColliderBoundMinInGridPos(moveTransformGridCollider).z - offset;
            int ZEnd = levelGrid.GetColliderBoundMaxInGridPos(moveTransformGridCollider).z + offset;

            int XOrigin = levelGrid.GetColliderBoundMaxInGridPos(moveTransformGridCollider).x + 1;
            int XEnd = validTargetArea == Vector2.zero ? levelGrid.GetColliderBoundMaxInGridPos(moveTransformGridCollider).x + range : levelGrid.GetColliderBoundMaxInGridPos(moveTransformGridCollider).x + Mathf.FloorToInt(targetArea.x);

            return GetValidUnfilteredGridPositions(XOrigin, XEnd, ZOrigin, ZEnd);
        }
        else if (GetDirection() == Direction.West)
        {
            //Facing Vector3.Left
            Vector2 targetArea = new Vector2(validTargetArea.y, validTargetArea.x);
            int skillWidth = validTargetArea == Vector2.zero ? range : Mathf.FloorToInt(validTargetArea.x);
            int offset = Mathf.FloorToInt((skillWidth - myUnit.GetHorizontalCellsOccupied()) / 2);

            int ZOrigin = levelGrid.GetColliderBoundMinInGridPos(moveTransformGridCollider).z - offset;
            int ZEnd = levelGrid.GetColliderBoundMaxInGridPos(moveTransformGridCollider).z + offset;

            int XOrigin = validTargetArea == Vector2.zero ? levelGrid.GetColliderBoundMinInGridPos(moveTransformGridCollider).x - range : levelGrid.GetColliderBoundMinInGridPos(moveTransformGridCollider).x - Mathf.FloorToInt(targetArea.x);
            int XEnd = levelGrid.GetColliderBoundMaxInGridPos(moveTransformGridCollider).x - 1;

            return GetValidUnfilteredGridPositions(XOrigin, XEnd, ZOrigin, ZEnd);
        }
        else if (GetDirection() == Direction.South)
        {
            //Facing Vector3.Back
            int skillWidth = validTargetArea == Vector2.zero ? range : Mathf.FloorToInt(validTargetArea.x);
            int offset = Mathf.FloorToInt((skillWidth - myUnit.GetHorizontalCellsOccupied()) / 2);

            int XOrigin = levelGrid.GetColliderBoundMinInGridPos(moveTransformGridCollider).x - offset;
            int XEnd = levelGrid.GetColliderBoundMaxInGridPos(moveTransformGridCollider).x + offset;

            int ZOrigin = validTargetArea == Vector2.zero ? levelGrid.GetColliderBoundMinInGridPos(moveTransformGridCollider).z - range : levelGrid.GetColliderBoundMinInGridPos(moveTransformGridCollider).z - Mathf.FloorToInt(validTargetArea.y);
            int ZEnd = levelGrid.GetColliderBoundMaxInGridPos(moveTransformGridCollider).z - 1;

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
        //List<GridPosition> unitGridPositions = myUnit.GetCurrentGridPositions();
        for (int x = XOrigin; x <= XEnd; x++)
        {
            for (int z = ZOrigin; z <= ZEnd; z++)
            {
                GridPosition gridPosition = new GridPosition(x, z);

                if (!LevelGrid.Instance.gridSystem.IsValidGridPosition(gridPosition) || !LevelGrid.Instance.IsWalkable(gridPosition))
                {
                    continue;
                }

                validGridPositionsList.Add(gridPosition);


                /*foreach (GridPosition unitGridPosition in unitGridPositions)
                {
                    GridPosition testGridPosition = unitGridPosition + offsetGridPosition;
                    if (!gridSystem.IsValidGridPosition(testGridPosition))
                    {
                        continue;
                    }

                    validGridPositionsList.Add(testGridPosition);
                }*/
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
            foreach (GridPosition unitGridPosition in myUnit.GetGridPositionsAtHypotheticalPos(myUnitMoveTransform.position))
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
            if (myUnit.GetGridPositionsAtHypotheticalPos(myUnitMoveTransform.position).Contains(gridPosition))
            {
                continue;
            }

            listToReturn.Add(gridPosition);
        }

        return listToReturn;
    }

    protected List<GridPosition> RemovePassThroughLogic(List<GridPosition> gridPositions)
    {
        //Bound GridPos
        GridPosition unitTopRight = levelGrid.GetColliderBoundMaxInGridPos(moveTransformGridCollider);
        GridPosition unitBottomLeft = levelGrid.GetColliderBoundMinInGridPos(moveTransformGridCollider);
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

            if (isToMyUnitRight && (GetDirection() == Direction.East || originateFromUnitCentre))
            {
                //Check if there are units behind
                gridPosBehind = listToReturn.Where((hitGridPos) => hitGridPos.z == gridPosition.z && hitGridPos.x > gridPosition.x).ToList();
            }
            else if (isToMyUnitLeft && (GetDirection() == Direction.West || originateFromUnitCentre))
            {
                gridPosBehind = listToReturn.Where((hitGridPos) => hitGridPos.z == gridPosition.z && hitGridPos.x < gridPosition.x).ToList();
            }
            else if (isAboveMyUnit && (GetDirection() == Direction.North || originateFromUnitCentre))
            {
                gridPosBehind = listToReturn.Where((hitGridPos) => hitGridPos.x == gridPosition.x && hitGridPos.z > gridPosition.z).ToList();
            }
            else if (isBelowMyUnit && (GetDirection() == Direction.South || originateFromUnitCentre))
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
                return FilterIntoCross(GetValidUnfilteredGridPositionsFromCentre());
            case SkillShape.Diagonal:
                return FilterIntoDiagonal();
            default:
                return originateFromUnitCentre ? GetValidUnfilteredGridPositionsFromCentre() : GetValidUnfilteredGridPositionsBasedOnDirection();
        }
    }
    //Shape Filter
    protected List<GridPosition> FilterIntoCross(List<GridPosition> gridPositions)
    {
        List<GridPosition> listToReturn = new List<GridPosition>();

        foreach (GridPosition gridPosition in gridPositions)
        {
            foreach (GridPosition unitGridPosition in myUnit.GetGridPositionsAtHypotheticalPos(myUnitMoveTransform.position))
            {
                if (gridPosition.x == unitGridPosition.x || gridPosition.z == unitGridPosition.z)
                {
                    if (!listToReturn.Contains(gridPosition))
                        listToReturn.Add(gridPosition);
                }
            }
        }

        return listToReturn;
    }

    protected List<GridPosition> FilterIntoDiagonal()
    {
        //Bound GridPos
        GridPosition unitTopRight = levelGrid.GetColliderBoundMaxInGridPos(moveTransformGridCollider);
        GridPosition unitBottomLeft = levelGrid.GetColliderBoundMinInGridPos(moveTransformGridCollider);
        GridPosition unitTopLeft = new GridPosition(unitBottomLeft.x, unitTopRight.z);
        GridPosition unitBottomRight = new GridPosition(unitTopRight.x, unitBottomLeft.z);

        //List
        List<GridPosition> listToReturn = new List<GridPosition>();
        GridPosition startingGridPos;

        //Counters
        int XAddition = 0;
        int ZAddition = 0;

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

            if (!LevelGrid.Instance.gridSystem.IsValidGridPosition(newGridPos))
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
    protected virtual Direction GetDirection()
    {
        return CombatFunctions.GetDirection(myUnitMoveTransform);
    }

    protected virtual Direction GetDiagonalDirection()
    {
        return CombatFunctions.GetDiagonalDirection(myUnitMoveTransform);
    }

    protected Vector3 GetDirectionAsVector()
    {
        return CombatFunctions.GetDirectionAsVector(myUnitMoveTransform);
    }

    //Getters

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


}
