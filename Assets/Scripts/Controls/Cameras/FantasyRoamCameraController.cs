using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;
using Cinemachine;

public class FantasyRoamCameraController : BaseCameraController
{
    [Title("Cinemachine Objects")]
    [SerializeField] protected CinemachineVirtualCamera VCamComponent;
    [Space(5)]
    [SerializeField] float minFollowYOffset = 2;
    [SerializeField] float maxFollowYOffset = 10;
    [SerializeField] float minFollowZOffset = -16;
    [SerializeField] float maxFollowZOffset = -8;
    [Title("Camera Speeds")]
    [Tooltip("Manipulate Camera's Y Speed")]
    [SerializeField] float zoomSpeed = 10f;


    //Cache

    protected CinemachineTransposer camTransposer;
    protected CinemachineOrbitalTransposer orbitalTransposer;

    protected override void Awake()
    {
        if(!CinemachineCameraTarget)
            CinemachineCameraTarget = PlayerSpawnerManager.Instance.GetPlayerStateMachine().CinemachineCameraTarget;

        camTransposer = VCamComponent.GetCinemachineComponent<CinemachineTransposer>();
        orbitalTransposer = VCamComponent.GetCinemachineComponent<CinemachineOrbitalTransposer>();

        base.Awake();
    }


    protected override void UpdateOrbitalTransposerSettings()
    {
        orbitalTransposer.m_XAxis.m_InvertInput = invertX;
        orbitalTransposer.m_XAxis.m_MaxSpeed = defaultXSpeed * XCamSpeedMultiplier;
    }

    private void Update()
    {
        if(enabled)
            FWRoamCameraControl();
    }

    private void FWRoamCameraControl()
    {
        // if there is an input and camera position is not fixed
        if (inputLookValue.sqrMagnitude >= _threshold && !LockCameraPosition)
        {
            //Horizontal Rotation Now Handled By Orbital Transposer.

            //Zoom
            float followOffsetY = camTransposer.m_FollowOffset.y;
            float followOffsetZ = camTransposer.m_FollowOffset.z;

            if (invertY)
            {
                followOffsetY -= inputLookValue.y * Time.deltaTime * zoomSpeed * YCamSpeedMultiplier;
                followOffsetZ += inputLookValue.y * Time.deltaTime * zoomSpeed * YCamSpeedMultiplier;
            }
            else
            {
                followOffsetY += inputLookValue.y * Time.deltaTime * zoomSpeed * YCamSpeedMultiplier;
                followOffsetZ -= inputLookValue.y * Time.deltaTime * zoomSpeed * YCamSpeedMultiplier;
            }
            
            followOffsetY = Mathf.Clamp(followOffsetY, minFollowYOffset, maxFollowYOffset);
            followOffsetZ = Mathf.Clamp(followOffsetZ, minFollowZOffset, maxFollowZOffset);

            camTransposer.m_FollowOffset = new Vector3(camTransposer.m_FollowOffset.x, followOffsetY, followOffsetZ);
        }
    }



    protected override void SetDefaultSpeedValues()
    {
        defaultXSpeed = orbitalTransposer.m_XAxis.m_MaxSpeed;
        defaultYSpeed = zoomSpeed;
    }



}
