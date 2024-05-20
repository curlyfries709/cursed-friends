using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EnemyTacticsManager : MonoBehaviour
{
    public static EnemyTacticsManager Instance { get; private set; }

    [Header("Ambush Point Data")]
    [SerializeField] AmbushPointVisual ambushPointsHeader;
    [SerializeField] float ambushPointOffset = 0.5f;
    public float maxInterceptionTime = 3f;
    [Header("Numbers")]
    [SerializeField] int maxGuardsChasing = 3;
    [SerializeField] int maxGuardsAtAmbushPoint = 2;
    [Space(5)]
    [SerializeField] float orderAmbushCycle = 3f;
    public float maxAmbushWaitTime = 5f;

    //Variables
    float timeOfLastOrderedAmbush = -1f;

    //List & Dicts
    List<EnemyStateMachine> guardsChasingPlayer = new List<EnemyStateMachine>();
    List<Transform> ambushPoints = new List<Transform>();

    Dictionary<Transform, List<EnemyStateMachine>> ambushPointAssignedGuardsDict = new Dictionary<Transform, List<EnemyStateMachine>>();

    //Caches
    Transform player;
    bool playerInDanger = false;

    private void Awake()
    {
        Instance = this;
        
        if(ambushPointsHeader)
            SetAmbushPoints();
    }

    private void Start()
    {
        player = StoryManager.Instance.GetPlayerStateMachine().transform;
    }

    // Update is called once per frame
    void Update()
    {
        OrderAmbush();
    }

    private void OrderAmbush()
    {
        if (Time.time > timeOfLastOrderedAmbush + orderAmbushCycle)
        {
            SetupAmbush();
        }
    }


    public void IsChasingPlayer(EnemyStateMachine guard, bool isChasing)
    {
        if (isChasing)
        {
            if (!guardsChasingPlayer.Contains(guard))
            {
                guardsChasingPlayer.Add(guard);
            }

            SetupAmbush();
        }
        else
        {
            guardsChasingPlayer.Remove(guard);
        }

        bool shouldEnable = guardsChasingPlayer.Count > 0;

        if (playerInDanger != shouldEnable)
        {
            playerInDanger = shouldEnable;
            PlayerStateMachine.PlayerInDanger?.Invoke(playerInDanger);
        }
    }

    public void AbadoningAmbushPoint(Transform ambush, EnemyStateMachine guard)
    {
        ambushPointAssignedGuardsDict[ambush].Remove(guard);
    }

    private void SetupAmbush()
    {
        if (guardsChasingPlayer.Count <= maxGuardsChasing){ return; }

        bool ambushSuccessful = false;
        //Find Enemies Chasing Player and Send the ones furthest from the player. 

        int numOfGuardsToSendAway = guardsChasingPlayer.Count - maxGuardsChasing;

        Dictionary<EnemyStateMachine, float> eligibleGuardsDistFromPlayer = new Dictionary<EnemyStateMachine, float>();

        foreach(EnemyStateMachine guard in guardsChasingPlayer)
        {
            Vector3 dir = (guard.transform.position - player.position).normalized;
            float result = Vector3.Dot(player.TransformDirection(Vector3.forward), dir);

            //If Guard In Front of Player or Not Chasing (perhaps attacking), continue.
            
            if (result > 0 || !guard.CanAmbushPlayer()) { continue; }

            eligibleGuardsDistFromPlayer[guard] = Vector3.Distance(guard.transform.position, player.transform.position);
        }

        numOfGuardsToSendAway = Mathf.Clamp(numOfGuardsToSendAway, 1, eligibleGuardsDistFromPlayer.Count);
        //Order Dict in Descending
        var sortedDict = eligibleGuardsDistFromPlayer.OrderByDescending(entry => entry.Value);

        for(int i = 0; i < numOfGuardsToSendAway; i++)
        {
            EnemyStateMachine guard = sortedDict.ElementAt(i).Key;
            Transform ambushPoint = SelectAmbushSpot(guard);

            if (ambushPoint)
            {
                ambushSuccessful = true;
                guard.GoToAmbushPoint(ambushPoint, GetAmbushPos(ambushPoint));
            }
        }

        if (ambushSuccessful)
        {
            timeOfLastOrderedAmbush = Time.time;
        }
    }

   private Transform SelectAmbushSpot(EnemyStateMachine guard)
    {
        //First Find Ambush Points Ahead of Player's Direction
        List<Transform> ambushPointsAheadOfPlayer = new List<Transform>();

        foreach (Transform point in ambushPoints)
        {
            Vector3 dir = (point.position - player.position).normalized;
            float result = Vector3.Dot(player.TransformDirection(Vector3.forward), dir);

            //Means the Ambush Point is Ahead of player
            if (result > 0)
            {
                ambushPointsAheadOfPlayer.Add(point);
            }
        }

        Transform closestPoint = null;
        float shortestDist = Mathf.Infinity;

        //Then Select Point closest to guard. If Ambush Point at max capacity find another one.
        foreach (Transform point in ambushPointsAheadOfPlayer)
        {
            if (ambushPointAssignedGuardsDict[point].Count >= maxGuardsAtAmbushPoint){continue;}

            float calculatedDist = Vector3.Distance(guard.transform.position, point.position);

            if(calculatedDist < shortestDist)
            {
                shortestDist = calculatedDist;
                closestPoint = point;
            }
        }

        if (closestPoint)
        {
            ambushPointAssignedGuardsDict[closestPoint].Add(guard);
        }

        return closestPoint;
    }

    private Vector3 GetAmbushPos(Transform ambushPoint)
    {
        int guardsAtAmbushPoint = ambushPointAssignedGuardsDict[ambushPoint].Count;

        if(guardsAtAmbushPoint == maxGuardsAtAmbushPoint)
        {
            //Return Right Pos
            return ambushPoint.position + ambushPoint.right * ambushPointOffset;
        }
        else
        {
            //Return Left Pos
            return ambushPoint.position + -ambushPoint.right * ambushPointOffset;
        }
    }

   

    private void SetAmbushPoints()
    {
        foreach (Transform child in ambushPointsHeader.transform)
        {
            ambushPoints.Add(child);
            ambushPointAssignedGuardsDict[child] = new List<EnemyStateMachine>();
        }
    }

    public List<EnemyStateMachine> GetAllChasingEnemies()
    {
        return guardsChasingPlayer;
    }
}
