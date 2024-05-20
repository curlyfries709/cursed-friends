using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.AI;
using System;

public class SneakBarrel : MonoBehaviour
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
    bool startCalled = false;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStateMachine>();

        transform.parent = player.transform;

        transform.localPosition = idleEquip.localPosition;
    }

    private void OnEnable()
    {
        if (!startCalled) { return; }

        //Set Barrel Sneaking in Animator.
        player.animator.SetBool(player.animator.animIDBarrel, true);

        player.SetControllerConfig(navMeshObstacle.center, navMeshObstacle.radius, navMeshObstacle.height);

        atIdlePos = false;
        atMovePos = false;

        player.HideCompanions?.Invoke(true);
    }

    private void Start()
    {
        startCalled = true;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!player.InStealth())
        {
            gameObject.SetActive(false);
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


            transform.parent = player.animator.GetSpine();
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

    private void OnDisable()
    {
        inSafeZone = true;

        player.animator.SetBool(player.animator.animIDBarrel, false);

        player.HideCompanions?.Invoke(false);

        if (player.InStealth())
        {
            player.SetStealthControllerConfigValues();
        }

    }

    public void Toggle()
    {
        if (!player.InStealth()) { return; }

        gameObject.SetActive(!gameObject.activeInHierarchy);
    }

    public void ForcedRemove()
    {
        //Remove From Inventory
        gameObject.SetActive(false);
    }

}
