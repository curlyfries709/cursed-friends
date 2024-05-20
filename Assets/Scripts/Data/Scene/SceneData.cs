using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;

public class SceneData : MonoBehaviour
{
    [Title("Grid")]
    public int gridWidth = 100;
    public int gridLength = 150;
    [Space(10)]
    public float gridVisualVerticalOffset = 0.001f;
    [Title("Scene Data")]
    public string sceneName;
    [Space(10)]
    public bool enemyHostilesOnStart = true;
    [ReadOnly] [SerializeField] bool isEnemyHostileCurrent;
    public Transform victoryTransform;
    [Space(10)]
    public AudioClip sceneMusic;
    [Title("Terrain")]
    [SerializeField] Terrain sceneTerrain;
    [SerializeField] LayerMask terrainLayerMask;
    [Space(10)]
    [SerializeField] float highestWalkableTerrainHeight = 8f;
    [Title("BAKE")]
    [SerializeField] SceneObstacleData obstacleData;
    [Tooltip("This will overwrite the current Obsctacle data on start when you next enter player mode. Only works in Unity Editor")]
    [SerializeField] bool bakeSceneObstacleData;
    [ShowIf("bakeSceneObstacleData")]
    [SerializeField] Transform bakeStartPoint;

    //Cache
    TerrainCollider terrainCollider;
    List<EnemyStateMachine> allEnemiesInScene;

    bool sceneHostilityAlteredByExternalEvent = false;

    private void Awake()
    {
        terrainCollider = sceneTerrain.GetComponent<TerrainCollider>();
        allEnemiesInScene = FindObjectsOfType<EnemyStateMachine>().ToList();
        isEnemyHostileCurrent = enemyHostilesOnStart;
    }

    private void Start()
    {
#if UNITY_EDITOR

        if (bakeSceneObstacleData)
        {
            obstacleData.Setup(gridWidth, gridLength, LevelGrid.Instance.GetCellSize());
            PathFinding.Instance.BakeNonWalkableNodes(obstacleData, bakeStartPoint, highestWalkableTerrainHeight);
            //obstacleData.SetDirty();
        }
        else
        {
            //Debug.Log("SHOWING BAKED WALKABLE NODES");
            //PathFinding.Instance.ShowAllWalkableNodes();
        }

#endif
        if(!sceneHostilityAlteredByExternalEvent)
            SetEnemiesAsHostile(enemyHostilesOnStart);
    }

    public void SetEnemiesAsHostile(bool isHostile)
    {
        isEnemyHostileCurrent = isHostile;
        sceneHostilityAlteredByExternalEvent = true;

        foreach (EnemyStateMachine enemy in allEnemiesInScene)
        {
            if (enemy.enemyGroup && enemy.enemyGroup.allowSceneDataToAlterHostility)
                enemy.isHostile = isEnemyHostileCurrent;
        }
    }

    public void BakeData()
    {
        PathFinding.Instance.BakeNonWalkableNodes(obstacleData, bakeStartPoint, highestWalkableTerrainHeight);
    }

    public Terrain GetTerrain()
    {
        return sceneTerrain;
    }

    public Collider GetTerrainCollider()
    {
        return terrainCollider;
    }

    public SceneObstacleData GetObstacleData()
    {
        return obstacleData;
    }

    public LayerMask GetTerrainLayerMask()
    {
        return terrainLayerMask;
    }
}
