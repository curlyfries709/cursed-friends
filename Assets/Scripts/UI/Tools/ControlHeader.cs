using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlHeader : MonoBehaviour
{
    private void Awake()
    {
        if(transform.childCount >= 3)
            ControlsManager.Instance.AddControlHeader(transform);
    }

    private void OnEnable()
    {
        ControlsManager.Instance.UpdateControlHeader(transform);
    }

    private void OnDestroy()
    {
        ControlsManager.Instance.RemoveControlHeader(transform);
    }
}
