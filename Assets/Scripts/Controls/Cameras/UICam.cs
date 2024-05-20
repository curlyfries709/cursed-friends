using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UICam : MonoBehaviour
{
    Camera mainCam;
    Camera uiCam;
    private void Awake()
    {
        mainCam = transform.parent.GetComponent<Camera>();
        uiCam = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        uiCam.fieldOfView = mainCam.fieldOfView;
    }
}
