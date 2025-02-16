using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSystemsManager : MonoBehaviour
{
    public static GameSystemsManager Instance { get; private set; }

    List<IMultiWorldCombatContacter> combatContacters = new List<IMultiWorldCombatContacter>();

    SceneData currentSceneData;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
    }

    public void SetupNewSceneLoadedData(SceneData newSceneData)
    {
        //Set Scene Data
        currentSceneData = newSceneData;
        //Spawn the player & Companions
        PlayerSpawnerManager.Instance.OnNewSceneLoadedEarly(currentSceneData);
        //Setup Level Grid
        LevelGrid.Instance.OnNewGridSceneLoadedEarly(currentSceneData);
    }

    public void CombatManagerSet(bool isSet)
    {
        foreach (IMultiWorldCombatContacter contacter in combatContacters)
        {
            contacter.SubscribeToCombatManagerEvents(isSet);
        }
    }

    public void ListenForCombatManagerInitialization(IMultiWorldCombatContacter contacter)
    {
        if (FantasyCombatManager.Instance)
        {
            //If it already exist, trigger the subscription
            contacter.SubscribeToCombatManagerEvents(true);
        }

        if(!combatContacters.Contains(contacter))
            combatContacters.Add(contacter);
    }

    //Getters
    public SceneData GetCurrentSceneData()
    {
        return currentSceneData;
    }

    public FantasySceneData GetSceneDataAsFantasyData()
    {
        return currentSceneData as FantasySceneData;
    }
}
