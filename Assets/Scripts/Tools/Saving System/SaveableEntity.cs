using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using Sirenix.OdinInspector;

[ExecuteAlways]
public class SaveableEntity : MonoBehaviour
{
    [ReadOnly]
    [SerializeField] string uniqueIdentifier = "";
    static Dictionary<string, SaveableEntity> globalLookUp = new Dictionary<string, SaveableEntity>();

   public string GetUID()
   {
        return uniqueIdentifier;
   }

    public object CaptureState()
    {
        Dictionary<string, object> state = new Dictionary<string, object>();
        //print("Capturing STate for " + gameObject.name + " With UID: " + GetUID());
        foreach (ISaveable saveable in GetComponents<ISaveable>())
        {
            state[saveable.GetType().ToString()] = saveable.CaptureState();
        }
        
        return state;
    }

    public void NewGameRestore()
    {
        foreach (ISaveable saveable in GetComponents<ISaveable>())
        {
            saveable.RestoreState(null);
        }
    }

    public void RestoreState(object state)
    {
        Restore(state, false);
    }

    public void OnNewTerritoryRestoreState(object state)
    {
        Restore(state, true);
    }

    private void Restore(object state, bool isNewSceneRestore)
    {
        //print("Restoring STate for " + gameObject.name + " With UID: " + GetUID());
        Dictionary<string, object> stateDict = (Dictionary<string, object>)state;
        foreach (ISaveable saveable in GetComponents<ISaveable>())
        {
            if (isNewSceneRestore && !saveable.AutoRestoreOnNewTerritoryEntry) { continue; }

            string typeString = saveable.GetType().ToString();

            if (stateDict.ContainsKey(typeString))
            {
                saveable.RestoreState(stateDict[typeString]);
            }
        }
    }



#if UNITY_EDITOR
    private void Update()
    {
        if (Application.IsPlaying(gameObject)) return;

        //if true means it's a prefab and not gameobject in scene.
        if (string.IsNullOrEmpty(gameObject.scene.path)) return;

        //Generating UUID during editing time & not Runtime
        SerializedObject serializedObj = new SerializedObject(this);
        SerializedProperty property = serializedObj.FindProperty("uniqueIdentifier");
       
        if(string.IsNullOrEmpty(property.stringValue)  || !IsUnique(property.stringValue))
        {
            property.stringValue = System.Guid.NewGuid().ToString();
            serializedObj.ApplyModifiedProperties();
        }

        globalLookUp[property.stringValue] = this;
    }
#endif
    private bool IsUnique(string candidate)
    {
        if (!globalLookUp.ContainsKey(candidate)) return true;

        if (globalLookUp[candidate] == this) return true;

        if(globalLookUp[candidate] == null)
        {
            globalLookUp.Remove(candidate);
            return true;
        }

        if(globalLookUp[candidate].GetUID() != candidate)
        {
            globalLookUp.Remove(candidate);
            return true;
        }
        return false;
    }

}
