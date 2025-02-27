using AnotherRealm;
using System;
using System.Collections.Generic;
using UnityEngine;

public class GridUnit : MonoBehaviour
{
    [Header("Grid Unit Profile")]
    public string unitName;
    public CombatUnitType unitType;
    [Header("Stats")]
    public UnitStats stats;
    [Header("Grid Unit Components")]
    public Transform camFollowTarget;
    public Transform modelHeader;
    [Space(10)]
    public BoxCollider gridCollider;

    //Events
    public Func<DamageData, DamageModifier> ModifyDamageDealt;
    public Func<DamageData, DamageModifier> ModifyDamageReceived;

    //Variables
    protected int totalCellsRequired;
    List<GridPosition> gridPositionsOccupied = new List<GridPosition>();

    //Cache
    protected Health myHealth;

    protected virtual void Awake()
    {
        myHealth = GetComponent<Health>();
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

    //GENERAL HELPERS
    public void ActivateUnit(bool activate)
    {
        if (activate && Health() && Health().isKOed) { return; }
        transform.parent.gameObject.SetActive(activate);
    }

    //GRID HELPERS
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
    public Vector3 GetClosestPointOnColliderToPosition(Vector3 worldPos)
    {
        return gridCollider.ClosestPointOnBounds(worldPos);
    }

    private int CalculateTotalCellsRequired()
    {
        return GetVerticalCellsOccupied() * CombatFunctions.GetHorizontalCellsOccupied(gridCollider);
    }

    public Health Health()
    {
        return myHealth;
    }

    public bool IsObject()
    {
        return myHealth.IsObject();
    }

    //SETTERS
    public void ShowSelectionVisual(bool show)
    {
        Health().ActivateHealthVisual(show);
    }
}
