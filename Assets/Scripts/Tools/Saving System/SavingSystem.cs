using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.SceneManagement;
using System.Linq;

public class SavingSystem : MonoBehaviour
{
    Dictionary<string, object> previousSceneState = new Dictionary<string, object>();
    Dictionary<string, object> currentLoadedData = new Dictionary<string, object>();

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
        CaptureState(previousSceneState);
    }


    public void Save(string saveFile)
    {
        Dictionary<string, object> state = LoadFile(saveFile);

        if (previousSceneState.Count > 0)
        {
            Debug.Log("Saving Previous Scene Data too");
            state = state.Concat(previousSceneState).ToDictionary(x => x.Key, x => x.Value);
        }

        CaptureState(state);
        SaveFile(saveFile, state);
        previousSceneState.Clear();
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


    public void Load(string saveFile)
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
        currentLoadedData = state;

        foreach (SaveableEntity saveable in FindObjectsOfType<SaveableEntity>(true))
        {
            string id = saveable.GetUID();

            if (state.ContainsKey(id))
            {
                saveable.RestoreState(state[id]);
            }
        }
    }

    public void NewGameRestore()
    {
        currentLoadedData = null;

        foreach (SaveableEntity saveable in FindObjectsOfType<SaveableEntity>(true))
        {
            saveable.NewGameRestore();
        }
    }

    public void NewTerritoryRestore()
    {
        foreach (SaveableEntity saveable in FindObjectsOfType<SaveableEntity>(true))
        {
            string id = saveable.GetUID();

            if (currentLoadedData.ContainsKey(id))
            {
                saveable.OnNewTerritoryRestoreState(currentLoadedData[id]);
            }
        }
    }

    public void RestoreIndividualState(SaveableEntity saveable)
    {
        string id = saveable.GetUID();

        if (currentLoadedData != null && currentLoadedData.ContainsKey(id))
        {
            saveable.RestoreState(currentLoadedData[id]);
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
