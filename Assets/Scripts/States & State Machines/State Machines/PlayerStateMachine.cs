using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using MoreMountains.Feedbacks;
using System;
using Sirenix.OdinInspector;
using UnityEditor;

public class PlayerStateMachine : CharacterStateMachine, IControls
{
    [Title("Speeds")]
    public float walkSpeed;
    public float runSpeed;
    public float sneakSpeed;
    [Tooltip("How fast the character turns to face movement direction")]
    [Range(0.0f, 0.3f)]
    public float RotationSmoothTime = 0.12f;

    [Tooltip("Acceleration and deceleration")]
    public float SpeedChangeRate = 10.0f;

    //public AudioClip LandingAudioClip;
    //public AudioClip[] FootstepAudioClips;
    //[Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    [Space(10)]
    [Tooltip("The height the player can jump")]
    [HideInInspector] public float JumpHeight = 1.2f;

    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    [HideInInspector] public float Gravity = -15.0f;

    [Space(10)]
    [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
    [HideInInspector] public float JumpTimeout = 0.50f;

    [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
    [HideInInspector] public float FallTimeout = 0.15f;

    [Header("Player Grounded")]
    [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
    public bool Grounded = true;

    [Tooltip("Useful for rough ground")]
    public float GroundedOffset = -0.14f;

    [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    public float GroundedRadius = 0.28f;

    [Tooltip("What layers the character uses as ground")]
    public LayerMask GroundLayers;
    [Title("Proximity Radius")]
    [SerializeField] SphereCollider proximityRadius;
    [Space(10)]
    public float sprintingRadius = 15;
    public float inCombatRadius = 5;
    [Title("Components")]
    [SerializeField] GameObject roamCam;
    public GameObject CinemachineCameraTarget;
    [Space(5)]
    [SerializeField] BoxCollider gridCollider;
    [Space(5)]
    [SerializeField] Transform throwOrigin;
    [Space(5)]
    [SerializeField] List<GameObject> freeRoamComponents = new List<GameObject>();
    [Title("CC Sneak Config")]
    public Vector3 ccSneakCenter;
    public float ccSneakRadius;
    public float ccSneakHeight;
    [Title("Roam Feedbacks")]
    public MMF_Player ambushEnemyFeedback;
    public MMF_Player neutralAttackFeedback;



    // player
    [HideInInspector] public float speed;
    [HideInInspector] public float animationBlend;
    [HideInInspector] public float targetRotation = 0.0f;
    [HideInInspector] public float rotationVelocity;
    [HideInInspector] public float verticalVelocity;
    [HideInInspector] public float terminalVelocity = 53.0f;

    // timeout deltatime
    [HideInInspector] public float jumpTimeoutDelta;
    [HideInInspector] public float fallTimeoutDelta;

    [HideInInspector] public bool disableMovement = false;

    // Input Variables
    public Vector2 moveValue { get; private set; }
    public bool inputSprint { get; private set; }

    //Auto Moving
    public bool IsAutoMoving { get; private set; }

    public float AutoMoveSpeed { get; private set; }

    [HideInInspector] public bool inputJump;

    //State Variables
    [HideInInspector] public bool allowStealthMode = true;

    //Other Variables
    public Vector3 defaultCCCenter { get; private set; }
    public float defaultCCRadius { get; private set; }
    public float defaultCCHeight { get; private set; }

    //Caches
    public CharacterAnimator animator { get; private set; }

    public MovementRestrictor moveRestrictor { get; private set; }
    public CharacterController controller { get; private set; }
    public GameObject mainCam { get; private set; }

    private PlayerInput playerInput;

    //States
    public PlayerFantasyRoamState fantasyRoamState { get; private set; }
    public PlayerFantasyCombatState fantasyCombatState { get; private set; }

    public PlayerStealthState stealthState { get; private set; }

    //Events
    public Action<bool> PlayerIsSprinting;
    public Action<bool> SwitchToStealth;
    public Action<bool> HideCompanions;

    public static Action<bool> PlayerInDanger;
    public static Action<PlayerBaseState> PlayerWarped;

    public enum PlayerState
    {
        FantasyRoam, 
        FantasyCombat,
        Stealth
    }

    private void Awake()
    {
        if (mainCam == null)
        {
            mainCam = GameObject.FindGameObjectWithTag("MainCamera");
        }

        animator = GetComponentInChildren<CharacterAnimator>();
        controller = GetComponent<CharacterController>();
        playerInput = ControlsManager.Instance.GetPlayerInput();
        moveRestrictor = GetComponent<MovementRestrictor>();

        SetProximityRadius(false);
        SetDefaultControllerConfigValues();
        IntializeStates();
    }

    private void OnEnable()
    {
        ControlsManager.Instance.SubscribeToPlayerInput("Player", this);
    }

    private void Start()
    {
        // reset our timeouts on start
        jumpTimeoutDelta = JumpTimeout;
        fallTimeoutDelta = FallTimeout;

        SetStartState();
    }

    private void SetStartState()
    {
        if (FantasyCombatManager.Instance && FantasyCombatManager.Instance.InCombat()) { return; }

        SwitchState(fantasyRoamState);
    }

    public override void BeginCombat()
    {
        SwitchState(fantasyCombatState);
    }

    public void WarpPlayer(Transform newTransform, PlayerState newplayerState, bool autoWarpCompanionsNearPos)
    {
        PlayerBaseState state;

        switch (newplayerState)
        {
            case PlayerState.FantasyCombat:
                state = fantasyCombatState;
                break;
            case PlayerState.Stealth:
                state = stealthState;
                break;
            default:
                state = fantasyRoamState;
                break;
        }

        WarpPlayer(newTransform, state , autoWarpCompanionsNearPos);
    }

    public void WarpPlayer(Vector3 newPosition, Quaternion newRotation, PlayerState newplayerState, bool autoWarpCompanionsNearPos)
    {
        PlayerBaseState state;

        switch (newplayerState)
        {
            case PlayerState.FantasyCombat:
                state = fantasyCombatState;
                break;
            case PlayerState.Stealth:
                state = stealthState;
                break;
            default:
                state = fantasyRoamState;
                break;
        }

        WarpToPosition(newPosition, newRotation);

        if (state != null)
            SwitchState(state);

        if (autoWarpCompanionsNearPos)
            PlayerWarped?.Invoke(state);
    }

    public void WarpPlayer(Transform newTransform, PlayerBaseState newPlayerState, bool autoWarpCompanionsNearPos)
    {
        WarpToPosition(newTransform.position, newTransform.rotation);

        if (newPlayerState != null)
            SwitchState(newPlayerState);

        if (autoWarpCompanionsNearPos)
            PlayerWarped?.Invoke(newPlayerState);
    }

    public override void WarpToPosition(Vector3 newPosition, Quaternion newRotation)
    {
        ActivateFreeRoamCam(false);
        controller.enabled = false;
        transform.position = newPosition;
        transform.rotation = newRotation;
        controller.enabled = true;
        ActivateFreeRoamCam(true);
    }

    public bool TryEnterStealth()
    {
        if (allowStealthMode)
        {
            if (currentState == fantasyRoamState)
            {
                SwitchState(stealthState);
                return true;
            }
        }

        return false;
    }

    public bool ExitStealth()
    {
        if (InStealth())
        {
            SwitchState(fantasyRoamState);
            return true;
        }

        return false;
    }

    public void PrepareForDestruction()
    {
        OnDestroy();
    }

    //SETTERS
    private void IntializeStates()
    {
        fantasyCombatState = new PlayerFantasyCombatState(this);
        fantasyRoamState = new PlayerFantasyRoamState(this);
        stealthState = new PlayerStealthState(this);
    }

    public void ActivateFreeRoamComponents(bool activate)
    {
        foreach (GameObject obj in freeRoamComponents)
        {
            obj.SetActive(activate);
        }

        if(gridCollider)
            gridCollider.enabled = !activate;
    }

    public void ActivateFreeRoamCam(bool activate)
    {
        roamCam.gameObject.SetActive(activate);
    }

    public override void ShowWeapon(bool show)
    {
        GridUnitAnimator unitAnimator = animator as GridUnitAnimator;

        if (unitAnimator)
        {
            unitAnimator.ShowWeapon(show);
        }
    }
    public void SetControllerConfig(Vector3 center, float radius, float height)
    {
        controller.center = center;
        controller.radius = radius;
        controller.height = height;
    }

    public void SetProximityRadius(bool inCombat)
    {
        if (!proximityRadius) { return; }

        proximityRadius.radius = inCombat ? inCombatRadius : sprintingRadius;
    }

    public void SetStealthControllerConfigValues()
    {
        SetControllerConfig(ccSneakCenter, ccSneakRadius, ccSneakHeight);
    }

    private void SetDefaultControllerConfigValues()
    {
        defaultCCCenter = controller.center;
        defaultCCRadius = controller.radius;
        defaultCCHeight = controller.height;
    }

    public void Sprint(bool value)
    {
        inputSprint = value;
        PlayerIsSprinting?.Invoke(value);
    }

    public void BeginAutoMove(bool begin, float autoMoveSpeed)
    {
        IsAutoMoving = begin;
        AutoMoveSpeed = autoMoveSpeed;
    }

    public void ResetMoveValue()
    {
        moveValue = Vector2.zero;
    }


    //GETTERS
    public bool InStealth()
    {
        return currentState == stealthState;
    }

    public float GetSpeed()
    {
        return controller.velocity.magnitude;
    }

    public Transform GetThrowOrigin()
    {
        return throwOrigin;
    }

    //Inputs
    private void OnMove(InputAction.CallbackContext context)
    {
        moveValue = context.ReadValue<Vector2>();
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        InteractionManager.Instance.HandleInteraction(false);
    }

    private void OnUseTool(InputAction.CallbackContext context)
    {
        RoamToolsManager.Instance.UseActiveTool();
    }

    private void OnPause(InputAction.CallbackContext context)
    {
        Sprint(false);
        GameManager.Instance.PauseGame(false);
    }

    private void OnWheel(InputAction.CallbackContext context)
    {
        RoamToolsManager.Instance.ActivateToolsWheelUI(true);
    }

    private void OnMenu(InputAction.CallbackContext context)
    {
        PhoneMenu.Instance.ActivateGameMenu(true);
    }

    private void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Sprint(true);
        }
        else if (context.canceled)
        {
            Sprint(false);
        }
    }

    private void OnStealth(InputAction.CallbackContext context)
    {
        if (InStealth())
        {
            ExitStealth();
        }
        else
        {
            TryEnterStealth();
        }
    }

    public void ListenToInput(bool listen)
    {
        if (listen)
        {
            //Movement
            playerInput.actions.FindAction("Move").performed += OnMove;
            playerInput.actions.FindAction("Move").canceled += OnMove;

            playerInput.actions.FindAction("Sprint").performed += OnSprint;
            playerInput.actions.FindAction("Sprint").canceled += OnSprint;

            //Buttons
            playerInput.actions.FindAction("Stealth").performed += OnStealth;
            playerInput.actions.FindAction("Interact").performed += OnInteract;

            //Tools
            playerInput.actions.FindAction("Tool").performed += OnUseTool;

            //Menus
            playerInput.actions.FindAction("Pause").performed += OnPause;
            playerInput.actions.FindAction("Menu").performed += OnMenu;
            playerInput.actions.FindAction("Wheel").performed += OnWheel;

        }
        else
        {
            Sprint(false);

            //Movement
            playerInput.actions.FindAction("Move").performed -= OnMove;
            playerInput.actions.FindAction("Move").canceled -= OnMove;

            playerInput.actions.FindAction("Sprint").performed -= OnSprint;
            playerInput.actions.FindAction("Sprint").canceled -= OnSprint;

            //Buttons
            playerInput.actions.FindAction("Stealth").performed -= OnStealth;
            playerInput.actions.FindAction("Interact").performed -= OnInteract;

            //Tool Contols
            playerInput.actions.FindAction("Tool").performed -= OnUseTool;

            //Menus
            playerInput.actions.FindAction("Pause").performed -= OnPause;
            playerInput.actions.FindAction("Wheel").performed -= OnWheel;
            playerInput.actions.FindAction("Menu").performed -= OnMenu;
            
        }
    }

    private void OnDestroy()
    {
        ControlsManager.Instance.RemoveIControls(this);
    }
}
