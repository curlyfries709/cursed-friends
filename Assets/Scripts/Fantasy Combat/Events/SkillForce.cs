using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Collections;
using AnotherRealm;
using System;
using Pathfinding;
using System.Linq;

public enum SkillForceType
{
    None,
    KnockbackAll,
    SuctionAll,
    KnockbackEnemiesSuctionAllies //Apply Knockback to enemies and suction to allies.
    //Cannot apply Knockback to allies as it is seen as an offensive force since it can inflict bump damge whereas suction is damageless 
}

public enum SkillForceDirectionType //For Suction, directions are in reverse
{
    PositionDirection, //Apply force in direction of (Target.GridPosition - Attacker.GridPosition).Normalized
    UnitForward //Apply force in direction related to the acting unit's forward direction
}

public struct SkillForceData
{
    public SkillForceType forceType;
    public SkillForceDirectionType directionType;
    public int forceDistance; //Only applies for Knockback

    public SkillForceData(SkillForceType forceType, SkillForceDirectionType forceDirectionType, int forceDistance)
    {
        this.forceType = forceType;
        this.directionType = forceDirectionType;
        this.forceDistance = forceDistance;
    }
}

public class SkillForce : MonoBehaviour, ITurnEndEvent
{
    public static SkillForce Instance { get; private set; }

    [Header("Knockback")]
    [SerializeField] PowerGrade knockbackAttackPowerGrade = PowerGrade.F;
    [Header("Knockback timers")]
    [SerializeField] float knockbackAnimTime = 0.5f;
    [SerializeField] float attackAnimDelayAfterKnockback = 0.475f;
    [Space(10)]
    [SerializeField] float attackerRotateTime = 0.15f;
    [Header("Bounce timers")]
    [SerializeField] float bounceAnimTime = 0.25f;
    [SerializeField] float bounceDelay = 0.15f;
    [Header("Knockback Damage Data")]
    [SerializeField] int knockbackDamageMinPercentage = 20;
    [SerializeField] int knockbackDamageMaxPercentage = 25;
    [Header("Suction")]
    [SerializeField] float suctionAnimTime = 0.5f;
    public int turnEndEventOrder { get; set; }

    public struct ApplyForceData
    {
        public GridUnit target;
        public GridUnit attacker; 
        public SkillForceType forceType;

        public int distance;
        public int damageReceived;

        public Vector3 direction;

        public ApplyForceData(GridUnit target, GridUnit attacker) //Called via public Suction Unit
        {
            this.target = target;
            this.attacker = attacker;

            //Below Data not necessary when using this constructor so give it zero data. 
            this.forceType = SkillForceType.SuctionAll;
            this.distance = 0;
            this.direction = Vector3.zero;
            this.damageReceived = 0;
        }

        public ApplyForceData(GridUnit target, GridUnit attacker, SkillForceType forceType, int distance, Vector3 direction, int damage)
        {
            this.target = target;
            this.attacker = attacker;
            this.forceType = forceType;
            this.distance = distance;
            this.direction = direction;
            this.damageReceived = damage;
        }
    }

    public struct KnockbackDestinationData
    {
        public GridUnit unitAtDestination;
        public Collider occupantCollider;

        public GridPosition knockbackDestination;
        public GridPosition bounceDestination;

        public KnockbackDestinationData(GridUnit unitAtDestination, Collider occupantCollider, GridPosition knockbackDestination, GridPosition bounceDestination)
        {
            this.unitAtDestination = unitAtDestination;
            this.occupantCollider = occupantCollider;
            this.knockbackDestination = knockbackDestination;
            this.bounceDestination = bounceDestination;
        }
    }

    bool unitBouncing = false;
    bool displayBumpDamage = false;

    //Caches
    List<ApplyForceData> unitsToApplyForce = new List<ApplyForceData>();

    Dictionary<GridUnit, KnockbackDestinationData> unitsToBounce = new Dictionary<GridUnit, KnockbackDestinationData>();
    Dictionary<GridUnit, int> bouncingUnitsDamageData = new Dictionary<GridUnit, int>();

    int numUnitsInFrontCurrentSuctionTarget = 0; //A counter for how many units are in front of the current suction target being checked. 

    private void Awake()
    {
        Instance = this;
        turnEndEventOrder = transform.GetSiblingIndex();
    }

    public void PrepareToApplyForceToUnit(GridUnit attacker, GridUnit target, SkillForceData inForceData, int damage)
    {
        //Only subscribe to Event once
        if(unitsToApplyForce.Count == 0)
        {
            unitBouncing = false;
            displayBumpDamage = false;

            IDamageable.TriggerHealthChangeEvent += ForceAllUnits;
            FantasyCombatManager.Instance.AddTurnEndEventToQueue(this);
        }

        Vector3 forceDirection = GetForceDirection(attacker, target, inForceData.forceType, inForceData.directionType);
        ApplyForceData forceData = new ApplyForceData(target, attacker, inForceData.forceType, inForceData.forceDistance, forceDirection, damage);

        unitsToApplyForce.Add(forceData);
    }

    public void PlayTurnEndEvent()
    {
        //FantasyCombatManager.Instance.UpdateDamageDataDisplayTime(Affinity.None, false, false);
        StartCoroutine(BounceRoutine());
    }

    public void OnEventCancelled()
    {
        ClearData();
    }

    private void ForceAllUnits(bool triggerEvent)
    {
        IDamageable.TriggerHealthChangeEvent -= ForceAllUnits;

        if (!triggerEvent)
        {
            OnEventCancelled();
            return;
        }

        //Knock them all back
        foreach (ApplyForceData data in unitsToApplyForce)
        {
            if(data.forceType == SkillForceType.KnockbackAll)
            {
                CharacterGridUnit allyAttacker = KnockbackUnit(data.target, data.distance, data.direction, out bool bounceUnit);

                if (bounceUnit)
                {
                    bouncingUnitsDamageData[data.target] = data.damageReceived;
                }

                //Trigger Attacker's anim
                if (allyAttacker)
                {
                    StartCoroutine(TriggerAttack(allyAttacker, -data.direction));
                }
            }
            else
            {
                SuctionUnit(data.target, data.attacker, data.direction);
            }
        }
    }

    private CharacterGridUnit KnockbackUnit(GridUnit target, int distance, Vector3 direction, out bool bounceUnit) //Returns allied unit of original attacker at bounce pos. 
    {
        KnockbackDestinationData knockbackDestinationData = GetKnockbackDestinationData(target, distance, direction);
        Vector3 knockbackDestinationWorldPos = LevelGrid.Instance.gridSystem.GetWorldPosition(knockbackDestinationData.knockbackDestination);
        bounceUnit = false;

        if (knockbackDestinationData.occupantCollider)
        {
            Collider collider = knockbackDestinationData.occupantCollider;
            
            Vector3 destination;

            if (collider is TerrainCollider)
            {
                //Raycast to get KnockbackDestination for terrain
                Vector3 lengthOrigin = new Vector3(target.transform.position.x, 0, target.transform.position.z);
                Vector3 lengthDestination = new Vector3(knockbackDestinationWorldPos.x, 0, knockbackDestinationWorldPos.z);

                float raycastLength = Vector3.Distance(lengthOrigin, lengthDestination) + 0.15f; //Added Small Padding.

                Vector3 castPos = new Vector3(target.transform.position.x, target.transform.position.y + 0.15f, target.transform.position.z);
                //Debug.DrawRay(castPos, direction.normalized * raycastLength, Color.red, 999);

                if (Physics.Raycast(castPos, direction.normalized, out RaycastHit hitInfo, raycastLength, GameSystemsManager.Instance.GetSceneDataAsFantasyData().GetTerrainLayerMask()))
                {
                    destination = hitInfo.point;
                }
                else
                {
                    //If It Didn't hit, we must assume that Knockback destination is not walkable.
                    float offset = raycastLength - (LevelGrid.Instance.GetCellSize() * 0.5f);
                    destination = target.transform.position + direction * offset;
                }
                
            }
            else
            {
                destination = knockbackDestinationData.occupantCollider.ClosestPointOnBounds(target.transform.position);
            }

            target.transform.DOMove(destination, knockbackAnimTime);
            bool isTargetAlive = target.GetDamageable().currentHealth > 0;

            //Prepare Animation
            CharacterGridUnit characterAtPos = knockbackDestinationData.unitAtDestination as CharacterGridUnit;
            CharacterGridUnit targetChar = target as CharacterGridUnit;

            if (isTargetAlive || (characterAtPos && WillUnitAtKnockbackPosTakeDamage(characterAtPos, targetChar)))
            {
                //Bounce Required
                unitBouncing = true;
                unitsToBounce[target] = knockbackDestinationData;
                bounceUnit = true;
            }

            if (isTargetAlive && ShouldUnitAtKnockbackPosAttack(characterAtPos, targetChar))
            {
                return characterAtPos;
            }
        }
        else
        {
            target.transform.DOMove(knockbackDestinationWorldPos, knockbackAnimTime).OnComplete(() => target.MovedToNewGridPos());
        }

        return null;
    }

    public void SuctionUnits(List<GridUnit> targets, GridUnit attacker, SkillForceDirectionType forceDirectionType, Action SuctionCompleteCallback)
    {
        foreach(GridUnit target in targets)
        {
            ApplyForceData applyForceData = new ApplyForceData(target, attacker);
            //Units To Apply Force List must be filled before calling SuctionUnit. Hence, separate loops.
            unitsToApplyForce.Add(applyForceData);
        }

        for (int i = 0; i < targets.Count; ++i)
        {
            GridUnit target = targets[i];
            Vector3 suctionDirection = GetForceDirection(attacker, target, SkillForceType.SuctionAll, forceDirectionType);

            //We only want to trigger this callback once, so do it for final unit to suction.
            Action callback = i == targets.Count - 1 ? SuctionCompleteCallback : null; 
            SuctionUnit(target, attacker, suctionDirection, callback);
        }

        //Add Turn End Event to clear data. 
        FantasyCombatManager.Instance.AddTurnEndEventToQueue(this);
    }

    private void SuctionUnit(GridUnit target, GridUnit attacker, Vector3 direction, Action SuctionCompleteCallback = null)
    {
        //Suction doesn't require distance as it pulls target to attacker as close as possible.
        //This supports suction of multiple units in the same axis. 

        //Reset data
        numUnitsInFrontCurrentSuctionTarget = 0;
        bool updatePosition = true;

        //Unit Grid Pos
        GridPosition targetGridPos = target.GetGridPositionsOnTurnStart()[0];
        GridPosition attackerGridPos = attacker.GetGridPositionsOnTurnStart()[0];

        if (CombatFunctions.IsGridPositionAdjacent(attackerGridPos, targetGridPos, true)) //If already adjacent, then no need to suction
        {
            updatePosition = false;
            OnSuctionComplete();
            return;
        }

        //Must begin from GridPos 1 unit in suction direction as to ignore target's grid pos when OnSuctionNodeChecked called
        GridPosition castOriginGridPos = CombatFunctions.GetGridPositionInDirection(targetGridPos, direction, 1);

        //Desired destination is GridPos 1 unit in opposite of suction direction from attacker Grid Pos
        GridPosition desiredDestinationGridPos = CombatFunctions.GetGridPositionInDirection(attackerGridPos, -direction, 1);

        GridGraph gridGraph = AstarPath.active.data.gridGraph;
        GridPosition newDestination;

        Vector2 normalizedPoint = LevelGrid.Instance.GetCellCentreNormalized();

        GridNodeBase startNode = LevelGrid.Instance.GetGridNode(castOriginGridPos);
        GridNodeBase endNode = LevelGrid.Instance.GetGridNode(desiredDestinationGridPos);

        //Swap the start and end node because there's a bug when tracing from start to end. 
        if (gridGraph.Linecast(endNode, normalizedPoint, startNode, normalizedPoint, out GridHitInfo hitInfo, null, OnSuctionNodeChecked, false))
        {
            //Means it hit something
            Vector3 hitWorldPos = (Vector3)hitInfo.node.position;
            GridPosition hitGridPos = LevelGrid.Instance.gridSystem.GetGridPosition(hitWorldPos);

            newDestination = CombatFunctions.GetGridPositionInDirection(hitGridPos, -direction, numUnitsInFrontCurrentSuctionTarget + 1); //1 is added as a 1 unit padding away from the obstacle
        }
        else
        {
            newDestination = CombatFunctions.GetGridPositionInDirection(desiredDestinationGridPos, -direction, numUnitsInFrontCurrentSuctionTarget);
        }

        GridPosition currentPos = target.GetGridPositionsOnTurnStart()[0];
        
        //Suction Unit
        Vector3 destinationWorldPos = LevelGrid.Instance.gridSystem.GetWorldPosition(newDestination);
        target.transform.DOMove(destinationWorldPos, suctionAnimTime).OnComplete(() => 
        {
            OnSuctionComplete();  
        });

        //Local function
        void OnSuctionComplete()
        {
            if(updatePosition)
                target.MovedToNewGridPos();

            if (SuctionCompleteCallback != null)
            {
                SuctionCompleteCallback();
            }
        }
    }

    private bool OnSuctionNodeChecked(GraphNode node)
    {
        /* AStar Docs: 
         * This delegate will be called for each node and if it returns false the node will be treated as unwalkable and a hit will be returned. 
         * Note that unwalkable nodes are always treated as unwalkable regardless of what this filter returns. */

        GridPosition gridPosition = LevelGrid.Instance.gridSystem.GetGridPosition((Vector3)node.position);

        bool isOccupiedByActiveUnit = LevelGrid.Instance.IsGridPositionOccupiedByUnit(gridPosition, false);
        bool isOccupiedByAnyUnit = LevelGrid.Instance.IsGridPositionOccupiedByUnit(gridPosition, true);

        if (!isOccupiedByActiveUnit && isOccupiedByAnyUnit) //Means the Unit is KOed 
        {
            return false;
        }
        else if (isOccupiedByActiveUnit)
        {
            if (unitsToApplyForce.Any((data) => data.target == LevelGrid.Instance.GetUnitAtGridPosition(gridPosition)))
            {
                //If unit is in UnitsToApplyForce, they're being suctioned too. 
                numUnitsInFrontCurrentSuctionTarget++;
            }
            else
            {
                /*If not in UnitsToApplyForce, then they have evaded or immune to suction or an enemy not targeted by suction. 
                 * So treat them like an obstacle*/
                return false;
            }
        }

        return true;
    }

    private void BounceAllUnits()
    {
        foreach(var item in unitsToBounce)
        {
            BounceForward(item.Key, item.Value.bounceDestination, item.Value.unitAtDestination);
        }

        /*if (!displayBumpDamage) { return; }
        
        if(IDamageable.unitAttackComplete == null)
        {
            //This Occurs when knockback damage is dealt to an alreayd single KOED Unit so No-one is subscribed.
            StartCoroutine(BouncedKOedUnitsRoutine());
        }
        else
        {
            
        }*/

        IDamageable.RaiseHealthChangeEvent(true);
    }


    private void ClearData()
    {
        //Clear List
        unitsToApplyForce.Clear();
        unitsToBounce.Clear();
        bouncingUnitsDamageData.Clear();

        numUnitsInFrontCurrentSuctionTarget = 0;
    }

    /*IEnumerator BouncedKOedUnitsRoutine()
    {
        yield return new WaitForSeconds(bounceAnimTime);
        FantasyCombatManager.Instance.ActionComplete();
    }*/

    private void BounceForward(GridUnit target, GridPosition bounceDestination, GridUnit unitAtKnockbackPos)
    {
        Vector3 bounceWorldPos = LevelGrid.Instance.gridSystem.GetWorldPosition(bounceDestination);
        target.transform.DOMove(bounceWorldPos, bounceAnimTime).SetEase(Ease.OutCubic).OnComplete(() => target.MovedToNewGridPos());

        if (unitAtKnockbackPos)
        {
            CharacterGridUnit characterAtPos = unitAtKnockbackPos as CharacterGridUnit;
            CharacterGridUnit targetChar = target as CharacterGridUnit;

            //Ensure Unit is still alive before dealing Damage

            if (ShouldUnitAtKnockbackPosAttack(characterAtPos, targetChar))
            {
                //Unit Takes Counter Damage From Enemy.
                characterAtPos.counterAttack.DealKnockbackDamage(targetChar, knockbackAttackPowerGrade);
                characterAtPos.unitAnimator.Idle();
            }
            else
            {
                displayBumpDamage = true;

                //Deal Damage to Unit.
                int rawDamage = bouncingUnitsDamageData[target];

                //If Unit Guarding or KOed Player (they remain on grid) do not take damage.
                if (!characterAtPos || (characterAtPos && !characterAtPos.Health().isGuarding && !characterAtPos.Health().isKOed)) //This means a disabled player that gets knocked by enemy would take damage :O
                {
                    unitAtKnockbackPos.GetComponent<IDamageable>().TakeBumpDamage(GetRandomBumpDamage(rawDamage)); //Damage Other Unit At Pos
                }

                //Target takes damage again
                target.GetComponent<IDamageable>().TakeBumpDamage(GetRandomBumpDamage(rawDamage));
            }
        }
        else
        {
            displayBumpDamage = true;
            //Target takes damage from obstacle.
            int rawDamage = bouncingUnitsDamageData[target];
            target.GetComponent<IDamageable>().TakeBumpDamage(GetRandomBumpDamage(rawDamage));
        }
    }

    IEnumerator BounceRoutine()
    {
        yield return new WaitForSeconds(bounceDelay);

        if (!unitBouncing)
        {
            ClearData();
            FantasyCombatManager.Instance.ActionComplete();
        }
        else
        {
            BounceAllUnits();
            ClearData();
        }
    }

    IEnumerator TriggerAttack(CharacterGridUnit attacker, Vector3 lookDirection)
    {
        Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
        attacker.transform.DORotate(lookRotation.eulerAngles, attackerRotateTime);

        yield return new WaitForSeconds(attackAnimDelayAfterKnockback);

        attacker.counterAttack.PlayBumpAttackAnimation();
    }


    private KnockbackDestinationData GetKnockbackDestinationData(GridUnit target, int distance, Vector3 direction)
    {
        GridGraph gridGraph = AstarPath.active.data.gridGraph;

        //Must begin from GridPos 1 unit in knockback direction as to ignore target's grid pos when OnKnockbackNodeChecked called
        GridPosition castOriginGridPos = CombatFunctions.GetGridPositionInDirection(target.GetGridPositionsOnTurnStart()[0], direction, 1);
        Vector3 castOriginWorldPos = LevelGrid.Instance.gridSystem.GetWorldPosition(castOriginGridPos);

        float cellSize = LevelGrid.Instance.GetCellSize();

        //Subtract 1 from distance because we ignore the target's current pos. 
        Vector3 castDestination = castOriginWorldPos + (direction * ((distance - 1) * cellSize));
        //Debug.DrawLine(castOriginWorldPos, castDestination, Color.red, 100);

        if(gridGraph.Linecast(castOriginWorldPos, castDestination, out GraphHitInfo hitInfo, null, OnKnockbackNodeChecked))
        {
            //Means it hit something
            Vector3 hitWorldPos = (Vector3)hitInfo.node.position;
            GridPosition hitGridPos = LevelGrid.Instance.gridSystem.GetGridPosition(hitWorldPos);

            if (IsGridPositionOccupied(target, hitGridPos))//Grid position Occupied
            {
                //We also bounce off KOed units. Just makes things easier and the design consistent.
                GridPosition posAfterBounce = CombatFunctions.GetGridPositionInDirection(hitGridPos, -direction, 1);
                return GetBounceData(hitGridPos, posAfterBounce);
            }
        }

        //No obstacle or active grid units detected. 
        GridPosition newDestination = LevelGrid.Instance.gridSystem.GetGridPosition(castDestination);
        return new KnockbackDestinationData(null, null, newDestination, newDestination);
    }

    private bool OnKnockbackNodeChecked(GraphNode node)
    {
        /* AStar Docs: 
         * This delegate will be called for each node and if it returns false the node will be treated as unwalkable and a hit will be returned. 
         * Note that unwalkable nodes are always treated as unwalkable regardless of what this filter returns. */

        GridPosition gridPosition = LevelGrid.Instance.gridSystem.GetGridPosition((Vector3)node.position);
        return !LevelGrid.Instance.IsGridPositionOccupiedByUnit(gridPosition, true);
    }

    private KnockbackDestinationData GetBounceData(GridPosition knockbackDestination, GridPosition positionAfterBounce)
    {
        Collider collider;
        GridUnit unitAtPos = null;

        if (LevelGrid.Instance.TryGetObstacleAtPosition(knockbackDestination, out Collider obstacleData))
        {
            collider = obstacleData;
        }
        else
        {
            //Must be unit
            unitAtPos = LevelGrid.Instance.GetUnitAtGridPosition(knockbackDestination);
            collider = unitAtPos.gridCollider;
        }

        return new KnockbackDestinationData(unitAtPos, collider, knockbackDestination, positionAfterBounce);
    }

    public GridUnit GetUnitInKnocbackRange(GridUnit target, int distance, Vector3 direction)
    {
        KnockbackDestinationData knockbackDestinationData = GetKnockbackDestinationData(target, distance, direction);
        return knockbackDestinationData.unitAtDestination;
    }

    private int GetRandomBumpDamage(int rawDamage)
    {
        return Mathf.RoundToInt(UnityEngine.Random.Range(((float)knockbackDamageMinPercentage / 100) * rawDamage, ((float)knockbackDamageMaxPercentage / 100) * rawDamage));
    }


    private bool ShouldUnitAtKnockbackPosAttack(CharacterGridUnit unitAtKnockbackPos, CharacterGridUnit target)
    {
        return /*!target.Health().isKOed && */unitAtKnockbackPos && target && unitAtKnockbackPos.team != target.team
            && unitAtKnockbackPos.Health().CanTriggerAssistAttack();
    }

    private bool WillUnitAtKnockbackPosTakeDamage(CharacterGridUnit unitAtKnockbackPos, CharacterGridUnit target)
    {
        if (!target)
            return false;

        return unitAtKnockbackPos.team == target.team || StatusEffectManager.Instance.IsUnitDisabled(unitAtKnockbackPos);
    }

    private bool IsGridPositionOccupied(GridUnit currentUnit, GridPosition gridPosition)
    {
        return !LevelGrid.Instance.CanOccupyGridPosition(currentUnit, gridPosition);
    }

    private Vector3 GetForceDirection(GridUnit attacker, GridUnit target, SkillForceType forceType, SkillForceDirectionType forceDirectionType)
    {

        if (forceDirectionType == SkillForceDirectionType.PositionDirection)
        {
            Vector3 forceDirection = (target.transform.position - attacker.transform.position).normalized;
            Vector3 roundedDirection = CombatFunctions.RoundDirection(forceDirection);

            if (forceType == SkillForceType.KnockbackAll)
            {
                return roundedDirection;
            }
            else if (forceType == SkillForceType.SuctionAll)
            {
                //Suction is opposite direction.
                return -roundedDirection;
            }
        }
        else
        {
            Vector3 unitForwardDirection = CombatFunctions.RoundDirectionToCardinalDirection(attacker.transform.forward);

            if (forceType == SkillForceType.KnockbackAll)
            {
                return unitForwardDirection;
            }
            else if (forceType == SkillForceType.SuctionAll)
            {
                //Suction is opposite direction.
                return -unitForwardDirection;
            }
        }

        //If reached, force Type is not valid.
        Debug.LogError("INVALID FORCE TYPE PROVIDED. Please set to KnockbalAll or SuctionAll");
        return Vector3.zero;
    }

    public List<System.Type> GetEventTypesThatCancelThis()
    {
        return new List<System.Type>();
    }

}
