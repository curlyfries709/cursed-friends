using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CompanionStateMachine : CharacterStateMachine
{
    // Variables
    [HideInInspector] public float animationBlend;
    [HideInInspector] public float targetRotation = 0.0f;
    [HideInInspector] public float rotationVelocity;
    [HideInInspector] public float verticalVelocity;
    [HideInInspector] public float terminalVelocity = 53.0f;

    //Speeds
    public float walkSpeed { get; private set; }
    public float runSpeed { get; private set; }
    public float sneakSpeed { get; private set; }

    //Follow Behaviour Variables
    [HideInInspector] public CompanionFollowBehaviour followBehaviour { get; private set; }

    [HideInInspector] public float horizontalFollowOffset = 0;
    [HideInInspector] public float verticalFollowOffset = 0;
    [HideInInspector] public Vector3 previousCCDir = Vector3.zero;

    [HideInInspector] public bool raiseSwapPosEventDesignee = false;

    //Caches
    public CharacterAnimator animator { get; private set; }
    public Transform player { get; private set; }
    public PlayerStateMachine playerStateMachine { get; private set; }
    public NavMeshAgent navMeshAgent { get; private set; }
    public CharacterController controller { get; private set; }
    public MovementRestrictor moveRestrictor { get; private set; }

    //States
    public CompanionFantasyCombatState fantasyCombatState { get; private set; }
    public CompanionFollowState followState { get; private set; }
    public CompanionIdleState idleState { get; private set; }
    public CompanionStealthFollowState stealthFollowState { get; private set; }
    public CompanionStealthIdleState stealthIdleState { get; private set; }


    private void Awake()
    {
        playerStateMachine = PlayerSpawnerManager.Instance.GetPlayerStateMachine();
        player = playerStateMachine.transform;

        animator = GetComponentInChildren<GridUnitAnimator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        controller = GetComponent<CharacterController>();
        moveRestrictor = GetComponent<MovementRestrictor>();

        IntializeStates();
        SetSpeeds();
    }

    private void OnEnable()
    {
        playerStateMachine.SwitchToStealth += SwitchToStealth;
        playerStateMachine.HideCompanions += HideModel;
        PlayerStateMachine.PlayerWarped += OnPlayerWarp;
    }
    
    private void Start()
    {
        //SetStartState();
    }

    private void SetStartState()
    {
        if (FantasyCombatManager.Instance.InCombat()) { return; }

        SwitchState(idleState);
    }


    public override void BeginCombat()
    {
        SwitchState(fantasyCombatState);
    }

    private void OnPlayerWarp(PlayerBaseState newPlayerState)
    {
        Vector3 newPosition = player.position + (player.right.normalized * horizontalFollowOffset)
            + (player.forward.normalized * verticalFollowOffset);

        WarpToPosition(newPosition, transform.rotation);
        SwitchState(GetNewStateFromPlayerState(newPlayerState));
    }

    public override void WarpToPosition(Vector3 newPosition, Quaternion newRotationn)
    {
        if (navMeshAgent.enabled)
        {
            navMeshAgent.Warp(newPosition);
        }
        else
        {
            transform.position = newPosition;
            transform.rotation = newRotationn;
        }
    }

    private CompanionBaseState GetNewStateFromPlayerState(PlayerBaseState playerState)
    {
        switch (playerState)
        {
            case PlayerFantasyCombatState:
                return fantasyCombatState;
            case PlayerStealthState:
                return stealthFollowState;
            default:
                return followState;
        }
    }

    private void SwitchToStealth(bool enterStealth)
    {
        if (enterStealth)
        {
            if (currentState == idleState)
            {
                SwitchState(stealthIdleState);
            }
            else
            {
                //SwitchState(stealthFollowState);
                SwitchState(stealthIdleState);
            }
        }
        else if(currentState != fantasyCombatState)
        {
            SwitchState(followState);
        }
    }


    public void SetupFollowBehaviour(CompanionFollowBehaviour companionFollowBehaviour)
    {
        followBehaviour = companionFollowBehaviour;
        SetStartState();
    }

    public override void ShowWeapon(bool show)
    {
        (animator as GridUnitAnimator).ShowWeapon(show);
    }

    private void OnDisable()
    {
        playerStateMachine.SwitchToStealth -= SwitchToStealth;
        playerStateMachine.HideCompanions -= HideModel;
        PlayerStateMachine.PlayerWarped -= OnPlayerWarp;
    }

    private void HideModel(bool hide)
    {
        animator.ShowModel(!hide);
    }

    private void IntializeStates()
    {
        moveRestrictor.enabled = false;

        fantasyCombatState = new CompanionFantasyCombatState(this);
        idleState = new CompanionIdleState(this);
        followState = new CompanionFollowState(this);
        stealthFollowState = new CompanionStealthFollowState(this);
        stealthIdleState = new CompanionStealthIdleState(this);
    }

    private void SetSpeeds()
    {
        walkSpeed = playerStateMachine.walkSpeed;
        runSpeed = playerStateMachine.runSpeed;
        sneakSpeed = playerStateMachine.sneakSpeed;
    }
}
