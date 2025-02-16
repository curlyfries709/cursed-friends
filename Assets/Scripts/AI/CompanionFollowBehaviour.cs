using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CompanionFollowBehaviour : MonoBehaviour
{
    [Header("Companions")]
    [SerializeField] int maxCompanionsFollow = 3;
    [Header("Party Follow Data")]
    [SerializeField] CompanionFollowData roamFollowData;
    [Header("Party Sneak Follow Data")]
    [SerializeField] CompanionFollowData sneakFollowData;

    List<CompanionStateMachine> followingCompanions = new List<CompanionStateMachine>();

    [System.Serializable]
    public struct CompanionFollowData
    {
        [Header("Distances")]
        public float horizontalDistance;
        public float verticalDistance;
        [Space(5)]
        public float distanceToBeginFollow;
        public float laggingBehindDistance;
        [Header("Speed & Acceleration")]
        public float acceleration;
        public float maxLagSpeed;
    }

    private void OnEnable()
    {
        PlayerSpawnerManager.Instance.SwapCompanionPositionsEvent += SwapCompanionPositions;
    }

    public void SetCompanionFollowBehaviour(List<CompanionStateMachine> spawnedCompanions)
    {
        foreach (CompanionStateMachine companionStateMachine in spawnedCompanions)
        {
            followingCompanions.Add(companionStateMachine);

            int index = followingCompanions.IndexOf(companionStateMachine);

            //Set Swap Pos Raise Event Designator
            if (index == 0)
            {
                companionStateMachine.raiseSwapPosEventDesignee = true;
            }

            companionStateMachine.SetupFollowBehaviour(this);

            UpdateCompanionFollowBehaviour(companionStateMachine, false);
        }
    }


    public void UpdateCompanionFollowBehaviour(CompanionStateMachine companion, bool isSneaking)
    {
        float localVerticalDis = isSneaking ? sneakFollowData.verticalDistance * -1f : roamFollowData.verticalDistance * -1f;
        float localHorizontalDis = isSneaking ? sneakFollowData.horizontalDistance : roamFollowData.horizontalDistance;

        int index = followingCompanions.IndexOf(companion);

        switch (index)
        {
            case 0:
                break;
            case 1:
                localHorizontalDis = localHorizontalDis * -1f;
                break;
            case 2:
                localVerticalDis = localVerticalDis - 1f;
                localHorizontalDis = 0;
                break;
        }

        companion.horizontalFollowOffset = localHorizontalDis;
        companion.verticalFollowOffset = localVerticalDis;
    }

    private void SwapCompanionPositions()
    {
        if (followingCompanions.Count > 0)
            followingCompanions[0].horizontalFollowOffset = followingCompanions[0].horizontalFollowOffset * -1;

        if (followingCompanions.Count > 1)
            followingCompanions[1].horizontalFollowOffset = followingCompanions[1].horizontalFollowOffset * -1;
    }

    public float GetDistanceToBeginFollow(CompanionStateMachine companion, bool isSneaking)
    {
        float distance = isSneaking ?sneakFollowData.distanceToBeginFollow : roamFollowData.distanceToBeginFollow;

        if (followingCompanions.IndexOf(companion) == maxCompanionsFollow - 1)
        {
            distance = distance + 1;
        }

        return distance;
    }

    public float GetLagSpeed(bool isSneaking)
    {
        return isSneaking ? sneakFollowData.maxLagSpeed : roamFollowData.maxLagSpeed;
    }

    public float GetLaggingBehindDistance(bool isSneaking)
    {
        return isSneaking ? sneakFollowData.laggingBehindDistance : roamFollowData.laggingBehindDistance;
    }

    public float GetAcceleration(bool isSneaking)
    {
        return isSneaking ? sneakFollowData.acceleration : roamFollowData.acceleration;
    }


    private void OnDisable()
    {
        PlayerSpawnerManager.Instance.SwapCompanionPositionsEvent -= SwapCompanionPositions;
    }

}
