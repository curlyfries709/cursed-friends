using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public enum ObstacleType
{
    Hollow,
    Filled
}


public class GridObject
{
    private GridSystem<GridObject> gridSystem;
    private GridPosition gridPosition;
    private GridUnit gridUnit;

    //Obstacle Data.
    Collider obstacleCollider;

    public GridObject(GridSystem<GridObject> gridSystem, GridPosition gridPosition)
    {
        this.gridSystem = gridSystem;
        this.gridPosition = gridPosition;
    }

    //Getters && Setters
    public void SetGridUnit(GridUnit gridUnit)
    {
        this.gridUnit = gridUnit;
    }

    public GridUnit GetGridUnit()
    {
        return gridUnit;
    }

    public bool IsOccupiedByAnyUnit()
    {
        return gridUnit;
    }

    public bool IsOccupiedByActiveUnit()
    {
        CharacterGridUnit character = gridUnit as CharacterGridUnit;

        if (character)
        {
            return !character.Health().isKOed;
        }
        else
        {
            return gridUnit;
        }
    }

    public bool SetObstacle(Collider obstacleCollider)
    {
        if(this.obstacleCollider && this.obstacleCollider is TerrainCollider) { return false; }//Cannot Set on Terrain obstacle

        this.obstacleCollider = obstacleCollider;
        return true;
    }

    public bool TryGetObstacle(out Collider obstacleData)
    {
        obstacleData = obstacleCollider;
        return obstacleCollider;
    }

    public bool RemoveObstacle(Collider obstacleCollider)
    {
        if (this.obstacleCollider && this.obstacleCollider is TerrainCollider) {return false; }//Cannot Remove Terrain obstacle

        this.obstacleCollider = null;
        return true;
    }

    /*public bool CanUnitMoveThroughThis()
    {
        return !gridUnit || (gridUnit && gridUnit.Health().isKOed);
    }*/

}
