using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.AI;
using System.Linq;

public class PathFinding : MonoBehaviour
{
    public static PathFinding Instance { get; private set; }

    [Header("Costs")]
    [SerializeField] private const int MOVE_STRAIGHT_COST= 10;
    [SerializeField] private const int MOVE_DIAGONAL_COST = 14;
    [Header("Obstacle detection")]
    [TagField]
    [SerializeField] string hollowObstacleTag;
    [TagField]
    [SerializeField] string filledObstacleTag;
    [Header("Obstacle Detection Values")]
    [SerializeField] private float obstacleDetectionOffsetDistance = 3f;
    [SerializeField] private float obstacleDetectionLength = 10f;
    [SerializeField] float boxCastRadius = 0.75f;
    [Space(10)]
    [SerializeField] float terrainCornerCastRadius = 0.8f;
    [SerializeField] private float terrainHeightCheckDiscrepancy = 0.75f;
    [Range(0.25f, 0.5f)]
    [SerializeField] float gridVisualAverageHeightIncrement = 0.25f;
    [Header("Layers")]
    [SerializeField] private LayerMask obstacleLayerMask;
    [SerializeField] LayerMask terrainLayerMask;
    [Header("Movement Behaviour")]
    [SerializeField] private bool includeDiagonalMovement = true;
    [SerializeField] bool canMoveThroughOccupiedPosition = false;


    private int width;
    private int length;
    private float cellSize;

    private SceneObstacleData sceneObstacleData;
    private float startTime;

    //Cache
    private GridSystem<PathNode> gridSystem;
    Dictionary<BoxCollider, List<GridPosition>> setDynamicObstacleData = new Dictionary<BoxCollider, List<GridPosition>>();

    private void Awake()
    {
        Instance = this;
    }

    public void Setup(int width, int length, float cellSize)
    {
        this.width = width;
        this.length = length;
        this.cellSize = cellSize;

        sceneObstacleData = LevelGrid.Instance.currentSceneData.GetObstacleData();
        gridSystem = new GridSystem<PathNode>(width, length, cellSize, (GridSystem<PathNode> g, GridPosition gridPosition) => new PathNode(gridPosition));
    }


    public void BakeNonWalkableNodes(SceneObstacleData obstacleData, Transform bakeStartPoint, float highestWalkableHeight)
    {
        Vector3 closestWalkablePos = bakeStartPoint.position;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < length; z++)
            {
                GridPosition gridPosition = new GridPosition(x, z);
                PathNode node = GetNode(x, z);
                Vector3 worldPos = LevelGrid.Instance.gridSystem.GetWorldPosition(gridPosition);

                Vector3 boxcastHalfExtents = new Vector3(boxCastRadius, boxCastRadius, boxCastRadius);
                Quaternion boxOrientation = Quaternion.LookRotation(Vector3.up);

                RaycastHit[] boxcastHits = Physics.BoxCastAll(worldPos + Vector3.down * obstacleDetectionOffsetDistance, boxcastHalfExtents, Vector3.up, boxOrientation, obstacleDetectionLength, obstacleLayerMask, QueryTriggerInteraction.Collide);

                if (boxcastHits.Length > 0)
                {
                    //Means there's an obstacle
                    //obstacleData.SetNodeIsWalkable(gridPosition, false);
                    node.SetIsWalkable(false);

                    //Set Obstacle Data
                    GridObject gridObject = LevelGrid.Instance.GetGridObjectAtPosition(gridPosition);
                    gridObject.SetObstacle(boxcastHits[0].collider);

                    continue;
                }

                //Must check if all four corners are walkable
                bool isWalkable = true;
                float checkCellRadius = terrainCornerCastRadius;
                float checkCellSize = checkCellRadius * 2;

                Vector3 newWalkablePos = closestWalkablePos;
                float prevCornerHeight = -200;


                for (float xCorner = worldPos.x - checkCellRadius; xCorner <= worldPos.x + checkCellRadius; xCorner = xCorner + checkCellSize)
                {
                    for (float yCorner = worldPos.z - checkCellRadius; yCorner <= worldPos.z + checkCellRadius; yCorner = yCorner + checkCellSize)
                    {
                        Vector3 cellCornerPos = new Vector3(xCorner, 0, yCorner);

                        //Check if walkable on navmesh.
                        NavMeshPath path = new NavMeshPath();
                        bool canSampleHeight = NavMesh.SamplePosition(cellCornerPos, out NavMeshHit hit, highestWalkableHeight, NavMesh.AllAreas);
                        Vector3 newCellCornerPos = new Vector3(cellCornerPos.x, hit.position.y, cellCornerPos.z);

                        bool foundPath = canSampleHeight && NavMesh.CalculatePath(closestWalkablePos, newCellCornerPos, NavMesh.AllAreas, path);
                        bool passedHeightCheck = prevCornerHeight == -200 || Mathf.Abs(newCellCornerPos.y - prevCornerHeight) <= terrainHeightCheckDiscrepancy;

                        if (!foundPath || path.status == NavMeshPathStatus.PathPartial || !passedHeightCheck)
                        {
                            isWalkable = false;
                            node.SetIsWalkable(false);
                            //obstacleData.SetNodeIsWalkable(gridPosition, false);

                            //Set Obstacle Data
                            GridObject gridObject = LevelGrid.Instance.GetGridObjectAtPosition(gridPosition);
                            gridObject.SetObstacle(LevelGrid.Instance.currentSceneData.GetTerrainCollider());

                            break;
                        }
                        else
                        {
                            prevCornerHeight = newCellCornerPos.y;
                            newWalkablePos = newCellCornerPos;
                        }
                    }

                    if (!isWalkable)
                        break;
                }

                if (isWalkable)
                {
                    List<Vector3> castPositions = new List<Vector3>();
                    NavMesh.SamplePosition(worldPos, out NavMeshHit centrePos, highestWalkableHeight, NavMesh.AllAreas);

                    float averageHeight = 0;
                    float centreHeight = centrePos.position.y;

                    for (float i = 0; i <= cellSize; i = i + gridVisualAverageHeightIncrement)
                    {
                        Vector3 castPos = new Vector3(worldPos.x, 0, ((worldPos.z - (cellSize * 0.5f)) + i));

                        NavMesh.SamplePosition(castPos, out NavMeshHit hitPos, highestWalkableHeight, NavMesh.AllAreas);
                        averageHeight = averageHeight + hitPos.position.y;
                        castPositions.Add(hitPos.position);
                    }

                    averageHeight = averageHeight / castPositions.Count;

                    Vector3 startPos = castPositions.First();
                    Vector3 endPos = castPositions.Last();
                    
                    Quaternion rotation = Quaternion.identity;
                    Vector3 direction = (endPos - startPos).normalized;
                    //rotation = Quaternion.FromToRotation(Vector3.forward, direction);
                    
                    if (averageHeight > 0.15f)
                    {
                        rotation = Quaternion.LookRotation(direction);

                       /* Debug.Log("Grid Position: " + gridPosition);
                        Debug.Log("Heigt Center: " + navmeshCentreHit.position.y);
                        Debug.Log("Heigt Start: " + navmeshBackHit.position.y);
                        Debug.Log("Heigt End: " + navmeshFrontHit.position.y);
                        Debug.Log("Average Height: " + averageHeight);*/
                    }
                    else
                    {
                        averageHeight = 0 + LevelGrid.Instance.currentSceneData.gridVisualVerticalOffset;
                    }

                    node.SetGridVisualTransform(averageHeight + LevelGrid.Instance.currentSceneData.gridVisualVerticalOffset, centreHeight, rotation);
                    //obstacleData.SetNodeVisualData(gridPosition, averageHeight + LevelGrid.Instance.currentSceneData.gridVisualVerticalOffset, rotation);
                    closestWalkablePos = newWalkablePos;
                }   
            }
        }

        //ShowAllWalkableNodes();
    }

    public void ShowAllWalkableNodes()
    {
        //SHOW ALL NODES
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < length; z++)
            {
                GridPosition gridPosition = new GridPosition(x, z);

                if (IsWalkable(gridPosition))
                {
                    PathNode node = GetNode(gridPosition.x, gridPosition.z);

                    GameObject visual = GridSystemVisual.Instance.GetGridVisualAtGridPosition(gridPosition);
                    visual.transform.position = new Vector3(visual.transform.position.x, node.GetVisualYPosition(), visual.transform.position.z);
                    visual.transform.rotation = node.GetRotation();
                    visual.transform.rotation = Quaternion.Euler(new Vector3(visual.transform.rotation.eulerAngles.x, 0, visual.transform.rotation.eulerAngles.z));

                    visual.SetActive(true);
                }
            }
        }
    }

    protected Quaternion AlignToSurface(Vector3 hitNormal)
    {
        //Up is just the normal
        Vector3 up = hitNormal;
        //Make sure the velocity is normalized
        Vector3 vel = Vector3.forward;
        //Project the two vectors using the dot product
        Vector3 forward = vel - up * Vector3.Dot(vel, up);

        //Set the rotation with relative forward and up axes
        Quaternion targetRot = Quaternion.LookRotation(forward.normalized, up);

        return targetRot;
    }

    public void SetDynamicObstacle(BoxCollider obstacleCollider, Collider modelCollider, bool set) //If set false, obstacle will be removed.
    {
        if (set)
        {
            int minX = LevelGrid.Instance.GetColliderBoundMinInGridPos(obstacleCollider).x;
            int maxX = LevelGrid.Instance.GetColliderBoundMaxInGridPos(obstacleCollider).x;

            int minZ = LevelGrid.Instance.GetColliderBoundMinInGridPos(obstacleCollider).z;
            int maxZ = LevelGrid.Instance.GetColliderBoundMaxInGridPos(obstacleCollider).z;

            //Create in dictionary
            setDynamicObstacleData[obstacleCollider] = new List<GridPosition>();

            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    GridPosition gridPosition = new GridPosition(x, z);

                    //Set Data
                    //Debug.Log(obstacleCollider.name + " Setting Node" + gridPosition.ToString() + " as Not Walkable");
                    GridObject gridObject = LevelGrid.Instance.GetGridObjectAtPosition(gridPosition);

                    if (gridObject.SetObstacle(modelCollider))
                    {
                        //Set is not walkable
                        GetNode(x, z).SetIsWalkable(!set);

                        //Add To Dict
                        setDynamicObstacleData[obstacleCollider].Add(gridPosition);
                    }
                }
            }
        }
        else
        {
            //Remove Obstacle From Grid Object
            foreach (GridPosition gridPosition in setDynamicObstacleData[obstacleCollider])
            {
                //Set Data
                //Debug.Log(obstacleCollider.name + "Setting Node" + gridPosition.ToString() + " as Walkable");

                GridObject gridObject = LevelGrid.Instance.GetGridObjectAtPosition(gridPosition);
                if (gridObject.RemoveObstacle(modelCollider))
                {
                    //Set is not walkable
                    GetNode(gridPosition.x, gridPosition.z).SetIsWalkable(!set);
                }
            }

            //Remove From Dictionary
            setDynamicObstacleData.Remove(obstacleCollider);
        }
    }

    public List<GridPosition> FindPath(GridPosition startGridPosition, GridPosition endGridPosition, CharacterGridUnit unitFollowingPath, out int pathLength, bool includeDiagonals)
    {
        List<PathNode> openList = new List<PathNode>();
        List<PathNode> closedList = new List<PathNode>();

        PathNode startNode = gridSystem.GetGridObject(startGridPosition);
        PathNode endNode = gridSystem.GetGridObject(endGridPosition);

        openList.Add(startNode);

        //Resetting all path nodes
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < length; z++)
            {
                GridPosition gridPosition = new GridPosition(x, z);
                PathNode pathNode = gridSystem.GetGridObject(gridPosition);

                pathNode.SetGCost(int.MaxValue);
                pathNode.SetHCost(0);
                pathNode.CalculateFCost();
                pathNode.ResetPreviousPathNode();
            }
        }

        startNode.SetGCost(0);
        startNode.SetHCost(CalculateDistance(startGridPosition, endGridPosition));
        startNode.CalculateFCost();

        while (openList.Count > 0)
        {
            PathNode currentNode = GetLowestFCostPathNode(openList);

            if (currentNode == endNode)
            {
                //reached Final node
                pathLength = endNode.GetFCost();
                return CalculatePath(endNode);
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);

            foreach (PathNode neighbourNode in GetNeighbourList(currentNode, includeDiagonals))
            {
                if (closedList.Contains(neighbourNode))
                {
                    continue;
                }

                if (!IsWalkable(neighbourNode.GetGridPosition()) || (IsMovementGridPositionOccupiedByAnotherUnit(neighbourNode.GetGridPosition(), unitFollowingPath) && neighbourNode.GetGridPosition() != endGridPosition))
                {
                    closedList.Add(neighbourNode);
                    continue;
                }


                int tentativeGCost = currentNode.GetGCost() + CalculateDistance(currentNode.GetGridPosition(), neighbourNode.GetGridPosition());
                
                if(tentativeGCost < neighbourNode.GetGCost())
                {
                    neighbourNode.SetPreviousNode(currentNode);
                    neighbourNode.SetGCost(tentativeGCost);
                    neighbourNode.SetHCost(CalculateDistance(neighbourNode.GetGridPosition(), endGridPosition));
                    neighbourNode.CalculateFCost();

                    if (!openList.Contains(neighbourNode))
                    {
                        openList.Add(neighbourNode);
                    }
                }
            }
        }

        // No Path found
        pathLength = 0;
        return null;

    }



    private int CalculateDistance(GridPosition gridPositionA, GridPosition gridPositionB)
    {
        GridPosition gridPositionDistance = gridPositionA - gridPositionB;
        int xDistance = Mathf.Abs(gridPositionDistance.x);
        int zDistance = Mathf.Abs(gridPositionDistance.z);
        int remaining = Mathf.Abs(xDistance - zDistance);

        return MOVE_DIAGONAL_COST * Mathf.Min(xDistance, zDistance) + MOVE_STRAIGHT_COST * remaining;
    }

    private List<GridPosition> CalculatePath(PathNode endNode)
    {
        List<PathNode> pathNodeList = new List<PathNode>();
        pathNodeList.Add(endNode);
        PathNode currentNode = endNode;

        while (currentNode.GetPreviousNode() != null)
        {
            pathNodeList.Add(currentNode.GetPreviousNode());
            currentNode = currentNode.GetPreviousNode();
        }

        pathNodeList.Reverse();

        List<GridPosition> gridPositionList = new List<GridPosition>();

        foreach (PathNode pathNode in pathNodeList)
        {
            gridPositionList.Add(pathNode.GetGridPosition());
        }

        return gridPositionList;
    }

    private PathNode GetLowestFCostPathNode(List<PathNode> pathNodeList)
    {
        PathNode lowestFCostPathNode = pathNodeList[0];
        for (int i = 0; i < pathNodeList.Count; i++)
        {
            if (pathNodeList[i].GetFCost() < lowestFCostPathNode.GetFCost())
            {
                lowestFCostPathNode = pathNodeList[i];
            }
        }

        return lowestFCostPathNode;
    }

    public PathNode GetNode(int x, int z)
    {
        return gridSystem.GetGridObject(new GridPosition(x, z));
    }

    private List<PathNode> GetNeighbourList(PathNode currentNode, bool includeDiagonals)
    {
        List<PathNode> neighbourList = new List<PathNode>();

        GridPosition currentGridPosition = currentNode.GetGridPosition();

        if (currentGridPosition.z + 1 < length)
        {
            //North Neighbour
            neighbourList.Add(GetNode(currentGridPosition.x, currentGridPosition.z + 1));
        }

        if (currentGridPosition.x + 1 < width)
        {
            //East Neighbour
            neighbourList.Add(GetNode(currentGridPosition.x + 1, currentGridPosition.z));

            if (includeDiagonals && includeDiagonalMovement)
            {
                if (currentGridPosition.z - 1 >= 0)
                {
                    //South East Neighbour
                    neighbourList.Add(GetNode(currentGridPosition.x + 1, currentGridPosition.z - 1));
                }

                if (currentGridPosition.z + 1 < length)
                {
                    //North East Neighbour
                    neighbourList.Add(GetNode(currentGridPosition.x + 1, currentGridPosition.z + 1));
                }
            }
        }

        //West Neighbour
        if (currentGridPosition.x - 1 >= 0)
        {
            neighbourList.Add(GetNode(currentGridPosition.x - 1, currentGridPosition.z));

            if (includeDiagonals && includeDiagonalMovement)
            {
                if (currentGridPosition.z - 1 >= 0)
                {
                    //South West Neighbour
                    neighbourList.Add(GetNode(currentGridPosition.x - 1, currentGridPosition.z - 1));
                }

                if (currentGridPosition.z + 1 < length)
                {
                    //North West Neighbour
                    neighbourList.Add(GetNode(currentGridPosition.x - 1, currentGridPosition.z + 1));
                }
            }
        }

        if (currentGridPosition.z - 1 >= 0)
        {
            //South Neighbour
            neighbourList.Add(GetNode(currentGridPosition.x, currentGridPosition.z - 1));
        }

        return neighbourList;
    }

    public List<GridPosition> GetGridPositionWalkableNeighbours(GridPosition gridPosition, bool includeDiagonals)
    {
        List<GridPosition> listToReturn = new List<GridPosition>();
        PathNode currentNode = new PathNode(gridPosition);

        foreach (PathNode node in GetNeighbourList(currentNode, includeDiagonals))
        {
            if (IsWalkable(node.GetGridPosition()))
            {
                listToReturn.Add(node.GetGridPosition());
            }
            else
            {
                LevelGrid.Instance.TryGetObstacleAtPosition(node.GetGridPosition(), out Collider obstacleData);
            }
        }

        return listToReturn;
    }

    public bool IsMovementGridPositionOccupiedByAnotherUnit(GridPosition gridPosition, GridUnit unit)
    {
        return !canMoveThroughOccupiedPosition && LevelGrid.Instance.IsGridPositionOccupied(gridPosition, false) && LevelGrid.Instance.gridSystem.GetGridObject(gridPosition).GetGridUnit() != unit;
    }

    public bool IsWalkable(GridPosition gridPosition)
    {
        //PathNode obstacleDataNode = sceneObstacleData.GetNode(gridPosition.x, gridPosition.z);

        //If Static Obstacle return false.
        /*if (!obstacleDataNode.IsWalkable())
        {
            return obstacleDataNode.IsWalkable();
        }*/

        //Return if Obstacle set at runtime.
        return gridSystem.GetGridObject(gridPosition).IsWalkable();
    }

    public float GetGridPositionCentreHeight(GridPosition gridPosition)
    {
        return GetNode(gridPosition.x, gridPosition.z).GetHeightOfCentre();
    }

    public bool HasPath(GridPosition startGridPosition, GridPosition endGridPosition, CharacterGridUnit unitFollowingPath, bool includeDiagonals)
    {
        return FindPath(startGridPosition, endGridPosition, unitFollowingPath, out int pathLength, includeDiagonals) != null;
    }

    public int DistanceInGridUnits(GridPosition startGridPosition, GridPosition endGridPosition, CharacterGridUnit unitFollowingPath)
    {
        FindPath(startGridPosition, endGridPosition, unitFollowingPath, out int pathLength, false);
        return pathLength / GetPathFindingDistanceMultiplier();
    }

    public int ManhanttanDistance(GridPosition startGridPosition, GridPosition endGridPosition)
    {
        //More Peformantive distance calculation, doesn't include diagonals though.
        return Mathf.Abs(endGridPosition.x - startGridPosition.x) + Mathf.Abs(endGridPosition.z - startGridPosition.z);
    }

    public int GetPathLength(GridPosition startGridPosition, GridPosition endGridPosition, CharacterGridUnit unitFollowingPath, bool includeDiagonals)
    {
        FindPath(startGridPosition, endGridPosition, unitFollowingPath, out int pathLength, includeDiagonals);
        return pathLength;
    }

    public int GetPathFindingDistanceMultiplier()
    {
        return MOVE_STRAIGHT_COST;
    }

    public ObstacleType GetObstacleType(string obstacleType)
    {
        if (obstacleType == hollowObstacleTag)
        {
            return ObstacleType.Hollow;
        }

        return ObstacleType.Filled;
    }

}
