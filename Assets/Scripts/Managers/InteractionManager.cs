using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.VisualScripting;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }

    [SerializeField] Interact currentAllowedInteraction;
    [Header("Values")]
    [Range(1, 90)]
    public float maxAngleBetweenInteractable = 30f;

    Transform interactor;
    List<Interact> currentActiveRadii = new List<Interact>();

    public bool enableInteraction { get; private set; }
    public bool PlayerInDanger { get; private set; }

    //Event
    public Func<bool, bool> HandleInteraction;
    public Action<bool> ShowInteractCanvas;


    private void Awake()
    {
        if (!Instance)
            Instance = this;

        enableInteraction = true;
    }

    private void OnEnable()
    {
        HandleInteraction += Interact;
        ShowInteractCanvas += SetEnableInteraction;

        PlayerStateMachine.PlayerInDanger += SetPlayerInDanger;
        PlayerStateMachine.PlayerWarped += OnPlayerWarped;
    }

    private void Update()
    {
        if (currentActiveRadii.Count > 0)
        {
            OnRadiusStay();
        }
    }

    private void OnPlayerWarped(PlayerBaseState newState)
    {
        currentActiveRadii.Clear();
        if (currentActiveRadii.Count == 0)
        {
            UpdateAllowedInteraction(currentAllowedInteraction, false);
        }
    }

    public bool CanInteractWith(CombatDirectInteractable interactable)
    {
        return interactable == currentAllowedInteraction;
    }

    private void SetEnableInteraction(bool enable)
    {
        enableInteraction = enable;
        currentAllowedInteraction?.ShowInteractUI(enable);
    }

    private void SetPlayerInDanger(bool inDanger)
    {
        PlayerInDanger = inDanger;
    }

    private void UpdateAllowedInteraction(Interact interactable, bool allowInteraction)
    {
        if (!enableInteraction) //Interaction banned
        {
            currentAllowedInteraction?.ShowInteractUI(false);
            currentAllowedInteraction = null;
        }
        else if (allowInteraction)
        {
            if (interactable != currentAllowedInteraction)
            {
                currentAllowedInteraction?.ShowInteractUI(false);
                currentAllowedInteraction = interactable;
                currentAllowedInteraction.ShowInteractUI(true);
            }
        }
        else if(!allowInteraction && interactable == currentAllowedInteraction)
        {
            currentAllowedInteraction?.ShowInteractUI(false);
            currentAllowedInteraction = null;
        }
    }

    public void OnRadiusEnter(Interact interactable, Transform interactor)
    {
        if (!currentActiveRadii.Contains(interactable))
        {
            this.interactor = interactor;
            currentActiveRadii.Add(interactable);
        }
    }

    public void OnRadiusStay()
    {
        List<Interact> allValidInteractions = new List<Interact>();

        foreach (Interact interactable in currentActiveRadii)
        {
            if (interactable.IsCorrectPositionAndRotation(interactor))
            {
                allValidInteractions.Add(interactable);
            }
        }

        if (allValidInteractions.Count == 0)
        {
            UpdateAllowedInteraction(currentAllowedInteraction, false);
        }
        else if(allValidInteractions.Count == 1)
        {
            UpdateAllowedInteraction(allValidInteractions[0], true);
        }
        else
        {
            UpdateAllowedInteraction(GetClosestInteractable(allValidInteractions), true);
        }
    }

    public void OnRadiusExit(Interact interactable)
    {
        if (currentActiveRadii.Contains(interactable))
        {
            currentActiveRadii.Remove(interactable);
        }

        if(currentActiveRadii.Count == 0)
        {
            UpdateAllowedInteraction(currentAllowedInteraction, false);
        }
    }

    public bool IsValidCombatInteraction()
    {
        return enableInteraction && currentAllowedInteraction != null;
    }

    private Interact GetClosestInteractable(List<Interact> interactables)
    {
        float closestDistance = Mathf.Infinity;
        Interact closestInteractable = null;

        foreach(Interact interactable in interactables)
        {
            float calculatedDistance = Vector3.Distance(interactable.transform.position, interactor.transform.position);
            if(calculatedDistance < closestDistance)
            {
                closestDistance = calculatedDistance;
                closestInteractable = interactable;
            }
        }

        return closestInteractable;
    }

    private bool Interact(bool inCombat)
    {
        if (currentAllowedInteraction)
        {
            currentAllowedInteraction.HandleInteraction(inCombat);
            return true;
        }

        return false;
    }

    private void OnDisable()
    {
        PlayerStateMachine.PlayerInDanger -= SetPlayerInDanger;
        HandleInteraction -= Interact;
        ShowInteractCanvas -= SetEnableInteraction;
        PlayerStateMachine.PlayerWarped -= OnPlayerWarped;
    }

}
