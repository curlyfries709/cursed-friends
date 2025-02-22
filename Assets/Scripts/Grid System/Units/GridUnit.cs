using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GridUnit : MonoBehaviour
{
    [Header("Grid Unit Profile")]
    public string unitName;
    public CombatUnitType unitType;
    [Header("Grid Unit Components")]
    public Transform camFollowTarget;
    public Transform modelHeader;
    public BoxCollider gridCollider;

    //Events
    public Func<DamageData, DamageModifier> ModifyDamageReceived;

    //Variables
    protected int totalCellsRequired;
    List<GridPosition> gridPositionsOccupied = new List<GridPosition>();

    //Cache
    protected IDamageable damageable;

    protected virtual void Awake()
    {
        damageable = GetComponent<IDamageable>();
    }

    protected virtual void OnEnable()
    {
        if (!(this is CharacterGridUnit))
        {
            //Only Set Grid Pos for Objects.
            SetGridPositions();
        }
    }

    private void OnDisable()
    {
        if (!(this is CharacterGridUnit))
        {
            //Only for Objects.
            LevelGrid.Instance.RemoveUnitFromGrid(this);
        }
    }

    protected virtual void Start()
    {
        totalCellsRequired = CalculateTotalCellsRequired();
    }

    public virtual void Warp(Vector3 destination, Quaternion rotation)
    {
        //gridCollider.enabled = false;
        transform.position = destination;
        transform.rotation = rotation;
        //gridCollider.enabled = true;
    }

    public void SetGridPositions()
    {
        UpdateTurnStartGridPositions();
        LevelGrid.Instance.SetUnitAtGridPosistions(gridPositionsOccupied, this);
    }

    public void MovedToNewGridPos()
    {
        List<GridPosition> orginalGridPositions = new List<GridPosition>(gridPositionsOccupied);

        UpdateTurnStartGridPositions();
        LevelGrid.Instance.UnitMovedGridPositions(this, orginalGridPositions, gridPositionsOccupied);
    }

    private void UpdateTurnStartGridPositions()
    {
        GridSystem<GridObject> gridSystem = LevelGrid.Instance.gridSystem;
        gridPositionsOccupied.Clear();

        gridCollider.enabled = false;

        for (int x = gridSystem.GetGridPosition(gridCollider.bounds.min).x; x <= gridSystem.GetGridPosition(gridCollider.bounds.max).x; x++)
        {
            for (int z = gridSystem.GetGridPosition(gridCollider.bounds.min).z; z <= gridSystem.GetGridPosition(gridCollider.bounds.max).z; z++)
            {
                gridPositionsOccupied.Add(new GridPosition(x, z));
            }
        }

        gridCollider.enabled = true;

    }

    public List<GridPosition> GetCurrentGridPositions(bool disableColliderForAccurateCheck = true)
    {
        GridSystem<GridObject> gridSystem = LevelGrid.Instance.gridSystem;
        List<GridPosition> currentGridPositions = new List<GridPosition>();

        if(disableColliderForAccurateCheck)
            gridCollider.enabled = false;

        for (int x = gridSystem.GetGridPosition(gridCollider.bounds.min).x; x <= gridSystem.GetGridPosition(gridCollider.bounds.max).x; x++)
        {
            for (int z = gridSystem.GetGridPosition(gridCollider.bounds.min).z; z <= gridSystem.GetGridPosition(gridCollider.bounds.max).z; z++)
            {
                currentGridPositions.Add(new GridPosition(x, z));
            }
        }

        if(disableColliderForAccurateCheck)
            gridCollider.enabled = true;

        return currentGridPositions;
    }

    public List<GridPosition> GetGridPositionsAtHypotheticalPos(Vector3 newWorldPosition)
    {
        GridSystem<GridObject> gridSystem = LevelGrid.Instance.gridSystem;
        List<GridPosition> currentGridPositions = new List<GridPosition>();
        Vector3 offset = newWorldPosition - transform.position;

        gridCollider.enabled = false;

        for (int x = gridSystem.GetGridPosition(gridCollider.bounds.min + offset).x; x <= gridSystem.GetGridPosition(gridCollider.bounds.max + offset).x; x++)
        {
            for (int z = gridSystem.GetGridPosition(gridCollider.bounds.min + offset).z; z <= gridSystem.GetGridPosition(gridCollider.bounds.max + offset).z; z++)
            {
                currentGridPositions.Add(new GridPosition(x, z));
            }
        }

        gridCollider.enabled = true;
        return currentGridPositions;
    }



    //GETTERS
    public List<GridPosition> GetGridPositionsOnTurnStart()
    {
        return gridPositionsOccupied;
    }

    public int GetMaxCellsRequired()
    {
        return totalCellsRequired;
    }

    public int GetVerticalCellsOccupied()
    {
        return Mathf.CeilToInt(gridCollider.size.z / LevelGrid.Instance.GetCellSize());
    }

    public int GetHorizontalCellsOccupied()
    {
        int value = Mathf.CeilToInt(gridCollider.size.x / LevelGrid.Instance.GetCellSize());
        if(value > 1)
        {
            Debug.Log("HORIZONTAL CELLS OCCUPIED: " + value);
        }
        return Mathf.CeilToInt(gridCollider.size.x / LevelGrid.Instance.GetCellSize());
    }

    

    public Vector3 GetClosestPointOnColliderToPosition(Vector3 worldPos)
    {
        return gridCollider.ClosestPointOnBounds(worldPos);
    }
    private int CalculateTotalCellsRequired()
    {
        return GetVerticalCellsOccupied() * GetHorizontalCellsOccupied();
    }

    public IDamageable GetDamageable()
    {
        return damageable;
    }


    //SETTERS
    public void ShowSelectionVisual(bool show)
    {
        damageable.ActivateHealthVisual(show);
    }
}
