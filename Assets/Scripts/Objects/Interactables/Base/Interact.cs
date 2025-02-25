using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using AnotherRealm;
using Sirenix.OdinInspector;
using System;

[RequireComponent(typeof(BoxCollider))]
public abstract class Interact : MonoBehaviour
{
    [Header("Interaction Type")]
    [Tooltip("If Player being chased by enemy, should interaction be allowed?")]
    [SerializeField] protected bool allowInteractionWhilePlayerInDanger = false;
    [Space(10)]
    [SerializeField] protected bool allowInteractionDuringFreeRoam = true;
    [SerializeField] protected bool allowInteractionDuringCombat = true;
    [Space(10)]
    [SerializeField] protected bool interactorMustFaceMyForward = false;
    [Header("UI")]
    [SerializeField] protected FadeUI interactUI;
    [Space(10)]
    [ShowIf("interactUI")]
    [SerializeField] protected string objectTitleText;
    [ShowIf("interactUI")]
    [SerializeField] protected string interactTitleText;

    //Caches
    TextMeshProUGUI objectTitle;
    TextMeshProUGUI interactTitle;

    protected Collider interactionCollider;

    public abstract void HandleInteraction(bool inCombat);

    protected virtual void Start()
    {
        SetInteractCanvasText();

        SetCollider();
    }

    protected virtual void OnEnable()
    {
        FantasyCombatManager.Instance.CombatBegun += OnCombatBegin;
        FantasyCombatManager.Instance.CombatEnded += OnCombatEnd;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (TriggerCondition(other))
        {
            InteractionManager.Instance.OnRadiusEnter(this, other.transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (TriggerCondition(other))
        {
            InteractionManager.Instance.OnRadiusExit(this);
        }
    }

    protected virtual void OnDisable()
    {
        InteractionManager.Instance.OnRadiusExit(this);
    }

    //COMBAT
    protected void BeginCombatInteraction(List<GridUnit> targets)
    {
        CharacterGridUnit interactor = FantasyCombatManager.Instance.GetActiveUnit();

        interactor.unitAnimator.SetSpeed(0);

        //Warp Unit into Position & Rotation in an attempt to remove camera jitter.
        Vector3 desiredRotation = Quaternion.LookRotation(CombatFunctions.GetCardinalDirectionAsVector(interactor.transform)).eulerAngles;
        interactor.Warp(LevelGrid.Instance.gridSystem.GetWorldPosition(interactor.GetCurrentGridPositions()[0]), Quaternion.Euler(new Vector3(0, desiredRotation.y, 0)));

        GridSystemVisual.Instance.HideAllGridVisuals(true);

        List<GridUnit> targetedUnits = new List<GridUnit>(targets)
        {
            interactor
        };

        FantasyCombatManager.Instance.SetUnitsToShow(targetedUnits);

        //UpdatePosition
        interactor.MovedToNewGridPos();
    }

    private void OnCombatBegin(BattleStarter.CombatAdvantage advantage)
    {
        if (allowInteractionDuringCombat && !allowInteractionDuringFreeRoam) //Combat only interactable
        {
            interactionCollider.enabled = true;
        }
        else if(allowInteractionDuringFreeRoam && !allowInteractionDuringCombat) //Roam only interactable
        {
            DisableInteraction();
        }
    }

    private void OnCombatEnd(BattleResult result, IBattleTrigger trigger)
    {
        if(result == BattleResult.Defeat || result == BattleResult.Restart) { return; } 

        if (allowInteractionDuringCombat && !allowInteractionDuringFreeRoam) //Combat only interactable
        {
            DisableInteraction();
        }
        else if (allowInteractionDuringFreeRoam && !allowInteractionDuringCombat) //Roam only interactable
        {
            interactionCollider.enabled = true;
        }
    }

    protected void CombatInteractionComplete()
    {
        FantasyCombatManager.Instance.ActionComplete();
    }

    public void ShowInteractUI(bool show)
    {
        if(show && !InteractionManager.Instance.showInteractCanvas) { return; }

        interactUI.Fade(show);
    }

    protected void DisableInteraction()
    {
        ShowInteractUI(false);
        InteractionManager.Instance.OnRadiusExit(this);

        if (!interactionCollider)
            SetCollider();

        interactionCollider.enabled = false;
    }

    protected bool TriggerCondition(Collider other)
    {
        if (!allowInteractionWhilePlayerInDanger && InteractionManager.Instance.PlayerInDanger)
        {
            return false;
        }

        if (FantasyCombatManager.Instance.InCombat() && allowInteractionDuringCombat)
        {
            if(other.TryGetComponent(out PlayerGridUnit interactor))
            {
                return interactor == FantasyCombatManager.Instance.GetActiveUnit();
            }

            return false;
        }
        else if(allowInteractionDuringFreeRoam)
        {
            return other.CompareTag("Player");
        }

        return false;
    }

    public bool IsCorrectPositionAndRotation(Transform interactor)
    {
        if (FantasyCombatManager.Instance.InCombat())
        {
            return IsInteractorCorrectRotation(interactor) && IsInteractorCorrectGridPos(interactor);
        }
        else
        {
            return IsInteractorCorrectRotation(interactor);
        }
    }

    private bool IsInteractorCorrectGridPos(Transform interactor)
    {
        CharacterGridUnit unit = interactor.GetComponent<CharacterGridUnit>();
        List<GridPosition> currentGridPos = unit.GetCurrentGridPositions();

        return GetValidInteractionGridPositions().Intersect(currentGridPos).Any();
    }

    protected virtual bool IsInteractorCorrectRotation(Transform interactor)
    {
        float angle;

        if (interactorMustFaceMyForward)
        {
            angle = Vector3.Angle(-interactor.forward, transform.forward);
        }
        else
        {
            Vector3 direction = (transform.position - interactor.position).normalized;
            angle = Vector3.Angle(interactor.forward, direction);
        }


        return angle <= InteractionManager.Instance.maxAngleBetweenInteractable;
    }


    protected List<GridPosition> GetValidInteractionGridPositions()
    {
        List<GridPosition> validGridPositionsList = new List<GridPosition>();
        //List<GridPosition> interactableGridPositions = unit.GetGridPositionsOnTurnStart();

        //int unitMaxMoveDistance = 1;

       /* for (int x = -unitMaxMoveDistance; x <= unitMaxMoveDistance; x++)
        {
            for (int z = -unitMaxMoveDistance; z <= unitMaxMoveDistance; z++)
            {
                GridPosition offsetGridPosition = new GridPosition(x, z);

                foreach (GridPosition unitGridPosition in interactableGridPositions)
                {
                    GridPosition testGridPosition = unitGridPosition + offsetGridPosition;
                    if (!gridSystem.IsValidGridPosition(testGridPosition))
                    {
                        continue;
                    }

                    GridObject gridObject = gridSystem.GetGridObject(testGridPosition);

                    if (PathFinding.Instance.IsGridPositionOccupiedByAnotherUnit(testGridPosition, unit))
                    {
                        continue;
                    }

                    if (!PathFinding.Instance.IsWalkableGridPosition(testGridPosition))
                    {
                        continue;
                    }

                    if (!PathFinding.Instance.HasPath(unitGridPosition, testGridPosition, unit, true))
                    {
                        continue;
                    }

                    int pathFindingDistanceMultiplier = PathFinding.Instance.GetPathFindingDistanceMultiplier();
                    if (PathFinding.Instance.GetPathLength(unitGridPosition, testGridPosition, unit, true) > unitMaxMoveDistance * pathFindingDistanceMultiplier)
                    {
                        //Path Lenghth is too long
                        continue;
                    }

                    //Calculate Manhattan distance
                    //abs(x1 - x2) + abs(y1 - y2)

                    if (Mathf.Abs(unitGridPosition.x - testGridPosition.x) + Mathf.Abs(unitGridPosition.z - testGridPosition.z) <= unitMaxMoveDistance)
                    {
                        if (!validGridPositionsList.Contains(testGridPosition))
                            validGridPositionsList.Add(testGridPosition);
                    }
                }
            }
        }*/

        return validGridPositionsList;
    }

    private void SetInteractCanvasText()
    {
        if (!interactUI) 
        {
            if (allowInteractionDuringFreeRoam)
            {
                Debug.LogError(transform.name + " doesn't have an interact UI setup!");
            }
            return; 
        } 

        objectTitle =  interactUI.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        interactTitle = interactUI.transform.GetChild(1).GetComponent<TextMeshProUGUI>();

        objectTitle.text = objectTitleText;
        interactTitle.text = interactTitleText;
    }

    protected void SetCollider()
    {
        interactionCollider = GetComponent<Collider>();
    }
}
