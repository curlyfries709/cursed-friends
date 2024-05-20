using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolPathVisual : MonoBehaviour
{

    private void OnDrawGizmos()
    {
        for(int i = 0; i < transform.childCount; i++)
        {
            int j = GetNextIndex(i);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(GetWaypoint(i), GetWaypoint(j));
        }
    }

    private Vector3 GetWaypoint(int index)
    {
        return transform.GetChild(index).position;
    }

    private int GetNextIndex(int index)
    {
        if (index + 1 >= transform.childCount)
        {
            return 0;

        }

        return index + 1;
    }
}
