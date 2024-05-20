using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public abstract class BaseCameraController : MonoBehaviour
{
    protected float defaultXSpeed;
    protected float defaultYSpeed;

    protected float XCamSpeedMultiplier = 1;
    protected float YCamSpeedMultiplier = 1;

    protected bool invertX = false;
    protected bool invertY = false;

    protected CinemachineFreeLook freeLookCam;
    protected ICinemachineCamera cinemachineCamera;
    protected CinemachineInputProvider inputProvider;

    protected virtual void Awake()
    {
        SetDefaultSpeedValues();
        inputProvider = GetComponent<CinemachineInputProvider>();
    }

    protected virtual void OnEnable()
    {
        ConfigureSettings();
    }

    public void OnCamLive()
    {
        ControlsManager.Instance.OnControlledCamLive(inputProvider);
    }

    protected abstract void SetDefaultSpeedValues();
    protected abstract void UpdateOrbitalTransposerSettings();

    private void ConfigureSettings()
    {
        XCamSpeedMultiplier = GameManager.Instance.XCamSpeedMultiplier;
        YCamSpeedMultiplier = GameManager.Instance.YCamSpeedMultiplier;

        invertX = GameManager.Instance.InvertXCam;
        invertY = GameManager.Instance.InvertYCam;

        UpdateOrbitalTransposerSettings();
    }
}
