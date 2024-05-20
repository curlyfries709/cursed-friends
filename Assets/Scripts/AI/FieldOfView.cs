using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldOfView : MonoBehaviour
{
    [Header("Transform")]
    public Transform eyePoint;
    [SerializeField] Collider playerController;
    [Header("Values")]
    public float viewRadius;
    [Range(0, 360)]
    public float viewAngle;
    [Space(5)]
    [SerializeField] LayerMask fieldOfViewLayerMask;

    public Collider CanSeeSupiciousTarget(Collider target)
    {
        /*Vector3 testPos = new Vector3(target.position.x, eyePoint.position.y, target.position.z);
        //Vector3 dirToTarget = (target.position - eyePoint.position).normalized;
        Vector3 angleDirToTarget = (testPos - eyePoint.position).normalized;*/

        Vector3 closestPosOnCollider = target.ClosestPoint(eyePoint.position);
        Vector3 testPos = new Vector3(closestPosOnCollider.x, eyePoint.position.y, closestPosOnCollider.z);

        Vector3 dirToTarget = (closestPosOnCollider - eyePoint.position).normalized;
        Vector3 angleDirToTarget = (testPos - eyePoint.position).normalized;

        if (Vector3.Angle(transform.forward, angleDirToTarget) < viewAngle / 2)
        {
            //bool didHit = Physics.Raycast(eyePoint.position, angleDirToTarget, out RaycastHit hitInfo, viewRadius, fieldOfViewLayerMask, QueryTriggerInteraction.Ignore);
            //Debug.DrawRay(eyePoint.position, dirToTarget * viewRadius, Color.red);
            bool didHit = Physics.Raycast(eyePoint.position, dirToTarget, out RaycastHit hitInfo, viewRadius, fieldOfViewLayerMask, QueryTriggerInteraction.Ignore);

            if (didHit)
            {
                return hitInfo.collider;
            }
        }


        return null;
    }

    public bool CanSeePlayerTest()
    {
        return CanSeeSupiciousTarget(playerController);
    }

    public bool ShowEditorUI()
    {
        if (playerController == null)
            playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<CharacterController>();

        return playerController;
    }


    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }

        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}
