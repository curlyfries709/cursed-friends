using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;
using Cinemachine;

public class RoamCameraController : BaseCameraController
{
    [Title("Cinemachine Objects")]
    [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
    [SerializeField] GameObject CinemachineCameraTarget;
    [SerializeField] protected CinemachineVirtualCamera VCamComponent;
    [Title("Cinemachine Clamps")]

    [Tooltip("How far in degrees can you move the camera up")]
    [SerializeField] float TopClamp = 70.0f;

    [Tooltip("How far in degrees can you move the camera down")]
    [SerializeField] float BottomClamp = -30.0f;
    [Space(5)]
    [SerializeField] float minFollowYOffset = 2;
    [SerializeField] float maxFollowYOffset = 10;
    [SerializeField] float minFollowZOffset = -16;
    [SerializeField] float maxFollowZOffset = -8;
    [Title("Cinemachine Values")]
    [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
    [SerializeField] float CameraAngleOverride = 0.0f;
    [SerializeField] float fwCamZoomSpeed = 10f;

    [Tooltip("For locking the camera position on all axis")]
    [SerializeField] bool LockCameraPosition = false;

    // cinemachine
    private float cinemachineTargetYaw;
    private float cinemachineTargetPitch;

    private const float _threshold = 0.01f;

    //Input Variable
    private Vector2 inputLookValue;

    //Cache
    PlayerInput playerInput;

    protected CinemachineTransposer camTransposer;
    protected CinemachineOrbitalTransposer orbitalTransposer;

    protected override void Awake()
    {
        playerInput = ControlsManager.Instance.GetPlayerInput();

        if(!CinemachineCameraTarget)
            CinemachineCameraTarget = StoryManager.Instance.GetPlayerStateMachine().CinemachineCameraTarget;

        cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
        camTransposer = VCamComponent.GetCinemachineComponent<CinemachineTransposer>();
        orbitalTransposer = VCamComponent.GetCinemachineComponent<CinemachineOrbitalTransposer>();

        base.Awake();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        playerInput.onActionTriggered += OnLook;
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
                followOffsetY -= inputLookValue.y * Time.deltaTime * fwCamZoomSpeed * YCamSpeedMultiplier;
                followOffsetZ += inputLookValue.y * Time.deltaTime * fwCamZoomSpeed * YCamSpeedMultiplier;
            }
            else
            {
                followOffsetY += inputLookValue.y * Time.deltaTime * fwCamZoomSpeed * YCamSpeedMultiplier;
                followOffsetZ -= inputLookValue.y * Time.deltaTime * fwCamZoomSpeed * YCamSpeedMultiplier;
            }
            
            followOffsetY = Mathf.Clamp(followOffsetY, minFollowYOffset, maxFollowYOffset);
            followOffsetZ = Mathf.Clamp(followOffsetZ, minFollowZOffset, maxFollowZOffset);

            camTransposer.m_FollowOffset = new Vector3(camTransposer.m_FollowOffset.x, followOffsetY, followOffsetZ);
        }
    }

    private void RWRoamCameraRotation()
    {
        // if there is an input and camera position is not fixed
        if (inputLookValue.sqrMagnitude >= _threshold && !LockCameraPosition)
        {
            //Don't multiply mouse input by Time.deltaTime;
            float deltaTimeMultiplier = ControlsManager.Instance.IsCurrentDeviceKeyboardMouse() ? 1.0f : Time.deltaTime;

            cinemachineTargetYaw += inputLookValue.x * deltaTimeMultiplier;
            cinemachineTargetPitch += inputLookValue.y * deltaTimeMultiplier;
        }

        // clamp our rotations so our values are limited 360 degrees
        cinemachineTargetYaw = ClampAngle(cinemachineTargetYaw, float.MinValue, float.MaxValue);
        cinemachineTargetPitch = ClampAngle(cinemachineTargetPitch, BottomClamp, TopClamp);

        // Cinemachine will follow this target
        CinemachineCameraTarget.transform.rotation = Quaternion.Euler(cinemachineTargetPitch + CameraAngleOverride,
            cinemachineTargetYaw, 0.0f);
    }

    protected override void SetDefaultSpeedValues()
    {
        defaultXSpeed = orbitalTransposer.m_XAxis.m_MaxSpeed;
        defaultYSpeed = fwCamZoomSpeed;
    }


    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void OnDisable()
    {
        playerInput.onActionTriggered -= OnLook;
    }

    //Input Method
    private void OnLook(InputAction.CallbackContext context)
    {
        if (context.action.name != "Look") { return; }
        inputLookValue = context.ReadValue<Vector2>();
    }


}
