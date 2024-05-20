using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProximityRadius : MonoBehaviour
{
    EnemyStateMachine myEnemyStateMachine;
    PlayerStateMachine myPlayerstatemachine;

    bool isPlayer;
    bool isCompanion;
    bool isPlayerSprinting;

    private void Awake()
    {
        myEnemyStateMachine = GetComponentInParent<EnemyStateMachine>();
        myPlayerstatemachine = GetComponentInParent<PlayerStateMachine>();

        isPlayer = myPlayerstatemachine;
        isCompanion = transform.parent.CompareTag("Companion");
    }

    private void OnEnable()
    {
        if (isPlayer)
            myPlayerstatemachine.PlayerIsSprinting += SetPlayerSprinting;
    }

    private void OnTriggerStay(Collider other)
    {
        if (isPlayer && other.CompareTag("Enemy") && (isPlayerSprinting || FantasyCombatManager.Instance.InCombat()))
        {
            AlertEnemy(other, myPlayerstatemachine.transform.position, myPlayerstatemachine.transform.rotation);
        }
        else if (isCompanion && other.CompareTag("Enemy") && FantasyCombatManager.Instance.InCombat())
        {
            AlertEnemy(other, transform.position, transform.rotation);
        }
        else if (myEnemyStateMachine && other.CompareTag("Enemy"))
        {
            //if (myEnemyStateMachine.knowsPlayerPos || myEnemyStateMachine.IsInCombatState())
            if (myEnemyStateMachine.knowsPlayerPos && !myEnemyStateMachine.IsInCombatState()) //Updated to not alert during combat.
            {
                AlertEnemy(other, myEnemyStateMachine.playerLastKnownPos, myEnemyStateMachine.playerLastKnownRot);
            }
        }

        /* 
        else if (myEnemyStateMachine && other.CompareTag("Enemy"))
        {
            if (myEnemyStateMachine.IsInCombatState())
                AlertEnemy(other, myEnemyStateMachine.playerLastKnownPos, myEnemyStateMachine.playerLastKnownRot);
        }
        */
    }

    private void AlertEnemy(Collider other, Vector3 alertPosition, Quaternion alertRotation)
    {
        if (FantasyCombatManager.Instance.InCombat() && myEnemyStateMachine && !myEnemyStateMachine.IsInCombatState())
            Debug.Log("Enemy Alerted Via Proximity Radius");

        EnemyStateMachine enemy = other.GetComponent<EnemyStateMachine>();
        enemy.Alert(alertPosition, alertRotation);
    }

    private void SetPlayerSprinting(bool isSprinting)
    {
        isPlayerSprinting = isSprinting;
    }

    private void OnDisable()
    {
        if (isPlayer)
            myPlayerstatemachine.PlayerIsSprinting -= SetPlayerSprinting;
    }

}
