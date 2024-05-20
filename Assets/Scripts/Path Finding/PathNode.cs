using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathNode 
{
    [SerializeField] private Transform gridDebugObject;

    private GridPosition gridPosition;
    private int gCost;
    private int hCost;
    private int fCost;

    private PathNode previousPathNode;
    private bool isWalkable = true;

    private float visualYPosition;
    private float centreHeight = 0;
    private Quaternion gridVisualRotation;

    public PathNode(GridPosition gridPosition)
    {
        this.gridPosition = gridPosition;
    }

    public override string ToString()
    {
        return gridPosition.ToString();
    }

    //Doers
    public void CalculateFCost()
    {
        fCost = hCost + gCost;
    }

    public void ResetPreviousPathNode()
    {
        previousPathNode = null;
    }

    //Setters
    public void SetGCost(int gCost)
    {
        this.gCost = gCost;
    }

    public void SetHCost(int hCost)
    {
        this.hCost = hCost;
    }

    public void SetPreviousNode(PathNode pathNode)
    {
        previousPathNode = pathNode;
    }

    public void SetIsWalkable(bool isWalkable)
    {
        this.isWalkable = isWalkable;
    }

    public void SetGridVisualTransform(float newYpos, float centreHeight, Quaternion rotation)
    {
        this.centreHeight = centreHeight;
        visualYPosition = newYpos;
        gridVisualRotation = rotation;
    }

    //Getters
    public int GetFCost()
    {
        return fCost;
    }

    public int GetGCost()
    {
        return gCost;
    }

    public int GetHCost()
    {
        return hCost;
    }

    public GridPosition GetGridPosition()
    {
        return gridPosition;
    }

    public PathNode GetPreviousNode()
    {
        return previousPathNode;
    }

    public bool IsWalkable()
    {
        return isWalkable;
    }

    public float GetVisualYPosition()
    {
        return visualYPosition;
    }

    public float GetHeightOfCentre()
    {
        return centreHeight;
    }

    public Quaternion GetRotation()
    {
        return gridVisualRotation;
    }

}
