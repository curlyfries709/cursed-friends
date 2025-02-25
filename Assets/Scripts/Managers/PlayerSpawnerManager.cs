using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.Serialization;
using Unity.VisualScripting;
using UnityEngine;


public class PlayerSpawnerManager : MonoBehaviour, ISaveable
{
    public static PlayerSpawnerManager Instance { get; private set; }

    [Header("Player Prefabs")]
    [SerializeField] GameObject playerModernWorldPrefab;
    [SerializeField] GameObject playerFantasyWorldPrefab;
    [Header("Companion Prefabs")]
    [SerializeField] List<GameObject> fantasyCompanionPrefabs;
    [Header("Components")]
    [SerializeField] CompanionFollowBehaviour companionFollowBehaviour;
    [Header("TEST")]
    [SerializeField] bool spawnCompanions = true;

    bool isModernPlayerVersionSpawned = false;

    //CACHE
    GameObject spawnedPlayer = null;
    PlayerStateMachine playerStateMachine = null;

    List<GameObject> spawnedCompanions = new List<GameObject>();
    List<CompanionStateMachine> companionStateMachines = new List<CompanionStateMachine>();

    SceneData currentSceneData;

    //Events
    public Action SwapCompanionPositionsEvent;

    //Saving Data
    [SerializeField, HideInInspector]
    private PlayerState playerState = new PlayerState();

    bool isDataRestored = false;
    bool restoreCalled = false;
    bool resetVitals = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void OnEnable()
    {
        SavingLoadingManager.Instance.NewRealmEntered += OnNewRealmEntered;
    }

    public void OnNewSceneLoadedEarly(SceneData newSceneData)
    {
        currentSceneData = newSceneData;

        if (ShouldSpawnPlayer())
        {
            SpawnPlayer();
        }
        else
        {
            //Spawn the current player at player start position.
            TransportPlayer();
        }
    }

    public void OnNewRealmEntered(RealmType newRealmType)
    {
        if(spawnedPlayer && newRealmType == RealmType.Fantasy)
        {
            //When Entering Fantasy Realm, Reset Health. 
            resetVitals = true;
            RestoreCharacterData();
        }
    }

    private void SpawnPlayer()
    {
        GameObject prefabToSpawn;
 
        if(currentSceneData.GetRealmType() == RealmType.Fantasy)
        {
            isModernPlayerVersionSpawned = false;
            prefabToSpawn = playerFantasyWorldPrefab;
        }
        else if (currentSceneData.GetRealmType() == RealmType.Modern)
        {
            isModernPlayerVersionSpawned = true;
            prefabToSpawn = playerModernWorldPrefab;
        }
        else
        {
            return;
        }

        //Destroy the current spawned player GameObject. 
        if (spawnedPlayer)
        {
            playerStateMachine.PrepareForDestruction();
            Destroy(spawnedPlayer);

            //Destroy Companions
            foreach (GameObject prefab in spawnedCompanions)
            {
                Destroy(prefab);
            }

            companionStateMachines.Clear();
        }

        //The Parent GameObject must be spawned at 0, 0, 0.
        spawnedPlayer = Instantiate(prefabToSpawn, Vector3.zero, Quaternion.identity);
        playerStateMachine = spawnedPlayer.GetComponentInChildren<PlayerStateMachine>();

        //Now Spawn Companions
        SpawnCompanions(!isModernPlayerVersionSpawned);

        //Setup Party Data
        PartyManager.Instance.SetParty();

        //Mark as do not destroy
        DontDestroyOnLoad(spawnedPlayer);

        foreach (GameObject companion in spawnedCompanions)
        {
            DontDestroyOnLoad(companion);
        }

        //Restore Data
        if (restoreCalled)
        {
            RestoreCharacterData();
        }
        else
        {
            //Transport player & Companions
            TransportPlayer();
        }
        
    }

    private void SpawnCompanions(bool spawnFantasyVersions)
    {

#if UNITY_EDITOR
        if (!spawnCompanions) return;
#endif
        if (spawnFantasyVersions)
        {
            foreach(GameObject prefab in fantasyCompanionPrefabs)
            {
                GameObject spawnedCompanion = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                spawnedCompanions.Add(spawnedCompanion);

                CompanionStateMachine companionStateMachine = spawnedCompanion.GetComponentInChildren<CompanionStateMachine>();
                companionStateMachines.Add(companionStateMachine);
            }
        }
        else
        {

        }

        companionFollowBehaviour.SetCompanionFollowBehaviour(companionStateMachines);
    }


    private void TransportPlayer()
    {
        Transform spawnTransform = currentSceneData.GetPlayerStartTransform();
        playerStateMachine.WarpPlayer(spawnTransform, null, true);
    }

    public void ShowPlayerParty(bool show)
    {
        GetPlayerStateMachine().animator.ShowModel(show);

        foreach (CompanionStateMachine companion in companionStateMachines)
        {
            companion.animator.ShowModel(show);
        }
    }

    private void OnDisable()
    {
        SavingLoadingManager.Instance.NewRealmEntered -= OnNewRealmEntered;
    }

    private bool ShouldSpawnPlayer()
    {
        /*When to spawn player? 
         * On load of new level if player not already spawned. 
         * When loading a level in different realm. 
         */

        return !spawnedPlayer ||
            (currentSceneData.GetRealmType() == RealmType.Fantasy && isModernPlayerVersionSpawned) ||
            (currentSceneData.GetRealmType() == RealmType.Modern && !isModernPlayerVersionSpawned);
    }

    //GETTERS
    public PlayerStateMachine GetPlayerStateMachine()
    {
        return playerStateMachine;
    }

    //Saving
    [System.Serializable]
    public class PlayerState
    {
        //Position
        public Vector3 playerPosition = Vector3.zero;
        public Quaternion playerRotation = Quaternion.identity;

        //Vitals
        public Dictionary<string, CharacterHealth.HealthState> characterHealthState = new Dictionary<string, CharacterHealth.HealthState>();

        public bool dataCapture = false;
    }

    public object CaptureState()
    {
        playerState.playerPosition = GetPlayerStateMachine().transform.position;
        playerState.playerRotation = GetPlayerStateMachine().transform.rotation;

        playerState.dataCapture = true;

        Debug.Log("ADD FUNCTIONALITY FOR PLAYER SPAWN MANAGER TO CAPTURE FANTASY HEALTH");

        return SerializationUtility.SerializeValue(playerState, DataFormat.Binary);
    }

    public void RestoreState(object state)
    {
        restoreCalled = true;

        if (state == null)
        {
            resetVitals = true;
            return;
        }

        byte[] bytes = state as byte[];
        playerState = SerializationUtility.DeserializeValue<PlayerState>(bytes, DataFormat.Binary);
    }


    private void RestoreCharacterData()
    {
        if(restoreCalled && playerState.dataCapture)
        {
            //Warp Player
            GetPlayerStateMachine().WarpPlayer(playerState.playerPosition, playerState.playerRotation, PlayerStateMachine.PlayerState.FantasyRoam, true);
        }
        else
        {
            TransportPlayer();
        }

        if (currentSceneData.GetRealmType() == RealmType.Fantasy)
        {
            foreach (PlayerGridUnit playerGridUnit in PartyManager.Instance.GetAllPlayerMembersInWorld())
            {
                CharacterHealth healthComp = playerGridUnit.GetComponent<CharacterHealth>();

                if (healthComp)
                {
                    healthComp.RestoreState(resetVitals ? null : playerState.characterHealthState[playerGridUnit.unitName]);
                }
            }
        }

        //Update Bools
        isDataRestored = true;
        restoreCalled = false;
        resetVitals = false;
    }

    public bool IsDataRestored()
    {
        return isDataRestored;
    }
}
