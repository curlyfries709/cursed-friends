using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Sirenix.OdinInspector;
using System.Linq;
using System;

public class GridSystemVisual : MonoBehaviour
{
    public static GridSystemVisual Instance { get; private set; }

    [Header("Values")]
    [SerializeField] int visualsToSpawn = 50;
    [SerializeField] float objectAOEGridMultiplierScale = 0.75f;
    [Title("Prefab")]
    [SerializeField] GameObject gridSystemVisualSinglePrefab;
    [Space(10)]
    [SerializeField] Transform gridUIHeader;
    [Title("Materials")]
    [SerializeField] Material movementVisualMat;
    [SerializeField] Material invalidActionAreaMat;
    [Space(5)]
    [SerializeField] Material selectedAreaVisualMat;
    [SerializeField] Material validTargetAreaVisualMat;
    [Space(5)]
    [SerializeField] Material selectedObjectAOEMat;
    [Title("TEST")]
    [SerializeField] bool showGridPosText = true;

    //Grid System
    private GridSystem<GridObject> gridSystem;

    //Storage
    List<CellVisualData> spawnedGridVisuals = new List<CellVisualData>();
    Dictionary<GridPosition, CellVisualData> activeGridVisuals = new Dictionary<GridPosition, CellVisualData>();

    Dictionary<VisualType, List<CellVisualData>> activeGridVisualsOfType = new Dictionary<VisualType, List<CellVisualData>>();

    Dictionary<GridPosition, CellVisualData> activeObjectAOEVisuals = new Dictionary<GridPosition, CellVisualData>();

    public enum VisualType
    {
        Movement,
        Unoccupiable,
        SkillTargetArea,
        SkillAOE,
        ObjectAOE
    }

    public class CellVisualData
    {
        //Components
        public GameObject cellGO;
        public MeshRenderer renderer;
        public Collider collider;

        public TextMeshPro tmp;

        //DATA
        public GridPosition gridPosition;
        public VisualType currentVisualType;
        public IHighlightable currentHighlightable;

        public SortedList<int, VisualType> activeVisualTypes = new SortedList<int, VisualType>();

        public bool inUse = false;
    }

    private void Awake()
    {
        Instance = this;
    }

    public void Setup()
    {
        gridSystem = LevelGrid.Instance.gridSystem;
        CreateGridVisual();

        //Initialize lists
        for(int i = 0; i < Enum.GetNames(typeof(VisualType)).Length; ++i)
        {
            if((VisualType)i == VisualType.Unoccupiable) { continue; } //Unoccupiable will be grouped with movement. 

            activeGridVisualsOfType[(VisualType)i] = new List<CellVisualData>();
        }

    }

    public void CreateGridVisual()
    {
        if (gridUIHeader.childCount >= visualsToSpawn)
        {
            //Don't spawn if we have enough.
            return;
        }

        for(int i = gridUIHeader.childCount; i < visualsToSpawn; i++)
        {
            SpawnNewVisual();
        }
    }

    private CellVisualData SpawnNewVisual()
    {
        GameObject singleGridVisual = Instantiate(gridSystemVisualSinglePrefab, gridUIHeader);
        singleGridVisual.transform.localScale = Vector3.one * LevelGrid.Instance.GetCellSize();

        CellVisualData cellVisual = new CellVisualData();

        cellVisual.cellGO = singleGridVisual;
        cellVisual.renderer = singleGridVisual.GetComponentInChildren<MeshRenderer>();
        cellVisual.collider = singleGridVisual.GetComponentInChildren<Collider>();

        cellVisual.tmp = singleGridVisual.GetComponentInChildren<TextMeshPro>();

        spawnedGridVisuals.Add(cellVisual);

        return cellVisual;
    }

    public void ShowValidMovementGridPositions(List<GridPosition> validGridPositions, CharacterGridUnit movingUnit, bool isPlayer)
    {
        HideAllGridVisuals();

        foreach (GridPosition validGridPosition in validGridPositions)
        {
            VisualType moveVisualType = isPlayer ? VisualType.Movement : VisualType.SkillAOE;

            if (LevelGrid.Instance.IsGridPositionOccupiedByDifferentUnit(movingUnit, validGridPosition, true))
            {
                moveVisualType = VisualType.Unoccupiable;
            }

            ActivateCellVisualAtPos(validGridPosition, moveVisualType);
        }
    }

    public void ShowGridVisuals(PlayerBaseSkill currentPlayerSkill, List<GridPosition> validGridPositions, Dictionary<GridPosition, IHighlightable> gridPosHighlightableDict, VisualType visualType)
    {
        //Hide Grid Pos based on visual type.
        HideGridVisualsOfType(visualType, validGridPositions);

        foreach (KeyValuePair<GridPosition, IHighlightable> pair in gridPosHighlightableDict)
        {
            ShowGridVisualOfType(currentPlayerSkill, pair.Key, pair.Value, visualType);
        }
    }

    public void ShowGridVisuals(List<GridPosition> validGridPositions, VisualType visualType)
    {
        //Hide Grid Pos based on visual type.
        HideGridVisualsOfType(visualType, validGridPositions);

        foreach (GridPosition gridPosition in validGridPositions)
        {
            ShowGridVisualOfType(null, gridPosition, null, visualType);
        }
    }

    private void ShowGridVisualOfType(PlayerBaseSkill currentPlayerSkill, GridPosition gridPosition, IHighlightable highlightable, VisualType visualType)
    {
        //If the current visual type at pos is higher priority than passed visual type, move on
        //Prioties from low to high: Move/Invalid -> Valid Target Area -> Skill Selected Area -> Object AOE
        if (activeGridVisuals.ContainsKey(gridPosition) && activeGridVisuals[gridPosition].currentVisualType > visualType)
        {
            return;
        }

        CellVisualData cellVisualData = ActivateCellVisualAtPos(gridPosition, visualType);

        if (highlightable != null && highlightable.GetGridUnit() != FantasyCombatManager.Instance.GetActiveUnit())
        {
            cellVisualData.currentHighlightable = highlightable;
            cellVisualData.currentHighlightable?.ActivateHighlightedUI(true, currentPlayerSkill);
        }
    }

    public void HideGridVisualsOfType(VisualType visualType)
    {
        List<CellVisualData> currentCellsList = GetActiveListFromType(visualType);

        foreach (CellVisualData cellVisualData in currentCellsList)
        {
            DeactivateCellVisual(cellVisualData, !IsMovementType(visualType));
        }

        currentCellsList.Clear();
    }

    private void HideGridVisualsOfType(VisualType visualType, List<GridPosition> newGridPosList)
    {
        List<CellVisualData> currentCellsList = GetActiveListFromType(visualType);
        List<CellVisualData> listToHide = currentCellsList.Where((data) => !newGridPosList.Contains(data.gridPosition)).ToList();

        foreach (CellVisualData cellVisualData in listToHide)
        {
            DeactivateCellVisual(cellVisualData, !IsMovementType(visualType));
            currentCellsList.Remove(cellVisualData);
        }
    }

    public void HideAllGridVisuals()
    {
        //Hide Grid Visual
        for (int i = activeGridVisuals.Count - 1; i >= 0; i--)
        {
            KeyValuePair<GridPosition, CellVisualData> pair = activeGridVisuals.ElementAt(i);
            DeactivateCellVisual(pair.Value, false);
        }

        //Clear all lists since nothing is active
        foreach (KeyValuePair<VisualType, List<CellVisualData>> pair in activeGridVisualsOfType)
        {
            pair.Value.Clear();
        }
    }

    private CellVisualData ActivateCellVisualAtPos(GridPosition gridPosition, VisualType visualType)
    {
        CellVisualData foundCellVisualData;

        //Don't Bother or update if Cell already in use at position.
        if (visualType == VisualType.ObjectAOE)
        {
            foundCellVisualData = activeObjectAOEVisuals.ContainsKey(gridPosition) ? activeObjectAOEVisuals[gridPosition] : null;
        }
        else if (activeGridVisuals.ContainsKey(gridPosition))
        {
            foundCellVisualData = activeGridVisuals[gridPosition];
            VisualType currentVisualType = foundCellVisualData.currentVisualType;

            if (currentVisualType != visualType) //Upgrade the visual at the current pos
            {
                GetActiveListFromType(currentVisualType).Remove(foundCellVisualData);

                int currentVisualTypeIndex = (int)currentVisualType;
                foundCellVisualData.activeVisualTypes.Add(currentVisualTypeIndex, currentVisualType);

                SetVisualAppearanceByType(foundCellVisualData, visualType);
            }
        }
        else
        {
            foundCellVisualData = null;
        }

        
        if (foundCellVisualData != null)
        { 
            return foundCellVisualData; 
        }

        //Fetch an Unused GridPos
        CellVisualData cellVisual = spawnedGridVisuals.FirstOrDefault((visual) => !visual.inUse);

        if (cellVisual == null)
        {
            //If All are used, spawn new one.
            Debug.Log("Spawning New Grid To Pool");
            cellVisual = SpawnNewVisual();
        }

        GameObject singleGridVisual = cellVisual.cellGO;

        if (showGridPosText)
        {
            cellVisual.tmp.text = gridPosition.x + "," + gridPosition.z;
        }
        else
        {
            cellVisual.tmp.gameObject.SetActive(false);
        }

        //Set Visuals
        SetVisualAppearanceByType(cellVisual, visualType);

        //Set Position
        Vector3 worldPos = gridSystem.GetWorldPosition(gridPosition);
        singleGridVisual.transform.position = worldPos;

        //Set Rotation
        //singleGridVisual.transform.rotation = GetGridVisualRotation(gridPosition);
        //singleGridVisual.transform.rotation = Quaternion.Euler(new Vector3(singleGridVisual.transform.rotation.eulerAngles.x, 0, singleGridVisual.transform.rotation.eulerAngles.z));

        //Set Data
        cellVisual.gridPosition = gridPosition;
        cellVisual.inUse = true;

        //Activate
        singleGridVisual.SetActive(true);

        if(visualType == VisualType.ObjectAOE)
        {
            activeObjectAOEVisuals[gridPosition] = cellVisual;
        }
        else
        {
            activeGridVisuals[gridPosition] = cellVisual;
        }

        return cellVisual;
    }

    private void SetVisualAppearanceByType(CellVisualData cellVisual, VisualType visualType)
    {
        //Set Data
        cellVisual.currentVisualType = visualType;

        //Set Material
        Material material = GetGridMaterialFromType(visualType);
        cellVisual.renderer.material = material;

        //Set Collider
        cellVisual.collider.enabled = IsMovementType(visualType) ||
            cellVisual.activeVisualTypes.Any((pair) => pair.Key == (int)VisualType.Movement || pair.Key == (int)VisualType.Unoccupiable);

        //Set Scale
        cellVisual.cellGO.transform.localScale = Vector3.one
            * LevelGrid.Instance.GetCellSize() * (visualType == VisualType.ObjectAOE ? objectAOEGridMultiplierScale : 1);


        GetActiveListFromType(visualType).Add(cellVisual);
    }


    private void DeactivateCellVisual(CellVisualData cellVisual, bool canDowngrade)
    {
        if(cellVisual == null){ return; }

        int activeVisualTypeCount = cellVisual.activeVisualTypes.Count;
        bool isObjectAOEVisual = cellVisual.currentVisualType == VisualType.ObjectAOE;

        //Check if it can be downgraded. Do not downgrade Object AOE since it can be stacked upon another grid visual. 
        if (!canDowngrade)
        {
            //reset active types list
            cellVisual.activeVisualTypes.Clear();
        }
        else if (!isObjectAOEVisual && activeVisualTypeCount > 0)
        {
            //Get New Visual type
            int newVisualTypeIndex = activeVisualTypeCount - 1;
            VisualType newVisualType = cellVisual.activeVisualTypes.ElementAt(newVisualTypeIndex).Value;

            //remove from list 
            cellVisual.activeVisualTypes.RemoveAt(newVisualTypeIndex);

            if (!IsHighlightableType(newVisualType)) //Hide selection if non-highlightable type
            {
                cellVisual.currentHighlightable?.ActivateHighlightedUI(false, null);
            }

            //Update apperance
            SetVisualAppearanceByType(cellVisual, newVisualType);
            return;
        }

        //Cannot be downgraded. Entirely remove visual from grid Position
        cellVisual.currentHighlightable?.ActivateHighlightedUI(false, null);
        cellVisual.currentHighlightable = null;

        cellVisual.cellGO.SetActive(false);
        cellVisual.inUse = false;

        if (isObjectAOEVisual)
        {
            activeObjectAOEVisuals.Remove(cellVisual.gridPosition);
        }
        else
        {
            activeGridVisuals.Remove(cellVisual.gridPosition);
        }
    }

    public GameObject DebugShowVisualAtPosition(GridPosition gridPosition, VisualType visualType)
    {
        return ActivateCellVisualAtPos(gridPosition, visualType).cellGO;
    }

    public Quaternion GetGridVisualRotation(GridPosition gridPosition)
    {
        throw new System.NotImplementedException();

        /*GameObject cell = GridSystemVisual.Instance.DebugShowVisualAtPosition(gridSystem.GetGridPosition(testTransform.position));

        GraphCollision graphCollision = AstarPath.active.data.gridGraph.collision;
        Vector3 returnVal = graphCollision.CheckHeight(testTransform.position, out RaycastHit hit, out bool walkable);

        Debug.DrawRay(testTransform.position, hit.normal * 15, Color.red, 100);
        Debug.Log("Hit Normal " + hit.normal.ToString());
        //Debug.Log("Hit Collider" + hit.collider.name.ToString());
        Debug.Log("Hit Point: " + hit.point.ToString());
        Debug.Log("return Val: " + returnVal.ToString());
        cell.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
        //cell.transform.rotation = Quaternion.LookRotation(hit.normal);*/
    }

    public Material GetGridMaterialFromType(VisualType visualType)
    {
        switch(visualType)
        {
            case VisualType.SkillAOE:
                return selectedAreaVisualMat;
            case VisualType.SkillTargetArea:
                return validTargetAreaVisualMat;
            case VisualType.Movement:
                return movementVisualMat;
            case VisualType.Unoccupiable:
                return invalidActionAreaMat;
            case VisualType.ObjectAOE:
                return selectedObjectAOEMat;
            default:
                return movementVisualMat;
        }
    }

    protected bool IsMovementType(VisualType visualType)
    {
        return visualType == VisualType.Movement || visualType == VisualType.Unoccupiable;
    }

    protected bool IsHighlightableType(VisualType visualType)
    {
        return !(IsMovementType(visualType) || visualType == VisualType.SkillTargetArea);
    }

    protected List<CellVisualData> GetActiveListFromType(VisualType visualType)
    {
        if (IsMovementType(visualType))
        {
            return activeGridVisualsOfType[VisualType.Movement];
        }

        return activeGridVisualsOfType[visualType];
    }
}
