using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class POFDirector : MonoBehaviour
{
    public static POFDirector Instance { get; private set; }

    [Header("Animator Controllers")]
    [SerializeField] RuntimeAnimatorController leaderAnimator;
    [SerializeField] RuntimeAnimatorController companionAnimator;
    [Header("GameObjects")]
    [SerializeField] GameObject koBlendlistCam;
    [SerializeField] GameObject defaultBlendlistCam;
    [Space(10)]
    [SerializeField] GameObject hitTriggerPrefab;
    [SerializeField] List<GameObject> characterTrailPrefabs;
    [Header("Values")]
    [SerializeField] float heightToStopFall = 0.5f;
    [SerializeField] float dashAttackRotateTime = 0.15f;
    [Space(5)]
    [SerializeField] float flipJump = 2f;
    [Header("Timers")]
    [SerializeField] float fallDelay = 0.25f;
    [Space(10)]
    [SerializeField] float fallSpeed = 10f;
    [SerializeField] float landTime = 0.5335f;
    [SerializeField] float companionFirstDashTime = 0.1f;
    [SerializeField] float leaderChargeTime = 0.15f;
    [Space(10)]
    [SerializeField] float minDashAttackTime = 0.15f;
    [SerializeField] float maxDashAttackTime = 0.35f;
    [Space(10)]
    [SerializeField] float KODashTime = 3f;
    [SerializeField] float enemiesRemainingDashTime = 2f;
    [Space(10)]
    [SerializeField] float flipTime;
    [SerializeField] float poseTime;
    [Header("Transforms")]
    [SerializeField] Transform originHeader;
    [SerializeField] Transform highestOrigin;
    [SerializeField] Transform ChargeDestinationHeader;
    [SerializeField] Transform intiatorPosDestination;
    [Space(10)]
    [SerializeField] Transform dashDataHeader;
    [Space(10)]
    [SerializeField] Transform enemyPosHeader;
    [Space(10)]
    [SerializeField] Transform playerActorsHeader;
    [SerializeField] Transform enemyActorsHeader;
    [Space(10)]
    [SerializeField] Transform poseArtHeader;
    [Header("Indices")]
    [SerializeField] int originIndex = 0;
    [SerializeField] int destinationIndex = 1;
    [Header("TEST")]
    [SerializeField] Transform playerTestList;
    [SerializeField] Transform enemyTestList;

    int leaderIndex = 0;


    bool stopDashing = false;
    bool intiatorStopDashing = false;
    bool isVictory = false;

    Transform intiator = null;
    string intiatorName = "";

    GameObject leaderTrail;
    GameObject intiatorTrail;
    

    //Caches
    PowerOfFriendship powerOfFriendship;

    List<GameObject> triggersToDeactivate = new List<GameObject>();
    List<Transform> usedDashData = new List<Transform>();

    private void Awake()
    {
        Instance = this;
    }

    public void BeginCinematic()
    {
        ActivateCamera();

        stopDashing = false;
        intiatorStopDashing = false;

        SpawnEnemies();
        StartFall();
    }

    private void ActivateCamera()
    {
        koBlendlistCam.SetActive(isVictory);
        defaultBlendlistCam.SetActive(!isVictory);
    }

    private void StartFall()
    {
        foreach(Transform child in playerActorsHeader)
        {
            int index = child.GetSiblingIndex();

            child.position = originHeader.GetChild(index).position;
            child.rotation = originHeader.GetChild(index).rotation;

            if (index == leaderIndex)
            {
                child.DOMove(ChargeDestinationHeader.GetChild(index).position, leaderChargeTime);
                continue;
            }

            StartCoroutine(FallRoutine(child));
        }
    }

    IEnumerator FallRoutine(Transform fallingTransform)
    {
        yield return new WaitForSeconds(fallDelay);

        float fallTime = (fallingTransform.position.y) / fallSpeed;
        float timeToLand = (fallingTransform.position.y - heightToStopFall)/fallSpeed;
        fallingTransform.DOMoveY(0, fallTime).SetEase(Ease.InQuad);

        yield return new WaitForSeconds(timeToLand);

        Animator animator = fallingTransform.GetComponent<Animator>();
        animator.SetTrigger("Land");

        yield return new WaitForSeconds(landTime);
        DashAtCam(fallingTransform);
    }

    public void BeginGroupDash()
    {
        foreach (Transform player in playerActorsHeader)
        {
            BeginDashAttack(player);
        }

        leaderTrail.SetActive(true);
        StartCoroutine(DashCountDown());
    }

    private void BeginDashAttack(Transform player)
    {
        if(player == intiator && intiatorStopDashing) { return; }

        if(stopDashing) 
        {
            player.gameObject.SetActive(false);
            return; 
        }

        int randNum = Random.Range(0, dashDataHeader.childCount);
        Transform chosenDashData = dashDataHeader.GetChild(randNum);

        while (usedDashData.Contains(chosenDashData))
        {
            randNum = Random.Range(0, dashDataHeader.childCount);
            chosenDashData = dashDataHeader.GetChild(randNum);
        }

        usedDashData.Add(chosenDashData);

        Vector3 origin = chosenDashData.GetChild(originIndex).position;
        Vector3 destination = chosenDashData.GetChild(destinationIndex).position;

        player.position = origin;
        float randTime = Random.Range(minDashAttackTime, maxDashAttackTime);

        player.DOMove(destination, randTime).OnComplete(() => DashAttackComplete(player, chosenDashData));

        Vector3 desiredRotation = Quaternion.LookRotation((destination - origin).normalized).eulerAngles;
        player.DORotate(desiredRotation, dashAttackRotateTime);
    }

    IEnumerator DashCountDown()
    {
        yield return new WaitForSeconds(KODashTime);
        
        if (isVictory)
        {
            intiatorStopDashing = true;
            BeginFlip();
        }
        else
        {
            powerOfFriendship.UnloadPOF();
        }
    }

    private void BeginFlip()
    {
        Animator animator = intiator.GetComponent<Animator>();
        animator.SetTrigger("Flip");

        intiatorTrail.SetActive(false);
        intiator.DOJump(intiatorPosDestination.position, flipJump, 1, flipTime);
        intiator.DORotate(intiatorPosDestination.rotation.eulerAngles, dashAttackRotateTime);
    }

    public void PrepareEnemyKO()
    {
        foreach(GameObject trigger in triggersToDeactivate)
        {
            trigger.SetActive(false);
        }
    }

    public void ShowIntiatorUI()
    {
        stopDashing = true;

        foreach(Transform child in intiator)
        {
            child.gameObject.layer = LayerMask.NameToLayer("UI");
        }

        foreach(Transform child in poseArtHeader)
        {

            if (child.name.ToLower() == intiatorName.ToLower())
            {
                child.gameObject.SetActive(true);
                break;
            }
        }

        KOEnemies();
    }

    private void KOEnemies()
    {
        foreach (Transform enemy in enemyActorsHeader)
        {
            enemy.GetComponent<Animator>().SetTrigger("KO");
        }
    }

    public void Setup(int intiatorIndex, string intiatorName, List<PlayerGridUnit> playerActors, List<CharacterGridUnit> enemyActors, bool willKOAllEnemies, PowerOfFriendship powerOfFriendship)
    {
        //ClearActorHeaders();
        leaderIndex = playerActors.IndexOf(PartyManager.Instance.GetLeader());

        foreach (PlayerGridUnit actor in playerActors)
        {
            bool isLeader = playerActors.IndexOf(actor) == leaderIndex;

            Transform actorGO = Instantiate(actor.unitAnimator.gameObject, playerActorsHeader).transform;
            RuntimeAnimatorController animatorController = isLeader ? leaderAnimator : companionAnimator;

            if (isLeader)
                actorGO.SetAsFirstSibling();

            Animator animator = actorGO.GetComponent<Animator>();
            animator.runtimeAnimatorController = animatorController;

            GridUnitAnimator gridUnitAnimator = actorGO.GetComponent<GridUnitAnimator>();

            //Hide Unnecessary Visuals
            gridUnitAnimator.HideAllEquipment();
            gridUnitAnimator.HideStatusEffectsVFX();

            GameObject hitTrigger = Instantiate(hitTriggerPrefab, actorGO);
            hitTrigger.GetComponent<HitTrigger>().Setup(actor.gridCollider, true);

            GameObject charTrail = Instantiate(characterTrailPrefabs[playerActors.IndexOf(actor)], actorGO);

            if (isLeader)
            {
                leaderTrail = charTrail;
                leaderTrail.SetActive(false);
            }

            if(actor.unitName == intiatorName)
            {
                intiatorTrail = charTrail;
            }
        }

        foreach(CharacterGridUnit actor in enemyActors)
        {
            Transform actorGO = Instantiate(actor.unitAnimator.gameObject, enemyActorsHeader).transform;
            Animator animator = actorGO.GetComponent<Animator>();

            GameObject hitTrigger = Instantiate(hitTriggerPrefab, actorGO);

            hitTrigger.GetComponent<HitTrigger>().Setup(actor.gridCollider, false, animator);
            triggersToDeactivate.Add(hitTrigger);

            //Hide Status Effect VFX
            actorGO.GetComponent<GridUnitAnimator>().HideStatusEffectsVFX();
        }

        leaderIndex = 0;

        isVictory = willKOAllEnemies;
        intiator = playerActorsHeader.GetChild(intiatorIndex);
        this.intiatorName = intiatorName;
        this.powerOfFriendship = powerOfFriendship;
    }



    private void DashAttackComplete(Transform player, Transform chosenDashData)
    {
        usedDashData.Remove(chosenDashData);
        BeginDashAttack(player);
    }

    private void DashAtCam(Transform transformToMove)
    {
        int index = transformToMove.GetSiblingIndex();
        transformToMove.DOMove(ChargeDestinationHeader.GetChild(index).position, companionFirstDashTime);
    }

    private void SpawnEnemies()
    {
        foreach(Transform enemy in enemyActorsHeader)
        {
            int index = enemy.GetSiblingIndex();

            if(index >= enemyPosHeader.childCount)
            {
                return;
            }

            Transform pos = enemyPosHeader.GetChild(index);
            enemy.transform.position = pos.position;
            enemy.transform.rotation = pos.rotation;
        }
    }

}
