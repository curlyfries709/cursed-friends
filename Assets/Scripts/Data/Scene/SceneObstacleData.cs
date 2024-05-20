using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "New Scene Obstacle Data", menuName = "Configs/Scene Obstacle Data", order = 9)]
public class SceneObstacleData : ScriptableObject
{
    //[ReadOnly]
    [SerializeField] int gridWidth = 50;
    [ReadOnly]
    [SerializeField] int gridLength = 150;

    [ReadOnly]
    [SerializeField] float cellSize = 2;

    public GridSystem<PathNode> gridSystem;
 
    public void Setup(int width, int length, float cellSize)
    {
        gridWidth = width;
        gridLength = length;
        this.cellSize = cellSize;

        gridSystem = new GridSystem<PathNode>(width, length, cellSize, (GridSystem<PathNode> g, GridPosition gridPosition) => new PathNode(gridPosition));
    }

    public void SetNodeVisualData(GridPosition nodePosition, float YPosition, float centreHeight, Quaternion rotation)
    {
        PathNode node = GetNode(nodePosition.x, nodePosition.z);
        node.SetGridVisualTransform(YPosition, centreHeight, rotation);
    }

    public void SetNodeIsWalkable(GridPosition nodePosition, bool isWalkable)
    {
        PathNode node = GetNode(nodePosition.x, nodePosition.z);
        node.SetIsWalkable(isWalkable);
    }

    public PathNode GetNode(int x, int z)
    {
        return gridSystem.GetGridObject(new GridPosition(x, z));
    }

}
