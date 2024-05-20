using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(BoxCollider))]
public class GridObstacle : MonoBehaviour
{
    [SerializeField] Collider modelCollider;
    [SerializeField] BoxCollider gridCollider;

    private void OnEnable()
    {
        gridCollider.isTrigger = true;
        //PathFinding.Instance.SetNonwalkableNode(gridCollider, modelCollider, true);
    }

    private void OnDisable()
    {
        //PathFinding.Instance.SetNonwalkableNode(gridCollider, modelCollider, false);
    }
}
