using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGrid : MonoBehaviour   
{
    public static LevelGrid Instance { get; private set; }

    [Header("Grid Data")]
    [SerializeField] float cellSize = 2;
    [Space(10)]
    [SerializeField] PathFinding pathFinding;

    public GridSystem<GridObject> gridSystem;
    List<GridUnit> allActiveGridUnits = new List<GridUnit>();

    //Cache
    public SceneData currentSceneData { get; private set; }

    private int gridWidth;
    private int gridLength;
    

    private void Awake()
    {
        Instance = this;
        OnNewGridSceneLoaded();
    }

    private void Start()
    {
        currentSceneData.BakeData();
        GridSystemVisual.Instance.Setup(); 
    }

    private void OnNewGridSceneLoaded()
    {
        currentSceneData = FindObjectOfType<SceneData>();

        gridWidth = currentSceneData.gridWidth;
        gridLength = currentSceneData.gridLength;

        gridSystem = new GridSystem<GridObject>(gridWidth, gridLength, cellSize, (GridSystem<GridObject> g, GridPosition gridPosition) => new GridObject(g, gridPosition));
        pathFinding.Setup(gridWidth, gridLength, cellSize);
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
        RemoveUnitAtGridPositions(unit.GetGridPositionsOnTurnStart());
    }

    public GridUnit GetUnitAtGridPosition(GridPosition gridPosition)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        return gridObject.GetGridUnit();
    }

    public void RemoveUnitAtGridPositions(List<GridPosition> gridPositions)
    {
        foreach (GridPosition gridPosition in gridPositions)
        {
            GridObject gridObject = gridSystem.GetGridObject(gridPosition);
            gridObject.SetGridUnit(null);
        }
    }

    public void UnitMovedGridPositions(GridUnit unit, List<GridPosition> fromGridPositions, List<GridPosition> toGridPositions)
    {
        RemoveUnitAtGridPositions(fromGridPositions);
        SetUnitAtGridPosistions(toGridPositions, unit);
    }

    //GETTERS
    public GridObject GetGridObjectAtPosition(GridPosition gridPosition)
    {
        return gridSystem.GetGridObject(gridPosition);
    }

    public bool TryGetObstacleAtPosition(GridPosition gridPosition, out Collider obstacleData)
    {
        GridObject gridObject = GetGridObjectAtPosition(gridPosition);
        return gridObject.TryGetObstacle(out obstacleData);
    }

    public List<GridUnit> GetAllActiveGridUnits()
    {
        return allActiveGridUnits;
    }

    public bool IsGridPositionOccupied(GridPosition gridPosition, bool includeKOedUnits)
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
        return IsGridPositionOccupied(gridPosition, includeKOedUnits) && GetUnitAtGridPosition(gridPosition) != myUnit;
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
}
