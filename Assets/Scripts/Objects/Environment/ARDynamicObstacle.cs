
using UnityEngine;
using Pathfinding;
using System;

public class ARDynamicObstacle : DynamicObstacle
{
    [Header("Colliders")]
    [Tooltip("Obstacle Collider to determine the grid spaces it is taking up")]
    [SerializeField] BoxCollider gridCollider;
    [Tooltip("Collider for determining point of hit with raycasts")]
    [SerializeField] Collider modelCollider;

    bool isMoving = false; //is this instance of the class being updated. 
    bool completedInitialSet = false;
    bool subscribedToEvent = false;

    //Event
    public Action TransformUpdated; 

    public override void OnGraphsPreUpdate()
    {
        base.OnGraphsPreUpdate();

        if(!CanUpdate()) return;

        //Remove Obstacle from Level Grid
        if(LevelGrid.Instance && LevelGrid.Instance.IsGridSystemValid())
        {
            LevelGrid.Instance.SetDynamicObstacle(gridCollider, modelCollider, false);
        }
        else if (!subscribedToEvent)
        {
            subscribedToEvent = true;
            SavingLoadingManager.Instance.NewSceneLoadComplete += SetObstacleInLevelGrid;
        }
    }

    public void Warp(Transform destination)
    {
        isMoving = true;

        //Update Pos & Rot
        transform.position = destination.position;
        transform.rotation = destination.rotation;
    }

    public override void OnGraphsPostUpdate()
    {
        base.OnGraphsPostUpdate();

        if (!CanUpdate()) return;

        //Add Obstacle to Level Grid
        if (LevelGrid.Instance && LevelGrid.Instance.IsGridSystemValid())
        {
            LevelGrid.Instance.SetDynamicObstacle(gridCollider, modelCollider, true);
            completedInitialSet = true;
            isMoving = false;

            TransformUpdated?.Invoke();
        }    
    }

    public void SetObstacleInLevelGrid(SceneData sceneData)
    {
        LevelGrid.Instance.SetDynamicObstacle(gridCollider, modelCollider, false);
        LevelGrid.Instance.SetDynamicObstacle(gridCollider, modelCollider, true);
        completedInitialSet = true;

        SavingLoadingManager.Instance.NewSceneLoadComplete -= SetObstacleInLevelGrid;
        subscribedToEvent = false;
        isMoving = false;
    }

    bool CanUpdate()
    {
        if (!Application.isPlaying)
        {
            return false;
        }

        if (!completedInitialSet)
        {
            return true;
        }

        return isMoving;
    }
}
