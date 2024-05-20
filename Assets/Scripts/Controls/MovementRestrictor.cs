using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MovementRestrictor : MonoBehaviour
{
    [SerializeField] Transform raycastPoint;
    [SerializeField] float raycastLength = 1;
    [Space(5)]
    [SerializeField] LayerMask layersToDetect;
    [SerializeField] LayerMask unwalkableEnvironmentLayers;

    public Action<bool> OnUnwalkableAreaDetected;

    bool isUnwalkable = false;


    // Update is called once per frame
    void Update()
    {
        DetectUnwalkable();
    }

    private void DetectUnwalkable()
    {
        bool didHit = Physics.Raycast(raycastPoint.transform.position, transform.forward, out RaycastHit hitInfo, raycastLength, layersToDetect, QueryTriggerInteraction.Collide);
        Debug.DrawRay(raycastPoint.transform.position, transform.forward * raycastLength, Color.red);

        if (didHit && unwalkableEnvironmentLayers == (unwalkableEnvironmentLayers | (1 << hitInfo.collider.gameObject.layer)))
        {
            if (isUnwalkable != didHit)
            {
                SetIsUnwalkable(didHit);
            }
        }
        else if (isUnwalkable != false)
        {
            SetIsUnwalkable(false);
        }
    }


    private void SetIsUnwalkable(bool newValue)
    {
        isUnwalkable = newValue;
        OnUnwalkableAreaDetected?.Invoke(isUnwalkable);
    }
}
