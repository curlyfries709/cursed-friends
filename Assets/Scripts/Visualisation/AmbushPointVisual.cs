using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmbushPointVisual : MonoBehaviour
{
    [SerializeField] float sphereRadius = 0.3f;
    [SerializeField] Color sphereColor;

    private void OnDrawGizmos()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            Gizmos.color = sphereColor;
            Gizmos.DrawSphere(child.position, sphereRadius);
        }
    }
}
