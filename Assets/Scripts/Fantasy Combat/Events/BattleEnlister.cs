using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;
using AnotherRealm;
using Cinemachine;
using TMPro;

public class BattleEnlister : CombatAction, ITurnEndEvent
{
    public static BattleEnlister Instance { get; private set; }

    [Header("Timers & Values")]
    [SerializeField] float unitMoveToGridPosTime = 0.25f;
    [SerializeField] float unitJumpPower = 2;
    [Space(10)]
    [SerializeField] float unitRotateTime = 0.15f;
    [Space(10)]
    [SerializeField] float displayUnitTime = 0.5f;
    [Header("Cameras")]
    [SerializeField] CinemachineVirtualCamera cam;
    [Space(10)]
    [SerializeField] float orbitPointRotationSpeed;
    [SerializeField] Vector3 orbitPointStartingRotation;
    [Header("UI")]
    [SerializeField] FadeUI canvas;
    [SerializeField] TextMeshProUGUI canvasMessage;
    [SerializeField] string messageAppendText;

    Transform orbitPoint;

    List<GridPosition> takenGridPositions = new List<GridPosition>();
    List<CharacterGridUnit> newParticpants = new List<CharacterGridUnit>(); 

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        FantasyCombatManager.Instance.CombatBegun += OnCombatBegin;
    }

    private void Update()
    {
        if (cam.gameObject.activeInHierarchy)
        {
            orbitPoint.Rotate(Vector3.down * orbitPointRotationSpeed * Time.deltaTime);
        }
    }

    public void PlayTurnEndEvent()
    {
        BeginAction();
        takenGridPositions.Clear();

        List<GridUnit> unit = new List<GridUnit>
        {
            newParticpants[0]
        };

        FantasyCombatManager.Instance.SetUnitsToShow(unit);
        DisplayNewParticipant(newParticpants[0]);
        newParticpants.RemoveAt(0);
    }

    public void DisplayNewParticipant(CharacterGridUnit unit)
    {
        orbitPoint = unit.statusEffectCamTarget;

        cam.Follow = unit.statusEffectCamTarget;
        cam.LookAt = unit.statusEffectCamTarget;

        orbitPoint.localRotation = Quaternion.Euler(orbitPointStartingRotation);
        canvasMessage.text = EnemyDatabase.Instance.GetEnemyDisplayName(unit, unit.stats.data) + " " + messageAppendText;

        StartCoroutine(ShowUnitRoutine());
    }

    private IEnumerator ShowUnitRoutine()
    {
        cam.gameObject.SetActive(true);
        canvas.Fade(true);
        yield return new WaitForSeconds(displayUnitTime - canvas.fadeOutTime);
        canvas.Fade(false);
        yield return new WaitForSeconds(canvas.fadeOutTime);
        cam.gameObject.SetActive(false);
        FantasyCombatManager.Instance.ResetUnitsToShow();
        EndAction();
    }

    //LOGIC

    public void JoinBattle(EnemyStateMachine enemy, bool isPatrolling)
    {
        enemy.BeginCombat();

        EnemyTacticsManager.Instance.IsChasingPlayer(enemy, false);
        CharacterGridUnit enemyUnit = enemy.GetComponent<CharacterGridUnit>();

        //Add Unit To Battle.
        FantasyCombatManager.Instance.AddNewUnitDuringBattle(enemyUnit, !isPatrolling);

        if (isPatrolling)
        {
            FantasyCombatManager.Instance.AddTurnEndEventToQueue(this);
            newParticpants.Add(enemyUnit);
        }

        //Set Grid Pos
        GridPosition targetGridPos = BattleStarter.Instance.FindSuitableBattleStartGridPos(LevelGrid.Instance.gridSystem.GetGridPosition(enemy.transform.position), takenGridPositions);
        takenGridPositions.Add(targetGridPos);

        //Move To Grid Pos
        Vector3 worldPos = LevelGrid.Instance.gridSystem.GetWorldPosition(targetGridPos);

        Vector3 lookDirection = CombatFunctions.GetCardinalDirectionAsVector(enemy.transform);
        Quaternion lookRotation = Quaternion.LookRotation(lookDirection);

        enemy.transform.DORotate(lookRotation.eulerAngles, unitRotateTime);
        enemy.transform.DOJump(worldPos, unitJumpPower, 1, unitMoveToGridPosTime).OnComplete(() => enemyUnit.SetGridPositions());
    }

    private void OnCombatBegin(BattleStarter.CombatAdvantage advantage)
    {
        newParticpants.Clear();
    }
    private void OnDisable()
    {
        FantasyCombatManager.Instance.CombatBegun -= OnCombatBegin;
    }

    public void OnEventCancelled()
    {
        //Event Can't Be Cancelled.
    }

    public float GetTurnEndEventOrder()
    {
        return transform.GetSiblingIndex();
    }

    public List<Type> GetEventTypesThatCancelThis()
    {
        //Event Can't be Cancelled.
        return new List<Type>();
    }

    protected override bool ListenForUnitHealthUIComplete()
    {
        return false;
    }
}
