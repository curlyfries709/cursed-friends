using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamCanvas : MonoBehaviour
{
    [SerializeField] Canvas canvas;

    void Awake()
    {
        if (!canvas.worldCamera)
            canvas.worldCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
    }

}
