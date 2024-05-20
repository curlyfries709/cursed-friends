using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using MoreMountains.Feedbacks;
using System;
using Sirenix.OdinInspector;

public class PlayerStateMachine : StateMachine, IControls
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
    [SerializeField] GameObject fwRoamCam;
    public GameObject CinemachineCameraTarget;
    [Space(5)]
    [SerializeField] BoxCollider gridCollider;
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
    public Vector2 lookValue { get; private set; }
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
    public GridUnitAnimator animator { get; private set; }

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

        animator = GetComponentInChildren<GridUnitAnimator>();
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
        if (FantasyCombatManager.Instance.InCombat()) { return; }

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

        ActivateFreeRoamCam(false);
        controller.enabled = false;
        transform.position = newPosition;
        transform.rotation = newRotation;
        controller.enabled = true;
        ActivateFreeRoamCam(true);

        if (state != null)
            SwitchState(state);

        if (autoWarpCompanionsNearPos)
            PlayerWarped?.Invoke(state);
    }

    public void WarpPlayer(Transform newTransform, PlayerBaseState newPlayerState, bool autoWarpCompanionsNearPos)
    {
        ActivateFreeRoamCam(false);
        controller.enabled = false;
        transform.position = newTransform.position;
        transform.rotation = newTransform.rotation;
        controller.enabled = true;
        ActivateFreeRoamCam(true);

        if (newPlayerState != null)
            SwitchState(newPlayerState);

        if (autoWarpCompanionsNearPos)
            PlayerWarped?.Invoke(newPlayerState);
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

        gridCollider.enabled = !activate;
    }

    public void ActivateFreeRoamCam(bool activate)
    {
        fwRoamCam.gameObject.SetActive(activate);
    }

    public override void ShowWeapon(bool show)
    {
        animator.ShowWeapon(show);
    }
    public void SetControllerConfig(Vector3 center, float radius, float height)
    {
        controller.center = center;
        controller.radius = radius;
        controller.height = height;
    }

    public void SetProximityRadius(bool inCombat)
    {
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


    //Inputs
    private void OnMove(InputAction.CallbackContext context)
    {
        if (context.action.name != "Move") { return; }
        moveValue = context.ReadValue<Vector2>();
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        if (context.action.name != "Look") { return; }

        lookValue = context.ReadValue<Vector2>();
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (context.action.name != "Interact") { return; }

        if (context.performed)
        {
            InteractionManager.Instance.HandleInteraction(false);
        }
    }

    private void OnPause(InputAction.CallbackContext context)
    {
        if (context.action.name != "Pause") { return; }

        if (context.performed)
        {
            Sprint(false);
            GameManager.Instance.PauseGame(false);
        }
    }

 
    private void OnMenu(InputAction.CallbackContext context)
    {
        if (context.action.name != "Menu") { return; }

        if (context.performed)
        {
            PhoneMenu.Instance.ActivateGameMenu(true);
        }
    }

    private void OnSprint(InputAction.CallbackContext context)
    {
        if (context.action.name != "Sprint") { return; }
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
        if (context.action.name != "Stealth") { return; }

        if (context.performed && allowStealthMode)
        {
            if(currentState == fantasyRoamState)
            {
                SwitchState(stealthState);
            }
            else
            {
                SwitchState(fantasyRoamState);
            }
        }
    }

    public void ListenToInput(bool listen)
    {
        if (listen)
        {
            playerInput.onActionTriggered += OnMove;
            playerInput.onActionTriggered += OnLook;
            playerInput.onActionTriggered += OnSprint;
            playerInput.onActionTriggered += OnStealth;
            playerInput.onActionTriggered += OnInteract;
            playerInput.onActionTriggered += OnPause;
            playerInput.onActionTriggered += OnMenu;
        }
        else
        {
            Sprint(false);

            playerInput.onActionTriggered -= OnMove;
            playerInput.onActionTriggered -= OnLook;
            playerInput.onActionTriggered -= OnSprint;
            playerInput.onActionTriggered -= OnStealth;
            playerInput.onActionTriggered -= OnInteract;
            playerInput.onActionTriggered -= OnPause;
            playerInput.onActionTriggered -= OnMenu;
        }
    }

    private void OnDestroy()
    {
        ControlsManager.Instance.RemoveIControls(this);
    }
}
