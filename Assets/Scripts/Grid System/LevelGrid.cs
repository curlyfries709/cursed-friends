using Pathfinding;
using Pathfinding.Graphs.Grid;
using System.Collections.Generic;
using UnityEngine;

public class LevelGrid : MonoBehaviour   
{
    public static LevelGrid Instance { get; private set; }

    [Header("Grid Data")]
    [SerializeField] float cellSize = 2;

    public GridSystem<GridObject> gridSystem = null;
    List<GridUnit> allActiveGridUnits = new List<GridUnit>();

    //Cache
    Dictionary<BoxCollider, List<GridPosition>> setDynamicObstacleData = new Dictionary<BoxCollider, List<GridPosition>>();

    private int gridWidth;
    private int gridLength;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        SavingLoadingManager.Instance.EnteringNewTerritory += OnEnteringNewTerritory;
    }

    public void OnNewGridSceneLoadedEarly(SceneData currentSceneData)
    {
        FantasySceneData fantasySceneData = currentSceneData as FantasySceneData;

        if (!fantasySceneData) { return; }

        gridWidth = fantasySceneData.gridWidth;
        gridLength = fantasySceneData.gridLength;

        gridSystem = new GridSystem<GridObject>(gridWidth, gridLength, cellSize, (GridSystem<GridObject> g, GridPosition gridPosition) => new GridObject(g, gridPosition));

        GridSystemVisual.Instance.Setup();
    }

    private void OnEnteringNewTerritory()
    {
        gridSystem = null;
    }

    private void OnDisable()
    {
        SavingLoadingManager.Instance.EnteringNewTerritory -= OnEnteringNewTerritory;
    }

    public void SetDynamicObstacle(BoxCollider obstacleCollider, Collider modelCollider, bool set) //If set false, obstacle will be removed.
    {
        if (set && !setDynamicObstacleData.ContainsKey(obstacleCollider))
        {
            int minX = GetColliderBoundMinInGridPos(obstacleCollider).x;
            int maxX = GetColliderBoundMaxInGridPos(obstacleCollider).x;

            int minZ = GetColliderBoundMinInGridPos(obstacleCollider).z;
            int maxZ = GetColliderBoundMaxInGridPos(obstacleCollider).z;

            //Create in dictionary
            setDynamicObstacleData[obstacleCollider] = new List<GridPosition>();

            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    GridPosition gridPosition = new GridPosition(x, z);

                    //Set Data
                    //Debug.Log(obstacleCollider.name + " Setting dynamic obstacle at grid pos" + gridPosition.ToString());
                    GridObject gridObject = GetGridObjectAtPosition(gridPosition);

                    if (gridObject.SetObstacle(modelCollider))
                    {
                        //Add To Dict
                        setDynamicObstacleData[obstacleCollider].Add(gridPosition);
                        gridObject.SetPerformedObstacleCheck(true);
                    }
                }
            }
        }
        else if(!set && setDynamicObstacleData.ContainsKey(obstacleCollider))
        {
            //Remove Obstacle From Grid Object
            foreach (GridPosition gridPosition in setDynamicObstacleData[obstacleCollider])
            {
                //Set Data
                //Debug.Log(obstacleCollider.name + " removing dynamic obstacle at grid pos" + gridPosition.ToString());

                GridObject gridObject = GetGridObjectAtPosition(gridPosition);
                gridObject.RemoveObstacle(modelCollider);
            }

            //Remove From Dictionary
            setDynamicObstacleData.Remove(obstacleCollider);
        }
    }

    public void SetUnitAtGridPosistions(List<GridPosition> gridPositions, GridUnit unit)
    {
        if (!allActiveGridUnits.Contains(unit))
        {
            allActiveGridUnits.Add(unit);
        }

        foreach (GridPosition gridPosition in gridPositions)
        {
            GridObject gridObject = gridSystem.GetGridObject(gridPosition);
            gridObject.SetGridUnit(unit);
        }
    }

    public void RemoveUnitFromGrid(GridUnit unit)
    {
        allActiveGridUnits.Remove(unit);
        RemoveUnitAtGridPositions(unit, unit.GetGridPositionsOnTurnStart());
    }

    public GridUnit GetUnitAtGridPosition(GridPosition gridPosition)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        return gridObject.GetGridUnit();
    }

    public void RemoveUnitAtGridPositions(GridUnit unit, List<GridPosition> gridPositions)
    {
        foreach (GridPosition gridPosition in gridPositions)
        {
            GridObject gridObject = gridSystem.GetGridObject(gridPosition);

            if (gridObject.MatchesOccupyingUnit(unit))
            {
                gridObject.SetGridUnit(null);
            }
            else
            {
                GridUnit occupyingUnit = gridObject.GetGridUnit();

                Debug.Log("The unit " + unit.unitName + " that you are trying to remove from " + gridPosition.ToString() 
                    + " doesn't match the occupying unit: " + (occupyingUnit ? occupyingUnit.unitName : "NULL"));
            }
        }
    }

    public void UnitMovedGridPositions(GridUnit unit, List<GridPosition> fromGridPositions, List<GridPosition> toGridPositions)
    {
        RemoveUnitAtGridPositions(unit, fromGridPositions);
        SetUnitAtGridPosistions(toGridPositions, unit);
    }

    //GETTERS
    public bool IsWalkable(CharacterGridUnit unit, GridPosition gridPosition)
    {
        bool isWalkable = IsWalkable(gridPosition);

        if (!isWalkable)
        {
            return false;
        }

        Debug.Log("Update function IsWalkable with Unit argument in LevelGrid to include hazard check");

        return isWalkable;
    }

    public bool IsWalkable(GridPosition gridPosition)
    {
        return !IsObjectAtGridPosition(gridPosition) && !TryGetObstacleAtPosition(gridPosition, out Collider obstacleData);
    }

    public GridObject GetGridObjectAtPosition(GridPosition gridPosition)
    {
        return gridSystem.GetGridObject(gridPosition);
    }

    public bool TryGetObstacleAtPosition(GridPosition gridPosition, out Collider obstacleData) 
    {
        GridObject gridObject = GetGridObjectAtPosition(gridPosition);

        if (!gridObject.testedForObstacle) //Check if point hasn't been tested for obstacle
        {
            PerformObstacleCheck(gridPosition, gridObject);
        }

        return gridObject.TryGetObstacle(out obstacleData);
    }

    private void PerformObstacleCheck(GridPosition gridPosition, GridObject gridObject)
    {
        GridNodeBase gridNode = AstarPath.active.data.gridGraph.GetNode(gridPosition.x, gridPosition.z);

        if (!gridNode.Walkable)//Check if node marked as unwalkable
        {
            Vector3 worldPos = gridSystem.GetWorldPosition(gridPosition);
            worldPos.y = worldPos.y - 1;

            Collider obstacleCollider = GameSystemsManager.Instance.GetSceneDataAsFantasyData().GetTerrainCollider();

            float sphereCastRadius = 0.5f;
            float sphereCastDistance = 11f;

            LayerMask layerMask = AstarPath.active.data.gridGraph.collision.mask;

            if (Physics.SphereCast(worldPos, sphereCastRadius, Vector3.up, out RaycastHit hitInfo, sphereCastDistance, layerMask, QueryTriggerInteraction.Ignore)) //If didn't hit, then assume it is terrain obstacle
            {
                obstacleCollider = hitInfo.collider;
            }

            gridObject.SetObstacle(obstacleCollider);

            Debug.Log("Obstacle Check at Grid Position: " + gridPosition.ToString() + " Detected Collider: " + obstacleCollider.name);
        }

        gridObject.SetPerformedObstacleCheck(true);
    }

    public float GetGridHeightAtWorldPosition(GridPosition gridPosition, Vector3 worldPos)
    {
        GridObject gridObject = GetGridObjectAtPosition(gridPosition);

        if (!gridObject.performedHeightCheck)
        {
            gridObject.SetHeightAtCentre(PerformHeightCheckAtWorldPos(worldPos).y);
        }

        return gridObject.heightAtCentre;
    }

    /*public Quaternion GetVisualRotation(GridPosition gridPosition)
    {
        Vector3 worldPosAtCentre = gridSystem.GetWorldPosition(gridPosition);



    }*/

    private Vector3 PerformHeightCheckAtWorldPos(Vector3 worldPos)
    {
        GraphCollision graphCollision = AstarPath.active.data.gridGraph.collision;
        return graphCollision.CheckHeight(worldPos);
    }

    public List<GridUnit> GetAllActiveGridUnits()
    {
        return allActiveGridUnits;
    }

    public bool IsGridPositionOccupiedByUnit(GridPosition gridPosition, bool includeKOedUnits)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);

        if (includeKOedUnits)
        {
            return gridObject.IsOccupiedByAnyUnit();
        }
        else
        {
            return gridObject.IsOccupiedByActiveUnit();
        }
    }

    public bool IsGridPositionOccupiedByCharacterUnit(GridPosition gridPosition, bool includeKOedUnits)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);

        if (includeKOedUnits)
        {
            return gridObject.IsOccupiedByAnyUnit() && !IsObjectAtGridPosition(gridPosition);
        }
        else
        {
            return gridObject.IsOccupiedByActiveUnit() && !IsObjectAtGridPosition(gridPosition);
        }
    }

    public GridPosition GetColliderBoundMinInGridPos(BoxCollider gridCollider)
    {
        return gridSystem.GetGridPosition(gridCollider.bounds.min);
    }

    public GridPosition GetColliderBoundMaxInGridPos(BoxCollider gridCollider)
    {
        return gridSystem.GetGridPosition(gridCollider.bounds.max);
    }

    public bool IsGridPositionOccupiedByDifferentUnit(GridUnit myUnit, GridPosition gridPosition, bool includeKOedUnits)
    {
        return IsGridPositionOccupiedByUnit(gridPosition, includeKOedUnits) && GetUnitAtGridPosition(gridPosition) != myUnit;
    }

    public bool CanOccupyGridPosition(GridUnit unit, GridPosition gridPosition)
    {
        return !IsGridPositionOccupiedByDifferentUnit(unit, gridPosition, true) &&
            IsWalkable(gridPosition);
    }

    public GridNodeBase GetGridNode(GridPosition gridPosition)
    {
        GridGraph gridGraph = AstarPath.active.data.gridGraph;
        return gridGraph.GetNode(gridPosition.x, gridPosition.z);
    }

    public bool IsObjectAtGridPosition(GridPosition gridPos)
    {
        GridUnit unit = GetUnitAtGridPosition(gridPos);

        if (!unit)
            return false;

        return unit.IsObject();
    }

    public Vector2 GetCellCentreNormalized()
    {
        return new Vector2(0.5f, 0.5f);
    }

    public float GetCellSize()
    {
        return cellSize;
    }

    public int GetWidth()
    {
        return gridWidth;
    }

    public int GetLength()
    {
        return gridLength;
    }

    public bool IsGridSystemValid()
    {
        return gridSystem != null;
    }
}
