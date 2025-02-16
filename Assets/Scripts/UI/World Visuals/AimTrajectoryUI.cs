using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class AimTrajectoryUI : MonoBehaviour
{
    [Header("Throw Data")]
    [SerializeField] LayerMask layerMask;
    [Header("Trajectory Visual")]
    [SerializeField] LineRenderer aimTrajectoryVisual;
    [SerializeField] int aimTrajectorylineSegment = 25;

    ThrowableTool throwable = null;

    public void Begin(ThrowableTool throwable)
    {
        this.throwable = throwable;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!throwable) { return; }
        Visualize(throwable.GetThrowOrigin().position, throwable.GetVelocityToReachDestination());
    }

    void Visualize(Vector3 origin, Vector3 initialVelocity)
    {
        aimTrajectoryVisual.positionCount = aimTrajectorylineSegment;

        for (int i = 0; i < aimTrajectorylineSegment; i++)
        {
            float time = i * 0.1f;
            Vector3 targetPosition = origin + ((initialVelocity * time) + (0.5f * Physics.gravity * time * time));

            bool didHit = false;

            if(i > 0)
            {
                //Raycast between each point to see if obstacle in the way, if obstacle, end drawing.
                Vector3 prevPosition = aimTrajectoryVisual.GetPosition(i - 1);

                //If Hit Something. 
                if (Physics.Linecast(prevPosition, targetPosition, out RaycastHit hitInfo, layerMask, QueryTriggerInteraction.Ignore))
                {
                    if(hitInfo.collider.tag == "Companion")
                    {
                        Debug.Log("IMPLEMENT ORDER COMPANION TO MOVE FROM THROW PATH");
                    }

                    //Update TargetPosition
                    targetPosition = hitInfo.point;
                    didHit = true;

                    Debug.Log("Trajectory hit " + hitInfo.collider.name + " at index: " + i);
                }
            }

            aimTrajectoryVisual.SetPosition(i, targetPosition);

            if (didHit)
            {
                aimTrajectoryVisual.positionCount = i + 1;
                
                return;
            }    
        }
    }

    public void End()
    {
        gameObject.SetActive(false);
    }
}
