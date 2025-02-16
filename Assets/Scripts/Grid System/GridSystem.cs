
using UnityEngine;
using System;
public class GridSystem<TGridObject> 
{
    private int width;
    private int length;
    private float cellSize;
    private TGridObject[,] gridObjectArray;

    public GridSystem(int width, int length, float cellSize, Func<GridSystem<TGridObject>, GridPosition, TGridObject> createGridObject)
    {
        this.width = width;
        this.length = length;
        this.cellSize = cellSize;

        gridObjectArray = new TGridObject[width, length];

        for(int x = 0; x < width; x++)
        {
            for(int z = 0; z < length; z++)
            {
                GridPosition gridPosition = new GridPosition(x, z);
                gridObjectArray[x,z] = createGridObject(this, gridPosition);
            }
        }
    }

    public Vector3 GetWorldPosition(GridPosition gridPosition)
    {
        Vector3 worldPos = new Vector3(gridPosition.x * cellSize, 0, gridPosition.z * cellSize);
        float yHeight = LevelGrid.Instance.GetGridHeightAtWorldPosition(gridPosition, worldPos);
        worldPos.y = yHeight;

        return worldPos;
    }

    public GridPosition GetGridPosition(Vector3 worldPosition)
    {
        return new GridPosition(Mathf.RoundToInt(worldPosition.x / cellSize), Mathf.RoundToInt(worldPosition.z / cellSize));
    }

    public Vector3 RoundWorldPositionToGridPosition(Vector3 worldPosition)
    {
        return GetWorldPosition(GetGridPosition(worldPosition));
    }

    public TGridObject GetGridObject(GridPosition gridPosition)
    {
        return gridObjectArray[gridPosition.x, gridPosition.z];
    }

    public bool IsValidGridPosition(int gridPosX, int gridPosZ)
    {
        return IsValidGridPosition(new GridPosition(gridPosX, gridPosZ));
    }

    public bool IsValidGridPosition(GridPosition gridPosition)
    {
        return gridPosition.x >= 0 && 
            gridPosition.z >= 0 && 
            gridPosition.x < width && 
            gridPosition.z < length;
    }

    public void CreateDebugObjects(Transform debugPrefab)
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < length; z++)
            {
                GridPosition gridPosition = new GridPosition(x, z);

                Transform debugTransform = GameObject.Instantiate(debugPrefab, GetWorldPosition(gridPosition) + new Vector3(0, 0.001f, 0), Quaternion.identity);

            }
        }
    }
}
