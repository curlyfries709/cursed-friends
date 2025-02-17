using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using UnityEngine.UIElements;

public class PathFinding : MonoBehaviour
{
    public static PathFinding Instance { get; private set; }


    CustomTraversalProvider customTraversalProvider = null;
    public class TerminatePathAtMaxDistance : ABPathEndingCondition
    {
        // Reuse the constructor in the superclass
        public TerminatePathAtMaxDistance(ABPath p, CharacterGridUnit traversingUnit) : base(p) 
        {
            this.traversingUnit = traversingUnit;
        }

        // Maximum world distance to the target node before terminating the path
        public int maxDistance = 5;
        private CharacterGridUnit traversingUnit;

        public override bool TargetFound(GraphNode node, uint H, uint G)
        {
            GridPosition gridPosition = LevelGrid.Instance.gridSystem.GetGridPosition((Vector3)node.position);

            return G >= CalculateTraversalCostFromDistance(maxDistance) &&
                !LevelGrid.Instance.IsGridPositionOccupiedByDifferentUnit(traversingUnit, gridPosition, true);
        }
    }

    private void Awake()
    {
        if (!Instance)
            Instance = this;
    }

    public void QueryStartToEndPath(GridPosition startGridPosition, GridPosition endGridPosition, CharacterGridUnit unitFollowingPath, OnPathDelegate onPathCompleteCallback)
    {
        Vector3 startPos = LevelGrid.Instance.gridSystem.GetWorldPosition(startGridPosition);
        Vector3 endPos = LevelGrid.Instance.gridSystem.GetWorldPosition(endGridPosition);

        ABPath path = ABPath.Construct(startPos,endPos, onPathCompleteCallback);
        path.Claim(this);

        path.traversalProvider = GetDefaultTraversalProvider(unitFollowingPath);

        // Calculate the path by using the AstarPath component directly
        AstarPath.StartPath(path, true);
    }

    public void QueryPathNodesWithinUnitMoveRange(CharacterGridUnit unitFollowingPath, OnPathDelegate onPathCompleteCallback)
    {
        GridPosition startGridPosition = unitFollowingPath.GetGridPositionsOnTurnStart()[0];
        int range = unitFollowingPath.MoveRange();

        QueryPathNodesWithinRange(startGridPosition, unitFollowingPath, range, onPathCompleteCallback);
    }

    public void QueryPathNodesWithinRange(GridPosition startGridPosition, CharacterGridUnit unitFollowingPath, int range, OnPathDelegate onPathCompleteCallback)
    {
        int calculatedDistance = CalculateTraversalCostFromDistance(range + 1); //Add padding to include start pos.
        Vector3 startPos = LevelGrid.Instance.gridSystem.GetWorldPosition(startGridPosition);

        ConstantPath constructPath = ConstantPath.Construct(startPos, calculatedDistance, onPathCompleteCallback);
        constructPath.Claim(this);
        constructPath.traversalProvider = GetDefaultTraversalProvider(unitFollowingPath);

        AstarPath.StartPath(constructPath);
    }

    public void QueryPartialPathToPoint(GridPosition startGridPosition, GridPosition targetGridPosition, CharacterGridUnit unitFollowingPath, int maxPathDistance, OnPathDelegate onPathCompleteCallback)
    {
        if(maxPathDistance <= 0)
        {
            Debug.Log("Max Distance is equal or less than 0. This is not valid. Please fix");
            return;
        }

        Vector3 startPos = LevelGrid.Instance.gridSystem.GetWorldPosition(startGridPosition);
        Vector3 endPos = LevelGrid.Instance.gridSystem.GetWorldPosition(targetGridPosition);

        ABPath path = ABPath.Construct(startPos, endPos, onPathCompleteCallback);
        path.Claim(this);
        path.calculatePartial = true;
 
        path.traversalProvider = GetDefaultTraversalProvider(unitFollowingPath);

        //Set Custom Ending Condition
        TerminatePathAtMaxDistance endingCondition = new TerminatePathAtMaxDistance(path, unitFollowingPath);
        endingCondition.maxDistance = maxPathDistance;
        path.endingCondition = endingCondition;

        AstarPath.StartPath(path, true);
    }

    public void QueryClosestNodeToDestination(List<GridPosition> positionsToCheck, GridPosition destination, CharacterGridUnit unitFollowingPath, OnPathDelegate onPathCompleteCallback)
    {
        Vector3 destinationWorldPos = LevelGrid.Instance.gridSystem.GetWorldPosition(destination);

        List<Vector3> startPositions = new List<Vector3>();

        foreach(GridPosition position in positionsToCheck)
        {
            startPositions.Add(LevelGrid.Instance.gridSystem.GetWorldPosition(position));
        }

        MultiTargetPath multiPath = MultiTargetPath.Construct(startPositions.ToArray(), destinationWorldPos, null, onPathCompleteCallback);
        multiPath.Claim(this);
        multiPath.traversalProvider = GetDefaultTraversalProvider(unitFollowingPath);
        multiPath.pathsForAll = false;

        AstarPath.StartPath(multiPath);
    }

    public List<GridPosition> GetGridPositionWalkableNeighbours(GridPosition gridPosition, bool includeDiagonals)
    {
        List<GridPosition> listToReturn = new List<GridPosition>();

        foreach (GridPosition neighbourGridPos in GetNeighbourList(gridPosition, includeDiagonals))
        {
            if (LevelGrid.Instance.IsWalkable(neighbourGridPos))
            {
                listToReturn.Add(neighbourGridPos);
            }
        }

        return listToReturn;
    }

    public List<GridPosition> GetGridPositionOccupiableNeighbours(GridPosition gridPosition, CharacterGridUnit unitLookingToOccupy, bool includeDiagonals)
    {
        List<GridPosition> listToReturn = new List<GridPosition>();

        foreach (GridPosition neighbourGridPos in GetNeighbourList(gridPosition, includeDiagonals))
        {
            if (LevelGrid.Instance.CanOccupyGridPosition(unitLookingToOccupy, neighbourGridPos))
            {
                listToReturn.Add(neighbourGridPos);
            }
        }

        return listToReturn;
    }

    private List<GridPosition> GetNeighbourList(GridPosition currentGridPosition, bool includeDiagonals)
    {
        List<GridPosition> neighbourList = new List<GridPosition>();

        int length = LevelGrid.Instance.GetLength();
        int width = LevelGrid.Instance.GetWidth();

        if (currentGridPosition.z + 1 < length)
        {
            //North Neighbour
            neighbourList.Add(new GridPosition(currentGridPosition.x, currentGridPosition.z + 1));
        }

        if (currentGridPosition.x + 1 < width)
        {
            //East Neighbour
            neighbourList.Add(new GridPosition(currentGridPosition.x + 1, currentGridPosition.z));

            if (includeDiagonals)
            {
                if (currentGridPosition.z - 1 >= 0)
                {
                    //South East Neighbour
                    neighbourList.Add(new GridPosition(currentGridPosition.x + 1, currentGridPosition.z - 1));
                }

                if (currentGridPosition.z + 1 < length)
                {
                    //North East Neighbour
                    neighbourList.Add(new GridPosition(currentGridPosition.x + 1, currentGridPosition.z + 1));
                }
            }
        }

        //West Neighbour
        if (currentGridPosition.x - 1 >= 0)
        {
            neighbourList.Add(new GridPosition(currentGridPosition.x - 1, currentGridPosition.z));

            if (includeDiagonals)
            {
                if (currentGridPosition.z - 1 >= 0)
                {
                    //South West Neighbour
                    neighbourList.Add(new GridPosition(currentGridPosition.x - 1, currentGridPosition.z - 1));
                }

                if (currentGridPosition.z + 1 < length)
                {
                    //North West Neighbour
                    neighbourList.Add(new GridPosition(currentGridPosition.x - 1, currentGridPosition.z + 1));
                }
            }
        }

        if (currentGridPosition.z - 1 >= 0)
        {
            //South Neighbour
            neighbourList.Add(new GridPosition(currentGridPosition.x, currentGridPosition.z - 1));
        }

        return neighbourList;
    }

    public static int CalculateTraversalCostFromDistance(int distanceInGridUnits)
    {
        return distanceInGridUnits * (int)AstarPath.active.data.gridGraph.nodeSize * Int3.Precision;
    }

    public List<GridPosition> GetGridPositionsFromPath(Path path)
    {
        List<GridPosition> gridPositions = new List<GridPosition>();

        if (path is ConstantPath constantPath)
        {
            foreach (GraphNode node in constantPath.allNodes)
            {
                GridPosition gridPosition = LevelGrid.Instance.gridSystem.GetGridPosition((Vector3)node.position);

                gridPositions.Add(gridPosition);
            }
        }
        else
        {
            foreach (Vector3 pos in path.vectorPath)
            {
                GridPosition gridPosition = LevelGrid.Instance.gridSystem.GetGridPosition(pos);

                gridPositions.Add(gridPosition);
            }
        }

        ReturnPathToPool(path);
        return gridPositions;
    }
    private ITraversalProvider GetDefaultTraversalProvider(CharacterGridUnit characterTraversingPath)
    {
        if (customTraversalProvider == null)
        {
            customTraversalProvider = new CustomTraversalProvider(characterTraversingPath);
            return customTraversalProvider;
        }

        customTraversalProvider.ResetWithNewCharacter(characterTraversingPath);
        return customTraversalProvider;
    }

    public int GetPathLengthInGridUnits(GridPosition startGridPosition, GridPosition endGridPosition, CharacterGridUnit unitFollowingPath)
    {
        Vector3 startPos = LevelGrid.Instance.gridSystem.GetWorldPosition(startGridPosition);
        Vector3 endPos = LevelGrid.Instance.gridSystem.GetWorldPosition(endGridPosition);

        ABPath path = ABPath.Construct(startPos, endPos, null);
        path.Claim(this);
        path.traversalProvider = GetDefaultTraversalProvider(unitFollowingPath);

        // Calculate the path by using the AstarPath component directly
        AstarPath.StartPath(path, true);

        //Call after path started to ensure path is complete when function returns. 
        path.BlockUntilCalculated();
        int pathLength = path.vectorPath.Count;

        Debug.Log("Current Implementation for this function is not ideal. Consider refactoring so path.BlockUntilCalculated isn't called");

        ReturnPathToPool(path);
        return pathLength;
    }

    public int ManhanttanDistance(GridPosition startGridPosition, GridPosition endGridPosition)
    {
        //More Peformantive distance calculation, doesn't include diagonals though.
        return Mathf.Abs(endGridPosition.x - startGridPosition.x) + Mathf.Abs(endGridPosition.z - startGridPosition.z);
    }

    public void ReturnPathToPool(Path path)
    {
        if (path != null)
            path.Release(this);
    }

    /* private void SetGraphNodeWalkability()
 {
     AstarPath.active.AddWorkItem(new AstarWorkItem(ctx => {
         var gg = AstarPath.active.data.gridGraph;
         int x = 38;
         int z = 24;
         var node = gg.GetNode(x, z);

         node.Walkable = false;

         // Recalculate all grid connections
         // This is required because we have updated the walkability of some nodes
         // gg.RecalculateAllConnections();

         // If you are only updating one or a few nodes you may want to use
         gg.CalculateConnectionsForCellAndNeighbours(x, z); //only on those nodes instead for performance.
     }));


 }*/

}
