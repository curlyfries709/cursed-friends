using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RealmType
{
    Fantasy,
    Modern,
    NotSet
}

public class SceneData : MonoBehaviour
{
    [Header("Scene Data")]
    [SerializeField] protected string sceneName;
    [SerializeField] protected RealmType realmType = RealmType.Fantasy;
    [Space(10)]
    [SerializeField] Transform playerStartTransform;
    [Space(10)]
    [SerializeField] protected AudioClip sceneMusic;

    //GETTERS
    public string GetSceneName()
    {
        return sceneName;
    }

    public AudioClip GetSceneMusic()
    {
        return sceneMusic;
    }
    
    public RealmType GetRealmType()
    {
        return realmType;
    }

    public bool IsFantasyRealm()
    {
        return realmType == RealmType.Fantasy;
    }

    public Transform GetPlayerStartTransform()
    {
        return playerStartTransform;
    }
}
