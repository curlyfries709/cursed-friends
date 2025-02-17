using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Sirenix.OdinInspector;
using System.Linq;
using Pathfinding.Graphs.Grid;

public class GridSystemVisual : MonoBehaviour
{
    public static GridSystemVisual Instance { get; private set; }

    [Header("Values")]
    [SerializeField] int visualsToSpawn = 50;
    [Title("Prefab")]
    [SerializeField] GameObject gridSystemVisualSinglePrefab;
    [SerializeField] Transform gridUIHeader;
    [Title("Materials")]
    [SerializeField] Material movementVisualMat;
    [SerializeField] Material selectedAreaVisualMat;
    [SerializeField] Material validTargetAreaVisualMat;
    [SerializeField] Material invalidActionAreaMat;
    [Title("TEST")]
    [SerializeField] bool showGridPosText = true;

    //Grid System
    private GridSystem<GridObject> gridSystem;

    //Arrays
    private CellVisualData[,] gridSystemVisualDataArray;

    //Storage
    List<GridPosition> currentValidMovementGridPos = new List<GridPosition>();
    List<GridPosition> currentValidTargetArea = new List<GridPosition>();
    List<GridPosition> currentSelectedArea = new List<GridPosition>();

    List<CellVisualData> spawnedGridVisuals = new List<CellVisualData>();
    List<CellVisualData> activeGridVisuals = new List<CellVisualData>();

    Dictionary<GridPosition, Material> movementMatDict = new Dictionary<GridPosition, Material>();
    public class CellVisualData
    {
        public GameObject cellGO;
        public MeshRenderer renderer;
        public Collider collider;
        public Material material;
        public TextMeshPro tmp;

        public GridPosition gridPosition; 
        public bool inUse = false;
    }

    private void Awake()
    {
        Instance = this;
    }

    public void Setup()
    {
        gridSystemVisualDataArray = new CellVisualData[LevelGrid.Instance.GetWidth(), LevelGrid.Instance.GetLength()];
        gridSystem = LevelGrid.Instance.gridSystem;

        CreateGridVisual();
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
        cellVisual.material = cellVisual.renderer.material;

        cellVisual.tmp = singleGridVisual.GetComponentInChildren<TextMeshPro>();

        spawnedGridVisuals.Add(cellVisual);

        return cellVisual;
    }



    public void ShowValidMovementGridPositions(List<GridPosition> validGridPositions, CharacterGridUnit selectedUnit, bool isPlayer)
    {
        currentValidMovementGridPos.Clear();
        movementMatDict.Clear();

        HideAllGridVisuals(true);

        foreach(GridPosition validGridPosition in validGridPositions)
        {
            Material moveMatToUse = isPlayer ? movementVisualMat : selectedAreaVisualMat;

            if (gridSystem.GetGridObject(validGridPosition).IsOccupiedByAnyUnit() && selectedUnit != gridSystem.GetGridObject(validGridPosition).GetGridUnit())
            {
                moveMatToUse = invalidActionAreaMat;
            }

            movementMatDict[validGridPosition] = moveMatToUse;

            ActivateCellVisualAtPos(validGridPosition, moveMatToUse, true);
            currentValidMovementGridPos.Add(validGridPosition);
        }
    }

    public void ShowValidTargetAndSelectedGridPositions(List<GridPosition> targetArea, List<GridPosition> selectedGridPositions, List<GridUnit> selectedUnits, bool show)
    {
        ShowHighlightedGridPos(targetArea, currentValidTargetArea, validTargetAreaVisualMat, true, show);
        currentValidTargetArea = targetArea;
        ShowOnlySelectedGridPositions(selectedGridPositions, selectedUnits, show, false);

    }

    public void ShowOnlySelectedGridPositions(List<GridPosition> selectedGridPositions, List<GridUnit> selectedUnits, bool show, bool showMovement = true)
    {
        ShowHighlightedGridPos(selectedGridPositions, currentSelectedArea, selectedAreaVisualMat, showMovement, show);
        currentSelectedArea = selectedGridPositions;

        HUDManager.Instance.UpdateTurnOrderNames(show ? selectedUnits : new List<GridUnit>());

        //Also Need To Highlight The selected Units
        foreach(GridUnit unit in LevelGrid.Instance.GetAllActiveGridUnits())
        {
            unit.ShowSelectionVisual(selectedUnits.Contains(unit) && show);
        }
    }

    private void ShowHighlightedGridPos(List<GridPosition> highlightedGridPositions, List<GridPosition> positionsToHide, Material material, bool showMovement, bool show)
    {
        HideListedGridVisual(positionsToHide);

        if (showMovement || !show)
        {
            ShowMovementVisualDuringSelection(highlightedGridPositions, !show);
        }

        foreach (GridPosition validGridPosition in highlightedGridPositions)
        {
            if (show)
            {
                ActivateCellVisualAtPos(validGridPosition, material, currentValidMovementGridPos.Contains(validGridPosition));
            }
            else
            {
                if (!currentValidMovementGridPos.Contains(validGridPosition))
                {
                    DeactivateCellVisual(GetVisualDataAtGridPosition(validGridPosition));
                }
            }
        }
    }

    private void ShowMovementVisualDuringSelection(List<GridPosition> highlightedGridPositions, bool hidingSelection)
    {
        foreach (GridPosition movePos in currentValidMovementGridPos)
        {
            if (!highlightedGridPositions.Contains(movePos) || hidingSelection)
            {
                ActivateCellVisualAtPos(movePos, movementMatDict[movePos], true);
            }
        }
    }

    private void ActivateCellVisualAtPos(GridPosition gridPosition, Material material, bool enableVisualCollider)
    {
        //Don't Bother if Cell Already in use at position
        if (activeGridVisuals.Any((visual) => visual.inUse && visual.gridPosition == gridPosition)){ return; }

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

        //Set Visuals & Data
        cellVisual.renderer.material = material;

        gridSystemVisualDataArray[gridPosition.x, gridPosition.z] = cellVisual;
        cellVisual.gridPosition = gridPosition;
        cellVisual.material = material;

        //Set Collider
        cellVisual.collider.enabled = enableVisualCollider;

        //Set Position
        Vector3 worldPos = gridSystem.GetWorldPosition(gridPosition);
        singleGridVisual.transform.position = worldPos;

        //Set Rotation
        //singleGridVisual.transform.rotation = GetGridVisualRotation(gridPosition);
        //singleGridVisual.transform.rotation = Quaternion.Euler(new Vector3(singleGridVisual.transform.rotation.eulerAngles.x, 0, singleGridVisual.transform.rotation.eulerAngles.z));

        //Activate
        cellVisual.inUse = true;
        singleGridVisual.SetActive(true);

        if (!activeGridVisuals.Contains(cellVisual))
            activeGridVisuals.Add(cellVisual);
    }

    public void HideListedGridVisual(List<GridPosition> list)
    {
        foreach(GridPosition pos in list)
        {
            CellVisualData foundCell = GetVisualDataAtGridPosition(pos);
            DeactivateCellVisual(foundCell);
        }
    }

    public void HideAllGridVisuals(bool hideUnitSelection)
    {
        //Hide Grid Visual
        for (int i = activeGridVisuals.Count - 1; i >= 0; i--)
        {
            DeactivateCellVisual(activeGridVisuals[i]);
        }

        //Hide Selected Unit Visuals
        if (hideUnitSelection)
        {
            foreach (GridUnit unit in LevelGrid.Instance.GetAllActiveGridUnits())
            {
                unit.ShowSelectionVisual(false);
            }
        } 
    }

    private void DeactivateCellVisual(CellVisualData cellVisual)
    {
        if(cellVisual == null){ return; }

        gridSystemVisualDataArray[cellVisual.gridPosition.x, cellVisual.gridPosition.z] = null;
        cellVisual.cellGO.SetActive(false);
        cellVisual.inUse = false;

        activeGridVisuals.Remove(cellVisual);
    }

    /* public Transform GetGridVisualTransformAtGridPosition(GridPosition gridPosition)
     {
         return gridSystemVisualObjectArray[gridPosition.x, gridPosition.z].transform;
     }*/

    public GameObject DebugShowVisualAtPosition(GridPosition gridPosition)
    {
        Material moveMatToUse = movementVisualMat;
        ActivateCellVisualAtPos(gridPosition, moveMatToUse, true);

        return gridSystemVisualDataArray[gridPosition.x, gridPosition.z].cellGO;
    }

    public CellVisualData GetVisualDataAtGridPosition(GridPosition gridPosition)
    {
        return gridSystemVisualDataArray[gridPosition.x, gridPosition.z];
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
}
