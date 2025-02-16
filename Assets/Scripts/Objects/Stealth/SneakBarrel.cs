using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.AI;
using System;


public class SneakBarrel : BaseTool
{
    [Header("Components")]
    [SerializeField] NavMeshObstacle navMeshObstacle;
    [Header("Transforms")]
    [SerializeField] Transform idleEquip;
    [SerializeField] Transform moveEquip;
    [Space(10)]
    public bool inSafeZone = true;

    PlayerStateMachine player;

    bool animating = false;
    bool atIdlePos = false;
    bool atMovePos = false;

    public override void Activate()
    {
        SetPlayer();
        transform.parent = player.transform;
        transform.localPosition = idleEquip.localPosition;

        atIdlePos = false;
        atMovePos = false;

        if (!player.InStealth() && !player.TryEnterStealth())
        {
            Debug.Log("SNEAK BARREL CANNOT FORCE STEALTH STATE");
            CancelUse();
            return;
        }

        //Set Barrel Sneaking in Animator.

        if (GetPlayerFantasyAnimator())
        {
            GetPlayerFantasyAnimator().SetBool(GetPlayerFantasyAnimator().animIDBarrel, true);
        }

        player.SetControllerConfig(navMeshObstacle.center, navMeshObstacle.radius, navMeshObstacle.height);


        player.HideCompanions?.Invoke(true);

        base.Activate();
    }

    public override void Use()
    {
        ToggleState();
    }

    public override void CancelUse()
    {
        if (!isActivated) return;

        Deactivate();
    }

    private void Update()
    {
        if (!player.InStealth())
        {
            CancelUse();
            return;
        }

        if (!animating && player.moveValue == Vector2.zero && !atIdlePos)
        {
            animating = true;
            atMovePos = false;


            transform.parent = player.transform;
            transform.localRotation = idleEquip.localRotation;
            transform.DOLocalMove(idleEquip.localPosition, player.SpeedChangeRate * Time.deltaTime).OnComplete(() => UpdateBarrel(true));
        }
        else if (!animating && player.moveValue != Vector2.zero && !atMovePos)
        {
            animating = true;
            atIdlePos = false;


            transform.parent = GetPlayerFantasyAnimator().GetSpine();
            transform.DOLocalRotate(moveEquip.localRotation.eulerAngles, player.SpeedChangeRate * Time.deltaTime);
            transform.DOLocalMove(moveEquip.localPosition, player.SpeedChangeRate * Time.deltaTime).OnComplete(() => UpdateBarrel(false));
        }
    }

    public bool IsSuspicious()
    {
        return !inSafeZone || player.moveValue != Vector2.zero;
    }

    private void UpdateBarrel(bool idle)
    {
        animating = false;

        if (idle)
        {
            atIdlePos = true;
        }
        else
        {
            atMovePos = true;
        }
    }


    public override void Deactivate()
    {
        SetPlayer();

        inSafeZone = false;

        GetPlayerFantasyAnimator().SetBool(GetPlayerFantasyAnimator().animIDBarrel, false);

        player.HideCompanions?.Invoke(false);

        if (player.InStealth())
        {
            player.SetStealthControllerConfigValues();
        }

        base.Deactivate();
    }

    public override void ToggleState()
    {
        base.ToggleState();
        gameObject.SetActive(isActivated);
    }

    public void ForcedRemove()
    {
        Debug.Log("UPDATE FORCE REMOVE FUNCTION");
        //Remove From Inventory
        CancelUse();
    }

    GridUnitAnimator GetPlayerFantasyAnimator()
    {
        return player.animator as GridUnitAnimator;
    }

    private void SetPlayer()
    {
        player = PlayerSpawnerManager.Instance.GetPlayerStateMachine();
    }

}
