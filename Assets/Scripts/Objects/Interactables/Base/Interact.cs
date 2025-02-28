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
        FantasyCombatManager.Instance.CombatBegun -= OnCombatBegin;
        FantasyCombatManager.Instance.CombatEnded -= OnCombatEnd;
    }

    //COMBAT
    protected void BeginCombatInteraction(List<GridUnit> targets)
    {
        CharacterGridUnit interactor = FantasyCombatManager.Instance.GetActiveUnit();

        interactor.unitAnimator.SetSpeed(0);

        //Warp Unit into Position & Rotation in an attempt to remove camera jitter.
        Vector3 desiredRotation = Quaternion.LookRotation(CombatFunctions.GetCardinalDirectionAsVector(interactor.transform)).eulerAngles;
        interactor.Warp(LevelGrid.Instance.gridSystem.GetWorldPosition(interactor.GetCurrentGridPositions()[0]), Quaternion.Euler(new Vector3(0, desiredRotation.y, 0)));

        GridSystemVisual.Instance.HideAllGridVisuals();

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
            EnableInteraction();
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
            EnableInteraction();
        }
    }

    protected void CombatInteractionComplete()
    {
        FantasyCombatManager.Instance.ActionComplete();
    }

    public virtual void ShowInteractUI(bool show)
    {
        if(show && !InteractionManager.Instance.showInteractCanvas) { return; }

        interactUI?.Fade(show);
    }

    protected void EnableInteraction()
    {
        if (!interactionCollider)
            SetCollider();

        interactionCollider.enabled = true;
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
        }
        else if(allowInteractionDuringFreeRoam)
        {
            return other.CompareTag("Player");
        }

        return false;
    }

    public virtual bool IsCorrectPositionAndRotation(Transform interactor)
    {
        return IsInteractorCorrectRotation(interactor);
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

        if (!FantasyCombatManager.Instance.InCombat())
        {
            OnCombatEnd(BattleResult.Victory, null); //To set collider enabled.
        }
    }
}
