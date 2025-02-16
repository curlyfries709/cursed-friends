using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;

public class FantasySceneData : SceneData
{
    [Title("Grid")]
    public int gridWidth = 100;
    public int gridLength = 150;
    [Space(10)]
    public float gridVisualVerticalOffset = 0.001f;
    [Title("Fantasy Scene Data")]
    public bool enemyHostilesOnStart = true;
    [ReadOnly] [SerializeField] bool isEnemyHostileCurrent;
    public Transform victoryTransform;
    [Title("Terrain")]
    [SerializeField] Terrain sceneTerrain;
    [SerializeField] LayerMask terrainLayerMask;
    [Space(10)]
    [SerializeField] float highestWalkableTerrainHeight = 8f;

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

    public Terrain GetTerrain()
    {
        return sceneTerrain;
    }

    public Collider GetTerrainCollider()
    {
        return terrainCollider;
    }

    public LayerMask GetTerrainLayerMask()
    {
        return terrainLayerMask;
    }
}
