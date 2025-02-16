using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* 
 The Scene Core GameObject is only required for scenes that are not loaded additively.
 */
public class PersistableObjectSpawner : MonoBehaviour
{
    public static PersistableObjectSpawner MainInstance;

    [Header("Scene Core Data")]
    [Tooltip("System applies to scenes outside the game such as the title screen")]
    [SerializeField] SceneType mySceneType;
    [SerializeField] Transform playerStartTransform;
    [Header("Objects To Spawn")]
    [SerializeField] GameObject systemPersistablesPrefab;
    [SerializeField] GameObject modernWorldOnlyPersistablePrefab;
    [SerializeField] GameObject fantasyWorldOnlyPersistablePrefab;
    [SerializeField] GameObject multiWorldPersistablePrefab;

    //SPAWNED PERSISTABLES
    GameObject spawnedSystemPersistables = null;
    GameObject spawnedModernWorldOnlyPersistable = null;
    GameObject spawnedFantasyWorldOnlyPersistable= null;
    GameObject spawnedMultiWorldPersistable = null;

    //At scene should have at most TWO PersistableObjectSpawner classes, the Main Instance (From Title Screen) and the loaded scene's instance.
    PersistableObjectSpawner activeSceneCore; 
    public enum SceneType
    {
        Fantasy,
        Modern, 
        System,
        MultiWorld
    }

    private void Awake()
    {
        if (!MainInstance)
        {
            MainInstance = this;
            DontDestroyOnLoad(gameObject);
        }

        MainInstance.SetScenePersistables(mySceneType, this);
    }


    public void SetScenePersistables(SceneType currentSceneType, PersistableObjectSpawner currentSceneSpawnerInstance)
    {
        activeSceneCore = currentSceneSpawnerInstance;

        //Always Try to Spawn System.
        TrySpawnPersistable(SceneType.System);

        if (currentSceneType == SceneType.System)
        {
            TryDestroyPersistable(SceneType.Fantasy);
            TryDestroyPersistable(SceneType.MultiWorld);
            TryDestroyPersistable(SceneType.Modern);
        }
        else if (currentSceneType == SceneType.Modern)
        {
            //Spawn
            TrySpawnPersistable(SceneType.Modern);
            TrySpawnPersistable(SceneType.MultiWorld);

            //Destroy;
            TryDestroyPersistable(SceneType.Fantasy);
        }
        else if (currentSceneType == SceneType.Fantasy)
        {
            //Spawn
            TrySpawnPersistable(SceneType.Fantasy);
            TrySpawnPersistable(SceneType.MultiWorld);

            //Destroy
            TryDestroyPersistable(SceneType.Modern);
        }

        //Spawn New Player
        //if (PlayerSpawnerManager.Instance)
            //PlayerSpawnerManager.Instance.OnNewSceneLoaded();

        //Set Party Data
        //if (PartyData.Instance)
            //PartyData.Instance.SetParty();
    }

    private void TrySpawnPersistable(SceneType sceneType)
    {
        //Get the persistable prefab.
        GameObject prefab = GetPersistablePrefabBySceneType(sceneType);

        if (!GetSpawnedPersistableRefBySceneType(sceneType))
        {
            //Spawn it and child it to this gameobject.
            GetSpawnedPersistableRefBySceneType(sceneType) = Instantiate(prefab, transform);
        }
    }

    private void TryDestroyPersistable(SceneType sceneType)
    {
        //Get The Spawned prefab.
        if (GetSpawnedPersistableRefBySceneType(sceneType))
        {
            //Destroy and set spawned to null. 
            Destroy(GetSpawnedPersistableRefBySceneType(sceneType));
            GetSpawnedPersistableRefBySceneType(sceneType) = null;
        }
    }

    private ref GameObject GetSpawnedPersistableRefBySceneType(SceneType sceneType)
    {
        switch (sceneType)
        {
            case SceneType.Fantasy:
                return ref spawnedFantasyWorldOnlyPersistable;
            case SceneType.Modern:
                return ref spawnedModernWorldOnlyPersistable;
            case SceneType.System:
                return ref spawnedSystemPersistables;
            case SceneType.MultiWorld:
                return ref spawnedMultiWorldPersistable;
            default:
                return ref spawnedSystemPersistables;
        }
    }

    private GameObject GetPersistablePrefabBySceneType(SceneType sceneType)
    {
        switch (sceneType)
        {
            case SceneType.Fantasy:
                return fantasyWorldOnlyPersistablePrefab;
            case SceneType.Modern:
                return modernWorldOnlyPersistablePrefab;
            case SceneType.System:
                return systemPersistablesPrefab;
            case SceneType.MultiWorld:
                return multiWorldPersistablePrefab;
            default:
                return null;
        }
    }

    //GETTERS
    public SceneType GetSceneType()
    {
        if(IsMainInstance())
        {
            return activeSceneCore == this ? mySceneType : activeSceneCore.GetSceneType();
        }

        return mySceneType;
    }

    public Transform GetPlayerStartTransform()
    {
        if (IsMainInstance())
        {
            return activeSceneCore == this ? playerStartTransform: activeSceneCore.GetPlayerStartTransform();
        }

        return playerStartTransform;
    }

    private bool IsMainInstance()
    {
        return MainInstance == this;
    }
}
