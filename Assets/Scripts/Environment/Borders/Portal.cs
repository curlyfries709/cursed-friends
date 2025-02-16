
using UnityEditor;
using UnityEngine;

public class Portal : MonoBehaviour
{
    [SerializeField] SceneAsset sceneNameToLoad;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Transport();
        }
    }

    private void Transport()
    {
        SavingLoadingManager.Instance.EnterNewTerritory(sceneNameToLoad.name);
    }
}
