using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using DG.Tweening;
using Cinemachine;


public class CharacterGridUnit : GridUnit, IHighlightable
{
    [Header("Character Profile")]
    public Sprite portrait;
    [Space(10)]
    [Tooltip("What team the unit belongs to. To distinguish which enemies will attack each other. ")]
    public CombatTeam team;
    [Header("Movement")]
    [SerializeField] int moveRange;
    public float moveSpeed;
    [Range(0.1f, 1f)]
    [SerializeField] float returnToGridPosJumpHeight = 0.25f;
    [Space(10)]
    [SerializeField] float guardRotationSpeed = 5f;
    [Header("Character Unit Components")]
    public GridUnitAnimator unitAnimator;
    [Space(10)]
    public CounterAttack counterAttack;
    [Header("Cam Follow Targets")]
    public Transform statusEffectCamTarget;
    public Transform counterCamTarget;
    [Header("Visuals")]
    [SerializeField] PhotoshootAnimator photoshootAnimator;
    [SerializeField] GameObject followCam;
    [Space(5)]
    public GameObject analysisCam;
    public GameObject koCam;

    //Variables
    [HideInInspector] public float delayBeforeReturn;
    [HideInInspector] public float returnToGridPosTime;

    //Caches
    protected CinemachineFreeLook freeLookPlayerCam;
    public bool hasUsedTacticThisTurn { get; protected set; } = false;

    //Events
    public Action BeginTurn;
    public Func<bool> CanTriggerSkill;
    public Action EndTurn;

    protected override void Awake()
    {
        base.Awake();
        followCam.SetActive(false);
        freeLookPlayerCam = followCam.GetComponent<CinemachineFreeLook>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        BeginTurn += OnBeginTurn;
    }

    protected virtual void OnBeginTurn()
    {
        CharacterHealth().Guard(false);
        hasUsedTacticThisTurn = false;
    }

    private void Update()
    {
        if (CharacterHealth().isGuarding)
        {
            CharacterGridUnit activeUnit = FantasyCombatManager.Instance.GetActiveUnit();

            if(activeUnit.team != team)
            {
                //transform.LookAt(activeUnit.transform.position);
                Quaternion lookAt = Quaternion.LookRotation(activeUnit.transform.position - transform.position);
                transform.rotation = Quaternion.Lerp(transform.rotation, lookAt, guardRotationSpeed * Time.deltaTime);
            }
        }
    }

    protected virtual void OnDisable()
    {
        BeginTurn -= OnBeginTurn;
    }

    public void ReturnToPosAfterAttack(bool warpToPos)
    {
        unitAnimator.ReturnToNormalSpeed();

        if (warpToPos)
        {
            Warp(LevelGrid.Instance.gridSystem.GetWorldPosition(GetGridPositionsOnTurnStart()[0]), transform.rotation);
        }
        else if(!IsAlreadyAtCorrectPos())
        {
            //Only do this if not already at pos.
            //unitAnimator.Idle();
            Invoke("ReturnToCorrectPos", delayBeforeReturn);
        }

        
        if (Mathf.Abs(transform.rotation.eulerAngles.y % 90) != 0)
        {
            //Correct Rotation to be multiple of 90
            float newRotationY = Mathf.Round(transform.rotation.eulerAngles.y / 90) * 90;
            Vector3 newRotation = new Vector3(0, newRotationY, 0);
            transform.DORotate(newRotation, 0.1f);
        }
    }

    public void ReturnToCorrectPos()
    {
        //transform.DOMove(LevelGrid.Instance.gridSystem.GetWorldPosition(GetGridPositionsOnTurnStart()[0]), returnToGridPosTime);
        transform.DOJump(LevelGrid.Instance.gridSystem.GetWorldPosition(GetGridPositionsOnTurnStart()[0]), returnToGridPosJumpHeight, 1, returnToGridPosTime);
    }

    public void ReturnToPosAfterEvade(float returnTime)
    {
        unitAnimator.SetTrigger(unitAnimator.animIDEvadeReturn);
        transform.DOMove(LevelGrid.Instance.gridSystem.GetWorldPosition(GetGridPositionsOnTurnStart()[0]), returnTime).OnComplete(()=> unitAnimator.Idle());
    }

    public bool IsAlreadyAtCorrectPos()
    {
        return Vector3.Distance(transform.position, LevelGrid.Instance.gridSystem.GetWorldPosition(GetGridPositionsOnTurnStart()[0])) < 0.1f;
    }

    //Setters
    public override void ShowModel(bool show)
    {
        unitAnimator.ShowModel(show);
    }
    public void ActivateFollowCam(bool activate)
    {
        followCam.SetActive(activate);
    }

    public Dictionary<GridPosition, IHighlightable> ActivateHighlightedUI(bool activate, PlayerBaseSkill selectedBySkill)
    {
        myHealth.ActivateHealthVisual(activate);
        return null;
    }

    public void UsedTactic()
    {
        hasUsedTacticThisTurn = true;
    }

    protected override void SetHighlightable()
    {
        myHighlightable = this;
    }

    //Getters
    public int MoveRange()
    {
        //If Overburdened return 1
        if (StatusEffectManager.Instance.HasReducedMovementDueToStatusEffect(this))
        {
            return 1;
        }

        return moveRange + stats.MovementBuff();
    }

    public PhotoshootAnimator GetPhotoShootSet()
    {
        return photoshootAnimator;
    }

    public CharacterHealth CharacterHealth()
    {
        return myHealth as CharacterHealth;
    }

    public GridUnit GetGridUnit()
    {
        return this;
    }

}
