using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaypointData : MonoBehaviour
{
    [SerializeField] bool idleAtWaypoint = true;

    const float waypointGizmoRadius = 0.3f;

    private void OnDrawGizmos()
    {
        Gizmos.color = idleAtWaypoint ? Color.cyan : Color.red;
        Gizmos.DrawSphere(transform.position, waypointGizmoRadius);
    }

    public bool ShouldIdleAtWaypoint()
    {
        return idleAtWaypoint;
    }
}
