using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModernRoamCameraController : BaseCameraController
{
    [Title("Cinemachine Clamps")]
    [Tooltip("How far in degrees can you move the camera up")]
    [SerializeField] float TopClamp = 70.0f;
    [Tooltip("How far in degrees can you move the camera down")]
    [SerializeField] float BottomClamp = -30.0f;
    [Title("Cinemachine Values")]
    [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
    [SerializeField] float CameraAngleOverride = 0.0f;
    [Title("Camera Speeds")]
    [SerializeField] float defaultXSpeedOverride = 1f;
    [SerializeField] float defaultYSpeedOverride = 1f;

    // cinemachine
    private float cinemachineTargetYaw;
    private float cinemachineTargetPitch;

    protected override void Awake()
    {
        base.Awake();
        cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
    }
    protected override void OnEnable()
    {
        base.OnEnable();

        //Hide the maginium particles in modern world.
        mainCam.transform.GetChild(0).gameObject.SetActive(false);
    }

    private void Update()
    {
        if (enabled)
            RWRoamCameraRotation();
    }

    private void RWRoamCameraRotation()
    {
        // if there is an input and camera position is not fixed
        if (inputLookValue.sqrMagnitude >= _threshold && !LockCameraPosition)
        {
            //Don't multiply mouse input by Time.deltaTime;
            float deltaTimeMultiplier = ControlsManager.Instance.IsCurrentDeviceKeyboardMouse() ? 1.0f : Time.deltaTime;

            if (invertX)
            {
                cinemachineTargetYaw += inputLookValue.x * deltaTimeMultiplier * defaultXSpeed;
            }
            else
            {
                cinemachineTargetYaw -= inputLookValue.x * deltaTimeMultiplier * defaultXSpeed;
            }

            if(invertY)
            {
                cinemachineTargetPitch -= inputLookValue.y * deltaTimeMultiplier * defaultYSpeed;
            }
            else
            {
                cinemachineTargetPitch += inputLookValue.y * deltaTimeMultiplier * defaultYSpeed;
            }
             
        }

        // clamp our rotations so our values are limited 360 degrees
        cinemachineTargetYaw = ClampAngle(cinemachineTargetYaw, float.MinValue, float.MaxValue);
        cinemachineTargetPitch = ClampAngle(cinemachineTargetPitch, BottomClamp, TopClamp);

        // Cinemachine will follow this target
        CinemachineCameraTarget.transform.rotation = Quaternion.Euler(cinemachineTargetPitch + CameraAngleOverride,
            cinemachineTargetYaw, 0.0f);
    }


    private float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    protected override void SetDefaultSpeedValues()
    {
        defaultXSpeed = defaultXSpeedOverride;
        defaultYSpeed = defaultYSpeedOverride;
    }

    protected override void UpdateOrbitalTransposerSettings()
    {
        //Doesnt user OrbitalTransposer so doesn't apply
    }
}
