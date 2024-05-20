using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PersistableObject : MonoBehaviour
{
    [SerializeField] PersistableType persistableType;

    public enum PersistableType
    {
        AlwaysPersist,
        MultiWorld,
        FantasyWorldOnly,
        ModernWorldOnly
    }

    static bool alwaysPersistSpawned = false;
    static bool fantasyPersistentSpawned = false;
    static bool multiWorldPersistentSpawned = false;
    static bool modernPersistentSpawned = false;

    //Subscribe to event on when new Scene Loaded. Destroy self if necessary.

    private void Awake()
    {
        Spawn();
    }


    private void Spawn()
    {
        if (persistableType == PersistableType.AlwaysPersist && !alwaysPersistSpawned)
        { 
            alwaysPersistSpawned = true;
            transform.parent = null;
            DontDestroyOnLoad(gameObject);
        }
        else if(persistableType == PersistableType.MultiWorld && !multiWorldPersistentSpawned)
        {
            /*multiWorldPersistentSpawned = true;
            transform.parent = null;
            DontDestroyOnLoad(gameObject);*/
        }
        else if(persistableType == PersistableType.FantasyWorldOnly && !fantasyPersistentSpawned)
        {
            /*fantasyPersistentSpawned = true;
            transform.parent = null;
            DontDestroyOnLoad(gameObject);*/
        }
        else if(persistableType == PersistableType.ModernWorldOnly && !modernPersistentSpawned)
        {
            /*modernPersistentSpawned = true;
            transform.parent = null;
            DontDestroyOnLoad(gameObject);*/
        }
        else
        {
            gameObject.SetActive(false); //To Ensure Childed Scripts DO not execute their awake.
            Destroy(gameObject);
        }
    }
}
