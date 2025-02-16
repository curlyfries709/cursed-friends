using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VCamCullingMask : MonoBehaviour
{
    [SerializeField] bool activateOnEnable = true;
    [SerializeField] LayerMask cullingMask;

    Camera mainCam;
    LayerMask defaultLayerMask;

    private void Awake()
    {
        mainCam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        defaultLayerMask = mainCam.cullingMask;
    }

    private void OnEnable()
    {
        if (activateOnEnable)
            SetCullingMask();
    }

    public void SetCullingMask()
    {
        mainCam.cullingMask = cullingMask;
    }

    private void OnDisable()
    {
        mainCam.cullingMask = defaultLayerMask;
    }
}
