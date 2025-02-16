using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.SceneManagement;

public class SavingSystem : MonoBehaviour
{
    Dictionary<string, object> currentGameSessionData = new Dictionary<string, object>();

    const string lastSavedSceneKey = "lastSceneBuildIndex";

    //Scene Loading
    public IEnumerator LoadLastScene(string saveFile)
    {
        Dictionary<string, object> state = LoadFile(saveFile);

        if (state.ContainsKey(lastSavedSceneKey))
        {
            int buildIndex = (int)state[lastSavedSceneKey];

            if (buildIndex != SceneManager.GetActiveScene().buildIndex)
            {
                yield return SceneManager.LoadSceneAsync(buildIndex);
            }
        }
       
        RestoreState(state);
    }

    public int GetLastSavedSceneIndex(string saveFile)
    {
        Dictionary<string, object> state = LoadFile(saveFile);

        if (state.ContainsKey(lastSavedSceneKey))
        {
            int buildIndex = (int)state[lastSavedSceneKey];
            return buildIndex;
        }

        return -1;
    }

    public void StoreCurrentSceneData()
    {
        CaptureState(currentGameSessionData);
    }


    public void Save(string saveFile)
    {
        //Dictionary<string, object> state = LoadFile(saveFile);
        Dictionary<string, object> state = currentGameSessionData;
        CaptureState(state);
        SaveFile(saveFile, state);
    }

    public void SavePersistentData(string saveFile, string key, object data)
    {
        Dictionary<string, object> persistentData = LoadFile(saveFile);
        persistentData[key] = data;
        SaveFile(saveFile, persistentData);
    }

    public Dictionary<string, object> LoadPersistentData(string saveFile)
    {
        return LoadFile(saveFile);
    }

    private void SaveFile(string saveFile, object state)
    {
        string path = GetPathFromSaveFile(saveFile);

        Debug.Log("Save File: " + path);
        //print("FilePath: " + path);

        /*Creates new file...if already exists it overwrites the file. */
        using (FileStream stream = File.Open(path, FileMode.Create))
        {
            //Using keyword auto closes files.
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, state);
        }
    }


    public void LoadFromFile(string saveFile)
    {
        RestoreState(LoadFile(saveFile));
    }

    private Dictionary<string, object> LoadFile(string saveFile)
    {
        string path = GetPathFromSaveFile(saveFile);

        if (!File.Exists(path))
        {
            return new Dictionary<string, object>();
        }
   
        using (FileStream stream = File.Open(path, FileMode.Open))
        {
            //Using keyword auto closes files.

            BinaryFormatter formatter = new BinaryFormatter();
            return (Dictionary<string, object>)formatter.Deserialize(stream);
        }
    }

    private void CaptureState(Dictionary<string, object> state)
    {
        foreach (SaveableEntity saveable in FindObjectsOfType<SaveableEntity>(true))
        {
            state[saveable.GetUID()] = saveable.CaptureState();
        }

        state[lastSavedSceneKey] = SceneManager.GetActiveScene().buildIndex;
    }

    private void RestoreState(Dictionary<string, object> state)
    {
        currentGameSessionData = state;

        foreach (SaveableEntity saveable in FindObjectsOfType<SaveableEntity>(true))
        {
            string id = saveable.GetUID();

            if (state.ContainsKey(id))
            {
                saveable.RestoreState(state[id]);
            }
        }
    }

    /*public void NewGameRestore()
    {
        foreach (SaveableEntity saveable in FindObjectsOfType<SaveableEntity>(true))
        {
            saveable.NewGameRestore();
        }
    }*/

    public void NewTerritoryRestore(bool fullRestore) //Full Restore also restores Manager data
    {
        Debug.Log("New Territory Restore called");

        foreach (SaveableEntity saveable in FindObjectsOfType<SaveableEntity>(true))
        {
            string id = saveable.GetUID();

            if (currentGameSessionData.ContainsKey(id))
            {
                saveable.RestoreState(currentGameSessionData[id], fullRestore);
            }
            else
            {
                saveable.RestoreState(null, fullRestore);
            }
        }
    }

    public void RestoreIndividualState(SaveableEntity saveable)
    {
        string id = saveable.GetUID();

        if (currentGameSessionData.ContainsKey(id))
        {
            saveable.RestoreState(currentGameSessionData[id]);
        }
        else
        {
            saveable.RestoreState(null);
        }
    }
    public void DeleteFile(string file)
    {
        string path = GetPathFromSaveFile(file);

        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private string GetPathFromSaveFile(string saveFile)
    {
        return Path.Combine(Application.persistentDataPath, saveFile + ".sav");
    }

    //Getters
    public bool DoesSaveFileExist(string saveFile)
    {
        return File.Exists(GetPathFromSaveFile(saveFile));
    }

}
