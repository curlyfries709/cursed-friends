using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;
using Sirenix.OdinInspector;


public class EnemyStateMachine : CharacterStateMachine
{
    [Title("Movement")]
    public float walkSpeed = 4;
    public float normalRunSpeed = 7;
    [Tooltip("Acceleration and deceleration")]
    public float SpeedChangeRate = 10.0f;
    [Tooltip("How fast the character turns to face movement direction")]
    [Range(0.0f, 0.3f)]
    public float RotationSmoothTime = 0.12f;
    [Title("Patrol Behaviour")]
    public bool isHostile = true;
    public bool useAmbushTactics = false;
    [Tooltip("Enemy will remain hostile even during cinematic mode. Helpful for chest monsters")]
    public bool ignoreCinematicMode = false;
    [Tooltip("Should Enemy immediately spot player when in stealth")]
    public bool ignorePlayerStealth = false;
    [Space(5)]
    [Tooltip("If this enemy doesn't following a patrol path, which idle state should it assume?")]
    public EnemyIdleStateType idleStateType;
    [Space(5)]
    [SerializeField] float minIdleTime = 1f;
    [SerializeField] float maxIdleTime = 3f;
    [Title("Patrol Path")]
    [SerializeField] PatrolPathVisual patrolPath;
    [Title("Hostile Behaviour")]
    public bool canMoveWhilstAttacking = true;
    [Space(5)]
    public float minAttackRange = 2f;
    public float maxAttackRange = 2.4f;
    public float timeBetweenAttacks = 2f;
    [Space(5)]
    public float distanceToGiveUpChasing = 10f;
    public float chaseDestinationUpdateTime = 0.25f;
    public float maxTimeToIntercept = 10f;
    [Space(5)]
    public float attackHitBoxActivationTime = 0.35f;
    [Space(5)]
    public float alertAlliesRadius = 5f;
    [Title("Search Behaviour")]
    [SerializeField] float minLookTime = 3f;
    [SerializeField] float maxLookTime = 5f;
    [Space(5)]
    public float sussingRotateTime = 2f;
    [Tooltip("Time to detect player from 0 suss when their distance is equal to the view radius")]
    public float completeDetectionTime = 5f;

    [Title("GameObjects")]
    public SightDetectionUI sightDetectionUI;
    [Space(10)]
    [SerializeField] List<GameObject> hostileBehaviourComponents;
    [Space(5)]
    [SerializeField] List<GameObject> friendlyBehaviourComponents;
    [Space(5)]
    [SerializeField] List<GameObject> freeRoamComponents;

    //Patrol States
    public EnemyIdleState idleState { get; private set; }
    public EnemyPatrollingState patrollingState { get; private set; }
    public EnemyGuardPointState guardPointState { get; private set; }
    public EnemySleepingState sleepingState { get; private set; }

    //Alert States
    public EnemySussingState sussState { get; private set; }
    public EnemyInvestigateState investigateState { get; private set; }
    public EnemyChasingState chasingState { get; private set; }
    public EnemyAttackState attackState { get; private set; }
    public EnemyRetreatState retreatState { get; private set; }
    public EnemyAmbushState ambushState { get; private set; }

    //Fantasy Combat States
    public EnemyCombatWaitingState enemyCombatWaitingState { get; private set; }
    public EnemyCombatActingState enemyCombatActingState { get; private set; }

    //OTHER STATES
    public EnemyEmptyState emptyState { get; private set; }

    //Caches
    public CharacterGridUnit myGridUnit { get; private set; }
    public FantasyCombatAI enemyAI { get; private set; }

    public NavMeshAgent navMeshAgent { get; private set; }

    public GridUnitAnimator animator { get; private set; }

    public Transform patrolRoute { get; private set; }

    public FieldOfView fieldOfView { get; private set; }
    public SneakBarrel barrel { get; private set; }
    public PlayerIntiateCombat hostileInteraction { get; private set; }


    //Variables
    public float defaultStoppingDistance { get; private set; }

    public bool enableCinematicMode { get; private set; }

    public Transform assignedAmbushPoint { get; private set; }

    [HideInInspector] public EnemyGroup enemyGroup;
    [HideInInspector] public Vector3 playerLastKnownPos;
    [HideInInspector] public Quaternion playerLastKnownRot;
    [HideInInspector] public Vector3 ambushPos;

    [HideInInspector] public bool continuePatrol = false; //Used to decide whether to find closest Waypoint or simply continue Patrol
    [HideInInspector] public bool attemptingToIntercept = false;
    [HideInInspector] public bool knowsPlayerPos = false;


    //Combat Variables
    [HideInInspector] public bool canGoAgain = false;

    private static List<int> navMeshSelectedPriority = new List<int>();


    //Events
    public Action AttemptAmbush;
    public Action EnemyFantasyCombatActionComplete; //PURPOSLY CREATED SO THAT THIS CAN BE CALLED BEFFORE ON ACTION COMPLETE OR ISSUES OCCUR.
 

    public enum EnemyIdleStateType
    {
        Idle,
        Sleeping,
        Playing
    }


    private void Awake()
    {
        myGridUnit = GetComponent<CharacterGridUnit>();
        enemyAI = GetComponent<FantasyCombatAI>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<GridUnitAnimator>();
        fieldOfView = GetComponent<FieldOfView>();
        hostileInteraction = hostileBehaviourComponents[0].GetComponent<PlayerIntiateCombat>();

        //barrel = GameObject.FindGameObjectWithTag("Barrel").GetComponentInParent<SneakBarrel>();

        if (patrolPath)
        {
            patrolRoute = patrolPath.transform;
        }

        defaultStoppingDistance = navMeshAgent.stoppingDistance;

        IntializeStates();

        //SetPriority();
    }

    private void OnEnable()
    {
        EnemyFantasyCombatActionComplete += OnFantasyCombatActionComplete;

        StoryManager.Instance.ActivateCinematicMode += SwitchToCinematicMode;
        CinematicManager.Instance.CinematicBegun += WarpBackToPatrol;
        SavingLoadingManager.Instance.ReturnToDefaultPosition += WarpBackToPatrol;
    }

    private void Start()
    {
        if (IsInCombatState()) { return; }
        SetStartState();
    }

    private void SetStartState()
    {
        SetHostileComponents(isHostile);

        if (!patrolPath || patrolRoute.childCount == 1)
        {
            SwitchState(guardPointState);
        }
        else
        {
            SwitchState(patrollingState);
        }
    }

    public void SetHostileComponents(bool isHostile)
    {
        this.isHostile = isHostile;

        foreach(GameObject GO in friendlyBehaviourComponents)
        {
            GO.SetActive(!isHostile);
        }

        foreach (GameObject GO in hostileBehaviourComponents)
        {
            GO.SetActive(isHostile);
        }
    }

    private void SwitchToCinematicMode(bool enableCinematicMode)
    {
        this.enableCinematicMode = enableCinematicMode;
    }


    public void Alert(Vector3 playerSeenAt, Quaternion playerLastRot)
    {
        if (IsInCombatState() || !IsHostile()) { return; }

        if (FantasyCombatManager.Instance.InCombat())
        {
            JoinBattle();
        }
        else if (currentState == ambushState)
        {
            AttemptAmbush?.Invoke();
        }
        else if ((currentState != chasingState) && (currentState != attackState))
        {
            attemptingToIntercept = true;

            sightDetectionUI.Alert();

            playerLastKnownPos = playerSeenAt;
            playerLastKnownRot = playerLastRot;

            SwitchState(chasingState);
        }
    }

    public void JoinBattle()
    {
        sightDetectionUI.Alert();
        BattleEnlister.Instance.JoinBattle(this, true);
    }


    public void WarpBackToPatrol()
    {
        if(enemyGroup && enemyGroup.isFixedBattleGroup)
        {
            GetComponent<CharacterGridUnit>().ActivateUnit(false);
            enemyGroup.WarpFixedBattleParticipant(this);

            return;
        }

        if (!InPatrolState())
        {
            navMeshAgent.Warp(patrolRoute.GetChild(0).position);
            SetStartState();
        }
    }

    public void GoToAmbushPoint(Transform ambushPoint, Vector3 ambushPos)
    {
        this.ambushPos = ambushPos;
        assignedAmbushPoint = ambushPoint;
        SwitchState(ambushState);
    }



    public bool IsHostile()
    {
        if (ignoreCinematicMode)
        {
            return isHostile;
        }

        return isHostile && !enableCinematicMode;
    }

    private void OnDisable()
    {
        EnemyFantasyCombatActionComplete -= OnFantasyCombatActionComplete;

        CinematicManager.Instance.CinematicBegun -= WarpBackToPatrol;
        StoryManager.Instance.ActivateCinematicMode -= SwitchToCinematicMode;
        SavingLoadingManager.Instance.ReturnToDefaultPosition -= WarpBackToPatrol;

        EnemyTacticsManager.Instance.IsChasingPlayer(this, false);
    }

    public bool CanAmbushPlayer()
    {
        //If Enemy doesn't use ambush tactics, they can't ambush.
        return currentState == chasingState && useAmbushTactics;
    }

    public override void BeginCombat()
    {
        enemyAI.OnCombatBegin();
        SwitchState(enemyCombatWaitingState);

        foreach(GameObject obj in freeRoamComponents)
        {
            obj.SetActive(false);
        }
    }

    public void FantasyCombatGoAgain()
    {
        canGoAgain = true;
        SwitchState(enemyCombatActingState);
    }

    private void OnFantasyCombatActionComplete()
    {
        SwitchState(enemyCombatWaitingState);
    }

    public bool IsInCombatState()
    {
        return currentState == enemyCombatWaitingState || currentState == enemyCombatActingState || currentState == emptyState;
    }

    private bool InPatrolState()
    {
        return currentState == patrollingState || currentState == guardPointState || currentState == sleepingState || currentState == idleState;
    }

    public float GetRandomIdleTime()
    {
        return UnityEngine.Random.Range(minIdleTime, maxIdleTime);
    }

    public float GetRandomLookTime()
    {
        return UnityEngine.Random.Range(minLookTime, maxLookTime);
    }

    public PlayerStateMachine GetPlayerStateMachine()
    {
        return PlayerSpawnerManager.Instance.GetPlayerStateMachine();
    }

    public float ChaseSpeed()
    {
        return normalRunSpeed * GameManager.Instance.GetDifficultyChaseSpeedMultiplier();
    }

    public override void ShowWeapon(bool show)
    {
        animator.ShowWeapon(show);
    }

    public bool IsReadyToChase()
    {
        return !animator.ShouldDrawWeapon();
    }

    public bool HasWeapon()
    {
        return animator.HasWeapon();
    }

    private void IntializeStates()
    {
        enemyCombatWaitingState = new EnemyCombatWaitingState(this);
        enemyCombatActingState = new EnemyCombatActingState(this);
        patrollingState = new EnemyPatrollingState(this);
        idleState = new EnemyIdleState(this);
        guardPointState = new EnemyGuardPointState(this);
        chasingState = new EnemyChasingState(this);
        sussState = new EnemySussingState(this);
        retreatState = new EnemyRetreatState(this);
        attackState = new EnemyAttackState(this);
        investigateState = new EnemyInvestigateState(this);
        ambushState = new EnemyAmbushState(this);
        emptyState = new EnemyEmptyState(this);
        sleepingState = new EnemySleepingState(this);
    }

    /*private void SetPriority()
    {
        int randNum = UnityEngine.Random.Range(0, 51);

        while (navMeshSelectedPriority.Contains(randNum))
        {
            randNum = UnityEngine.Random.Range(0, 51);
        }

        navMeshAgent.avoidancePriority = randNum;
        navMeshSelectedPriority.Add(randNum);
    }*/

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(transform.position, alertAlliesRadius);
    }
}
