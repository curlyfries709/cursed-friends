using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;

public abstract class BaseCameraController : MonoBehaviour
{
    [Header("Player Input")]
    [SerializeField] protected bool listenToPlayerInput = false;
    [Tooltip("For locking the camera position on all axis")]
    [SerializeField] protected bool LockCameraPosition = false;
    [Header("Cinemachine Components")]
    [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
    [SerializeField] protected GameObject CinemachineCameraTarget;

    protected float defaultXSpeed;
    protected float defaultYSpeed;

    protected float XCamSpeedMultiplier = 1;
    protected float YCamSpeedMultiplier = 1;

    protected bool invertX = false;
    protected bool invertY = false;

    //Cache
    protected PlayerInput playerInput;
    protected CinemachineFreeLook freeLookCam;
    protected ICinemachineCamera cinemachineCamera;
    protected CinemachineInputProvider inputProvider;
    protected Camera mainCam;

    //Input Variable
    protected Vector2 inputLookValue;
    protected const float _threshold = 0.01f;

    protected virtual void Awake()
    {
        SetDefaultSpeedValues();

        inputProvider = GetComponent<CinemachineInputProvider>();
        playerInput = ControlsManager.Instance.GetPlayerInput();
        mainCam = Camera.main;

    }

    protected virtual void OnEnable()
    {
        ConfigureSettings();

        if(listenToPlayerInput)
            playerInput.onActionTriggered += OnLook;
    }

    public void OnCamLive()
    {
        ControlsManager.Instance.OnControlledCamLive(inputProvider);
    }

    protected abstract void SetDefaultSpeedValues();
    protected abstract void UpdateOrbitalTransposerSettings();

    private void OnDisable()
    {
        if(listenToPlayerInput)
            playerInput.onActionTriggered -= OnLook;
    }

    private void ConfigureSettings()
    {
        XCamSpeedMultiplier = GameManager.Instance.XCamSpeedMultiplier;
        YCamSpeedMultiplier = GameManager.Instance.YCamSpeedMultiplier;

        invertX = GameManager.Instance.InvertXCam;
        invertY = GameManager.Instance.InvertYCam;

        UpdateOrbitalTransposerSettings();
    }

    //Input Method
    private void OnLook(InputAction.CallbackContext context)
    {
        if (context.action.name != "Look") { return; }
        inputLookValue = context.ReadValue<Vector2>();
    }
}
