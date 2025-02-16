using Cinemachine;
using UnityEngine;

public class CameraFollowPlayerSetter : MonoBehaviour
{
    [SerializeField] bool followTarget = true;
    [SerializeField] bool lookAtTarget = false;
    //Cache
    CinemachineVirtualCamera VCam;

    private void Awake()
    {
        VCam = GetComponent<CinemachineVirtualCamera>();
    }

    private void OnEnable()
    {
        Transform targetTransform = PlayerSpawnerManager.Instance.GetPlayerStateMachine().CinemachineCameraTarget.transform;

        if (followTarget)
            VCam.Follow = targetTransform;

        if(lookAtTarget)
            VCam.LookAt = targetTransform;
    }
}
