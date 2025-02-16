using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Collections;
using AnotherRealm;

public class Knockback : MonoBehaviour, ITurnEndEvent
{
    public static Knockback Instance { get; private set; }
    [Header("Power")]
    [SerializeField] PowerGrade knockbackAttackPowerGrade = PowerGrade.F;
    [Header("Knockback timers")]
    [SerializeField] float knockbackAnimTime = 0.5f;
    [SerializeField] float attackAnimDelayAfterKnockback = 0.25f;
    [Space(10)]
    [SerializeField] float attackerRotateTime = 0.15f;
    [Header("Bounce timers")]
    [SerializeField] float bounceAnimTime = 0.25f;
    [SerializeField] float bounceDelay = 0.25f;
    [Header("Damage Data")]
    [SerializeField] int knockbackDamageMinPercentage = 8;
    [SerializeField] int knockbackDamageMaxPercentage = 12;

    public int turnEndEventOrder { get; set; }

    public struct KnockbackData
    {
        public GridUnit target;
        public int distance;
        public int damageReceived;
        public Vector3 direction;

        public KnockbackData(GridUnit target, int distance, Vector3 direction, int damage)
        {
            this.target = target;
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
    List<KnockbackData> unitsToKnockback = new List<KnockbackData>();

    Dictionary<GridUnit, KnockbackDestinationData> unitsToBounce = new Dictionary<GridUnit, KnockbackDestinationData>();
    Dictionary<GridUnit, int> bouncingUnitsDamageData = new Dictionary<GridUnit, int>();

    private void Awake()
    {
        Instance = this;
        turnEndEventOrder = transform.GetSiblingIndex();
    }


    public void PrepareToKnockbackUnit(GridUnit attacker, GridUnit target, int distance, int damage)
    {
        //Only subscribe to Event once
        if(unitsToKnockback.Count == 0)
        {
            unitBouncing = false;
            displayBumpDamage = false;

            IDamageable.unitAttackComplete += KnockbackUnits;
            FantasyCombatManager.Instance.AddTurnEndEventToQueue(this);
        }

        Vector3 knockbackDirection = GetKnockbackDirection(attacker, target);

        KnockbackData knockbackData = new KnockbackData(target, distance, knockbackDirection, damage);

        unitsToKnockback.Add(knockbackData);
    }

    public void PlayTurnEndEvent()
    {
        //FantasyCombatManager.Instance.UpdateDamageDataDisplayTime(Affinity.None, false, false);
        StartCoroutine(Knockbackroutine());
    }

    public void OnEventCancelled()
    {
        //Event Cannot be cancelled so do nothing
        Debug.Log("KNOCKBACK EVENT CANCELLED!");
        ClearData();
    }

    private void KnockbackUnits(bool beginHealthCountdown)
    {
        IDamageable.unitAttackComplete -= KnockbackUnits;

        //Knock them all back
        foreach (KnockbackData data in unitsToKnockback)
        {
            CharacterGridUnit attacker = KnockbackUnit(data.target, data.distance, data.direction);
            bouncingUnitsDamageData[data.target] = data.damageReceived;

            //Trigger Attacker's anim
            if (attacker)
            {
                StartCoroutine(TriggerAttack(attacker, -data.direction));
            }
        }
    }

    private CharacterGridUnit KnockbackUnit(GridUnit target, int distance, Vector3 direction)
    {
        KnockbackDestinationData knockbackDestinationData = GetKnockbackDestinationData(target, distance, direction);
        Vector3 knockbackDestinationWorldPos = LevelGrid.Instance.gridSystem.GetWorldPosition(knockbackDestinationData.knockbackDestination);

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

        IDamageable.unitAttackComplete?.Invoke(true);
    }


    private void ClearData()
    {
        //Clear List
        unitsToKnockback.Clear();
        unitsToBounce.Clear();
        bouncingUnitsDamageData.Clear();
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

    IEnumerator Knockbackroutine()
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
        GridPosition startPos = target.GetGridPositionsOnTurnStart()[0];
        Vector2 maxLoopCount = new Vector2(direction.x, direction.z) * distance;

        if(direction == Vector3.back)
        {
            //X, y - 1
            int loopEndValue = startPos.z + (int)maxLoopCount.y;

            for (int y = startPos.z; y >= loopEndValue; y--)
            {
                GridPosition currentGridPos = new GridPosition(startPos.x, y);

                if (y < 0) //Grid Position Invalid
                {
                    GridPosition destination = new GridPosition(startPos.x, 0);
                    return new KnockbackDestinationData(null, null, destination, destination);
                }
                else if (IsGridPositionOccupied(target, currentGridPos))//Grid position Occupied
                {
                    GridPosition bouncePosition = new GridPosition(startPos.x, y + 1);
                    return GetBounceData(currentGridPos, bouncePosition);
                }
            }

            //Made to end of loop
            GridPosition knockbackDestination = new GridPosition(startPos.x, loopEndValue);
            return new KnockbackDestinationData(null, null, knockbackDestination, knockbackDestination);
        }
        else if(direction == Vector3.right)
        {
            //X+1 , y
            int loopEndValue = startPos.x + (int)maxLoopCount.x;

            for (int x = startPos.x; x <= loopEndValue; x++)
            {
                GridPosition currentGridPos = new GridPosition(x, startPos.z);

                if (IsGridPositionOccupied(target, currentGridPos))//Grid position Occupied
                {
                    GridPosition bouncePosition = new GridPosition(x-1, startPos.z);
                    return GetBounceData(currentGridPos, bouncePosition);
                }
            }

            //Made to end of loop
            GridPosition knockbackDestination = new GridPosition(loopEndValue, startPos.z);
            return new KnockbackDestinationData(null, null, knockbackDestination, knockbackDestination);
        }
        else if (direction == Vector3.left)
        {
            //X-1 , y

            int loopEndValue = startPos.x + (int)maxLoopCount.x;

            for (int x = startPos.x; x >= loopEndValue; x--)
            {
                GridPosition currentGridPos = new GridPosition(x, startPos.z);

                if (x < 0) //Grid Position Invalid
                {
                    GridPosition destination = new GridPosition(0, startPos.z);
                    return new KnockbackDestinationData(null, null, destination, destination);
                }
                else if (IsGridPositionOccupied(target, currentGridPos))//Grid position Occupied
                {
                    GridPosition bouncePosition = new GridPosition(x + 1, startPos.z);
                    return GetBounceData(currentGridPos, bouncePosition);
                }
            }

            //Made to end of loop
            GridPosition knockbackDestination = new GridPosition(loopEndValue, startPos.z);
            return new KnockbackDestinationData(null, null, knockbackDestination, knockbackDestination);
        }
        else
        {
            //X   Y+1
            int loopEndValue = startPos.z + (int)maxLoopCount.y;

            for (int y = startPos.z; y <= loopEndValue; y++)
            {
                GridPosition currentGridPos = new GridPosition(startPos.x, y);

                if (IsGridPositionOccupied(target, currentGridPos))//Grid position Occupied
                {
                    GridPosition bouncePosition = new GridPosition(startPos.x, y - 1);
                    return GetBounceData(currentGridPos, bouncePosition);
                }
            }

            //Made to end of loop
            GridPosition knockbackDestination = new GridPosition(startPos.x, loopEndValue);
            return new KnockbackDestinationData(null, null, knockbackDestination, knockbackDestination);
        }
    }

    private KnockbackDestinationData GetBounceData(GridPosition knockbackDestination, GridPosition bouncePosition)
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

        return new KnockbackDestinationData(unitAtPos, collider, knockbackDestination, bouncePosition);
    }

    public GridUnit GetUnitInKnocbackRange(GridUnit target, int distance, Vector3 direction)
    {
        KnockbackDestinationData knockbackDestinationData = GetKnockbackDestinationData(target, distance, direction);
        return knockbackDestinationData.unitAtDestination;
    }

    private int GetRandomBumpDamage(int rawDamage)
    {
        return Mathf.RoundToInt(Random.Range(((float)knockbackDamageMinPercentage / 100) * rawDamage, ((float)knockbackDamageMaxPercentage / 100) * rawDamage));
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

    private Vector3 GetKnockbackDirection(GridUnit attacker, GridUnit target)
    {
        //KnockbackDirection Is Opposite of Look at Attack Direction.
        return -CombatFunctions.GetAttackLookDirection(attacker, target);

        //OLD CODE
        //GetDirectionAsCompassVector((target.transform.position - attacker.transform.position).normalized);
        //CombatFunctions.RoundDirectionToCardinalDirection((target.transform.position - attacker.transform.position).normalized);
    }


    public List<System.Type> GetEventTypesThatCancelThis()
    {
        return new List<System.Type>();
    }

}
