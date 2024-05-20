using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicObstacle : MonoBehaviour
{
    [SerializeField] BoxCollider obstacleCollider;
    [SerializeField] Collider modelCollider;

    private void OnEnable()
    {
        PathFinding.Instance.SetDynamicObstacle(obstacleCollider, modelCollider, true);
    }

    public void OnWarp()
    {
        if (!enabled) { return; }

        obstacleCollider.enabled = false;
        obstacleCollider.enabled = true;

        //Remove Obstacle
        PathFinding.Instance.SetDynamicObstacle(obstacleCollider, modelCollider, false);
        //Add Obstacle At New Pos.
        PathFinding.Instance.SetDynamicObstacle(obstacleCollider, modelCollider, true);

        //Debug.Log("Obstacle At Pos: " + LevelGrid.Instance.TryGetObstacleAtPosition(LevelGrid.Instance.gridSystem.GetGridPosition(transform.position), out Collider ob));
    }

    private void OnDisable()
    {
        PathFinding.Instance.SetDynamicObstacle(obstacleCollider, modelCollider, false);
    }

}
