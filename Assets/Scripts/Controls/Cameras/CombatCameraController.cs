using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CombatCameraController : BaseCameraController
{
    [SerializeField] CinemachineFreeLook freeLookComponent;

    protected override void SetDefaultSpeedValues()
    {
        defaultXSpeed = freeLookComponent.m_XAxis.m_MaxSpeed;
        defaultYSpeed = freeLookComponent.m_YAxis.m_MaxSpeed;
    }

    protected override void UpdateOrbitalTransposerSettings()
    {
        //Invert
        freeLookComponent.m_XAxis.m_InvertInput = invertX;
        freeLookComponent.m_YAxis.m_InvertInput = !invertY;

        //Set Speeds
        freeLookComponent.m_XAxis.m_MaxSpeed = defaultXSpeed * XCamSpeedMultiplier;
        freeLookComponent.m_YAxis.m_MaxSpeed = defaultYSpeed * YCamSpeedMultiplier;
    }
}
