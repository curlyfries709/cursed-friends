using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;
using MoreMountains.Feedbacks;
using Cinemachine;
using UnityEngine.UI;
using UnityEngine.AI;

[EnumToggleButtons]
public enum FantasyCombatTarget
{
    Self,
    Ally,
    Enemy,
    Object,
    Grid
}

public enum OtherSkillType
{
    Recovery,
    Support,
    Tactic
}

public enum Direction
{
    North,
    South,
    West,
    East,
    NorthEast,
    NorthWest,
    SouthEast,
    SouthWest
}


namespace AnotherRealm
{
    public class CombatFunctions
    {
        //Patrolling

        public static bool HasAgentArrivedAtDestination(NavMeshAgent navMeshAgent)
        {
            if (!navMeshAgent.enabled) { return false; }

            if (!navMeshAgent.pathPending)
            {
                if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
                {
                    if (!navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude == 0f)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static void SetPatrolDestination(Transform patrolRoute, Vector3 currentPostion, ref bool idleAtWaypoint, ref int currentWaypointIndex, NavMeshAgent navMeshAgent, bool continuePatrol)
        {
            Transform closestWaypoint = GetClosestPatrolPoint(patrolRoute, currentPostion);

            if (continuePatrol)
            {
                currentWaypointIndex = GetNextWaypointIndex(closestWaypoint.GetSiblingIndex(), patrolRoute);
            }
            else
            {
                currentWaypointIndex = closestWaypoint.GetSiblingIndex();
            }

            Transform targetWaypoint = patrolRoute.GetChild(currentWaypointIndex);
            idleAtWaypoint = targetWaypoint.GetComponent<WaypointData>().ShouldIdleAtWaypoint();
            navMeshAgent.SetDestination(targetWaypoint.position);
        }

        public static Transform GetClosestPatrolPoint(Transform patrolRoute, Vector3 currentPosition)
        {
            float shortestDistance = Mathf.Infinity;
            Transform closestWaypoint = patrolRoute.GetChild(0);

            foreach (Transform waypoint in patrolRoute)
            {
                float calculatedDistance = Vector3.Distance(currentPosition, waypoint.position);

                if (calculatedDistance < shortestDistance)
                {
                    shortestDistance = calculatedDistance;
                    closestWaypoint = waypoint;
                }
            }

            return closestWaypoint;
        }

        /*public static void GoToNextWaypoint(ref int currentWaypointIndex, Transform patrolRoute, ref bool idleAtWaypoint, NavMeshAgent navMeshAgent)
        {
            currentWaypointIndex = GetNextWaypointIndex(currentWaypointIndex, patrolRoute);

            Transform newWaypoint = patrolRoute.GetChild(currentWaypointIndex);

            idleAtWaypoint = newWaypoint.GetComponent<WaypointData>().ShouldIdleAtWaypoint();
            navMeshAgent.SetDestination(newWaypoint.position);
        }*/

        public static int GetNextWaypointIndex(int currentWaypointIndex, Transform patrolRoute)
        {
            if (currentWaypointIndex + 1 >= patrolRoute.childCount)
            {
                return 0;
            }

            return currentWaypointIndex + 1;
        }


        //Grid Methods
        public static bool IsUnitStandingInMoreCellsThanNeccesary(CharacterGridUnit unit)
        {
            return unit.GetCurrentGridPositions(false).Count > unit.GetMaxCellsRequired();
        }

        public static List<GridPosition> GetValidGridPositionsBasedOnDirection(GridPosition centrePos, Vector2 targetArea, Direction direction)
        {
           // Debug.Log("Centre Pos: " + centrePos.ToString() + " Direction: " + direction.ToString());

            if (direction == Direction.North)
            {
                //Facing Vector3.Forward
                int areaWidth = Mathf.FloorToInt(targetArea.x); 
                int offset = Mathf.FloorToInt(areaWidth / 2); 

                int XOrigin = centrePos.x - offset; 
                int XEnd = centrePos.x + offset; 

                int ZOrigin = centrePos.z + 1; 
                int ZEnd = centrePos.z + Mathf.FloorToInt(targetArea.y); 

                return GetValidGridPositions(XOrigin, XEnd, ZOrigin, ZEnd);
            }
            else if (direction == Direction.East)
            {
                //Facing Vector3.Right
                Vector2 correctedTargetArea = new Vector2(targetArea.y, targetArea.x);
                int areaWidth = Mathf.FloorToInt(targetArea.x);

                int offset = Mathf.FloorToInt(areaWidth / 2);

                int ZOrigin = centrePos.z - offset;
                int ZEnd = centrePos.z + offset;

                int XOrigin = centrePos.x + 1;
                int XEnd = centrePos.x + Mathf.FloorToInt(correctedTargetArea.x);

                return GetValidGridPositions(XOrigin, XEnd, ZOrigin, ZEnd);
            }
            else if (direction == Direction.West)
            {
                //Facing Vector3.Left
                Vector2 correctedTargetArea = new Vector2(targetArea.y, targetArea.x);
                int areaWidth = Mathf.FloorToInt(targetArea.x);
                int offset = Mathf.FloorToInt(areaWidth / 2);

                int ZOrigin = centrePos.z - offset;
                int ZEnd = centrePos.z + offset;

                int XOrigin = centrePos.x - Mathf.FloorToInt(correctedTargetArea.x);
                int XEnd = centrePos.x - 1;

                return GetValidGridPositions(XOrigin, XEnd, ZOrigin, ZEnd);
            }
            else if (direction == Direction.South)
            {
                //Facing Vector3.Back
                int areaWidth =  Mathf.FloorToInt(targetArea.x);
                int offset = Mathf.FloorToInt(areaWidth / 2);

                int XOrigin = centrePos.x - offset;
                int XEnd = centrePos.x + offset;

                int ZOrigin = centrePos.z - Mathf.FloorToInt(targetArea.y);
                int ZEnd = centrePos.z - 1;

                return GetValidGridPositions(XOrigin, XEnd, ZOrigin, ZEnd);
            }
            else
            {
                Debug.Log("UNIT FACING INVALID DIRECTION");
                return new List<GridPosition>();
            }
        }

        private static List<GridPosition> GetValidGridPositions(int XOrigin, int XEnd, int ZOrigin, int ZEnd)
        {
            GridSystem<GridObject> gridSystem = LevelGrid.Instance.gridSystem;
            List<GridPosition> validGridPositionsList = new List<GridPosition>();

            /*Debug.Log("XORIGIN: " + XOrigin);
            Debug.Log("XEND: " + XEnd);

            Debug.Log("ZORIGIN: " + ZOrigin);
            Debug.Log("ZEND: " + ZEnd);*/

            for (int x = XOrigin; x <= XEnd; x++)
            {
                for (int z = ZOrigin; z <= ZEnd; z++)
                {
                    GridPosition gridPosition = new GridPosition(x, z);

                    if (!gridSystem.IsValidGridPosition(gridPosition) || !LevelGrid.Instance.IsWalkable(gridPosition) || LevelGrid.Instance.IsGridPositionOccupiedByUnit(gridPosition, true))
                    {
                        continue;
                    }

                    validGridPositionsList.Add(gridPosition);
                }
            }
            return validGridPositionsList;
        }


        //Damage Methods
        public static List<InflictedStatusEffectData> TryInflictStatusEffects(CharacterGridUnit attacker,  GridUnit target, List<ChanceOfInflictingStatusEffect> tryApplyStatusEffects)
        {
            List<InflictedStatusEffectData> successfulStatusEffects = new List<InflictedStatusEffectData>();
            List<ChanceOfInflictingStatusEffect> allStatusEffects = tryApplyStatusEffects.Concat(attacker.stats.Equipment().GetStatusEffectsToInflict()).ToList();

            CharacterGridUnit targetCharacter = target as CharacterGridUnit;

            if (targetCharacter)
            {
                foreach (ChanceOfInflictingStatusEffect effect in allStatusEffects)
                {
                    int randNum = Random.Range(0, 101);

                    if (randNum <= effect.bonusPercentOfInflictingEffect + attacker.stats.StatusEffectInflictChance)
                    {
                        InflictedStatusEffectData successfulEffect = new InflictedStatusEffectData(effect.statusEffect, targetCharacter, attacker.stats.SEDuration, effect.buffChange);
                        successfulStatusEffects.Add(successfulEffect);
                    }
                }
            }

            return successfulStatusEffects;
        }

        //Targeting Methods
        public static FantasyCombatTarget GetRelationWithTarget(CharacterGridUnit skillOwner, GridUnit target)
        {
            if (target.unitType == CombatUnitType.Object)
            {
                return FantasyCombatTarget.Object;
            }

            //From this point Onwards, Target must be a Character Grid Unit
            CharacterGridUnit targetCharacter = target as CharacterGridUnit;

            if (skillOwner == targetCharacter)
            {
                return FantasyCombatTarget.Self;
            }

            if (skillOwner.team == targetCharacter.team)
            {
                return FantasyCombatTarget.Ally;
            }

            if (skillOwner.team != targetCharacter.team)
            {
                return FantasyCombatTarget.Enemy;
            }

            return FantasyCombatTarget.Grid;
        }

        public static bool IsUnitValidTarget(List<FantasyCombatTarget> skillTargets, CharacterGridUnit skillOwner, GridUnit target)
        {
            if (!target)
            {
                return skillTargets.Contains(FantasyCombatTarget.Grid);
            }

            if(target.unitType == CombatUnitType.Object)
            {
                return skillTargets.Contains(FantasyCombatTarget.Object);
            }

            //From this point Onwards, Target must be a Character Grid Unit
            CharacterGridUnit targetCharacter = target as CharacterGridUnit;

            if (skillTargets.Contains(FantasyCombatTarget.Self) && skillOwner == targetCharacter)
            {
                return true;
            }

            if (skillTargets.Contains(FantasyCombatTarget.Ally) && skillOwner.team == targetCharacter.team)
            {
                return true;
            }

            if (skillTargets.Contains(FantasyCombatTarget.Enemy) && skillOwner.team != targetCharacter.team)
            {
                return true;
            }

            return false;
        }

        public static bool IsUnitValidTarget(FantasyCombatTarget skillTarget, CharacterGridUnit skillOwner, GridUnit target)
        {
            List<FantasyCombatTarget> skillTargets = new List<FantasyCombatTarget> { skillTarget };
            return IsUnitValidTarget(skillTargets, skillOwner, target);
        }

        public static List<CharacterGridUnit> GetEligibleTargets(CharacterGridUnit skillOwner, List<FantasyCombatTarget> targetTypes)
        {
            List<CharacterGridUnit> eligibleTargets = new List<CharacterGridUnit>();

            foreach (CharacterGridUnit unit in FantasyCombatManager.Instance.GetAllCharacterCombatUnits(false))
            {
                if (IsUnitValidTarget(targetTypes, skillOwner, unit))
                {
                    eligibleTargets.Add(unit);
                }
            }

            return eligibleTargets;
        }

        public static List<CharacterGridUnit> GetEligibleTargets(CharacterGridUnit skillOwner, FantasyCombatTarget targetType)
        {
            List<CharacterGridUnit> eligibleTargets = new List<CharacterGridUnit>();
            List<FantasyCombatTarget> targetTypeList = new List<FantasyCombatTarget>();
            targetTypeList.Add(targetType);

            foreach (CharacterGridUnit unit in FantasyCombatManager.Instance.GetAllCharacterCombatUnits(false))
            {
                if (IsUnitValidTarget(targetTypeList, skillOwner, unit))
                {
                    eligibleTargets.Add(unit);
                }
            }

            return eligibleTargets;
        }
        //Common Methods
        public static void UpdateListIndex(int indexChange, int currentIndex, out int IndexToChange, int listCount)
        {
            int newIndex;

            if (currentIndex + indexChange >= listCount)
            {
                newIndex = 0;
            }
            else if (currentIndex + indexChange < 0)
            {
                newIndex = listCount - 1;
            }
            else
            {
                newIndex = currentIndex + indexChange;
            }

            IndexToChange = newIndex;
        }

        public static void UpdateGridIndex(Vector2 cursorDirection, ref int indexToUpdate, int maxColumnCount, int gridCount)
        {
            //Fix Index
            if (indexToUpdate >= gridCount)
                indexToUpdate = gridCount - 1;

            if(cursorDirection.x != 0)
            {
                if(cursorDirection.x > 0) //Moving Right:
                {
                    indexToUpdate = indexToUpdate == gridCount - 1 ?  0: indexToUpdate + 1;
                }
                else
                {
                    indexToUpdate = indexToUpdate == 0 ? gridCount - 1: indexToUpdate - 1;
                }
                
            }
            else if(cursorDirection.y != 0 && gridCount >= maxColumnCount)
            {
                if (cursorDirection.y < 0) //Moving Down
                {
                    if (indexToUpdate - maxColumnCount < 0)
                    {
                        //New index would be out of Bounds.

                        //0 1 2
                        //3 4 5
                        //6 7 

                        //0 + (2 * 3) = 6
                        //1 + (1  * 3) = 4
                        //2 + (2 * 3) = 8

                        int highestIndex = gridCount - 1;
                        int numRowsBelow = Mathf.FloorToInt((highestIndex - indexToUpdate) / maxColumnCount);
                        //New Index = currentIndex + (number Rows of below * number of columns)
                        indexToUpdate = indexToUpdate + (numRowsBelow * maxColumnCount);
                    }
                    else
                    {
                        indexToUpdate = indexToUpdate - maxColumnCount;
                    }
                }
                else
                {
                    if (indexToUpdate + maxColumnCount >= gridCount)
                    {
                        //New index would be out of Bounds.

                        //0 1 2
                        //3 4 5           
                        //6 7 8

                        //6 - (2 * 3) = 0
                        //8 - (2 * 3) = 2
                        //4 - (1 * 3) = 1

                        int numRowsAbove = Mathf.FloorToInt(indexToUpdate / maxColumnCount);
                        //New Index = currentIndex - (number Rows of above * number of columns)
                        indexToUpdate = indexToUpdate - (numRowsAbove * maxColumnCount);
                    }
                    else
                    {
                        indexToUpdate = indexToUpdate + maxColumnCount;
                    }
                }
            }
        }

        public static void OverrideCMTargetGroup(CinemachineTargetGroup targetGroup, List<Transform> tranformsToAdd, float weight, float radius)
        {
            foreach (var target in targetGroup.m_Targets)
            {
                targetGroup.RemoveMember(target.target);
            }

            foreach (Transform transform in tranformsToAdd)
            {
                targetGroup.AddMember(transform, weight, radius);
            }
        }

        public static Element GetElement(CharacterGridUnit unit, Element skillElement, bool isMagicalAttack)
        {
            if (isMagicalAttack)
            {
                return skillElement;
            }

            return unit.stats.GetAttackElement();
        }


        public static int GetAffinityIndex(Element element)
        {
            return ((int)element) - 1;

            /*if (element != Element.None)
            {
                switch (element)
                {
                    case Element.Fire:
                        return 3;
                    case Element.Ice:
                        return 4;
                    case Element.Air:
                        return 5;
                    case Element.Earth:
                        return 6;
                    case Element.Holy:
                        return 7;
                    default:
                        return 8;
                }
            }*/
        }

        public static int GetNonAffinityIndex(OtherSkillType skillType)
        {
            switch (skillType)
            {
                case OtherSkillType.Recovery:
                    return 9;
                case OtherSkillType.Support:
                    return 10;
                default:
                    return 11;
            }
        }

        public static int GetPotionIconIndex(Potion.PotionIcon potionIcon)
        {
            switch (potionIcon)
            {
                case Potion.PotionIcon.HP:
                    return 0;
                case Potion.PotionIcon.SP:
                    return 1;
                case Potion.PotionIcon.Buff:
                    return 2;
                case Potion.PotionIcon.FP:
                    return 3;
                default:
                    return -1;
            }
        }

        public static int GetSkillCostTypeIndex(PlayerBaseSkill.SkillCostType costType)
        {
            switch (costType)
            {
                case PlayerBaseSkill.SkillCostType.HP:
                    return 1;
                case PlayerBaseSkill.SkillCostType.FP:
                    return 2;
                default:
                    return 0;
            }
        }

        public static GridUnit GetClosestUnit(List<GridUnit> units, Transform origin)
        {
            float closestDistance = Mathf.Infinity;
            GridUnit closestUnit = null;

            foreach (GridUnit unit in units)
            {
                float calculatedDistance = Vector3.Distance(unit.transform.position, origin.position);
                if (calculatedDistance < closestDistance)
                {
                    closestDistance = calculatedDistance;
                    closestUnit = unit;
                }
            }

            return closestUnit;
        }

        public static Transform GetClosestTransform(Transform listHeader, Vector3 position)
        {
            float closestDistance = Mathf.Infinity;
            Transform closestTransform = null;

            foreach (Transform child in listHeader)
            {
                float calculatedDistance = Vector3.Distance(child.position, position);
                if (calculatedDistance < closestDistance)
                {
                    closestDistance = calculatedDistance;
                    closestTransform = child;
                }
            }

            return closestTransform;
        }

        public static CharacterGridUnit GetClosestUnitOfTypeOrDefault(CharacterGridUnit yourUnit, FantasyCombatTarget preferredTargetType)
        {
            //Filter By Preferred Target Type.
            List<CharacterGridUnit> allEligbleUnits = GetEligibleTargets(yourUnit, preferredTargetType);

            //If Empty Revert to enemies, as all allies could be dead but all enemies dead only when Battle is over.
            if (allEligbleUnits.Count == 0)
            {
                allEligbleUnits = GetEligibleTargets(yourUnit, FantasyCombatTarget.Enemy);
            }

            return GetClosestUnit(allEligbleUnits, yourUnit.transform);
        }


        public static CharacterGridUnit GetClosestUnit(List<CharacterGridUnit> units, Transform origin)
        {
            float closestDistance = Mathf.Infinity;
            CharacterGridUnit closestUnit = null;

            foreach (CharacterGridUnit unit in units)
            {
                float calculatedDistance = Vector3.Distance(unit.transform.position, origin.position);
                if (calculatedDistance < closestDistance)
                {
                    closestDistance = calculatedDistance;
                    closestUnit = unit;
                }
            }

            return closestUnit;
        }

        public static List<GridUnit> SetOffensiveSkillUnitsToShow(GridUnit attackingUnit, List<GridUnit> selectedUnits, int knockbackDistance)
        {
            List<GridUnit> targetedUnits = new List<GridUnit>(selectedUnits);
            targetedUnits.Add(attackingUnit);

            if (knockbackDistance > 0)
            {
                foreach (GridUnit unit in selectedUnits)
                {
                    GridUnit unitInRange = SkillForce.Instance.GetUnitInKnocbackRange(unit, knockbackDistance, RoundDirection((unit.transform.position - attackingUnit.transform.position).normalized));
                    if (unitInRange)
                    {
                        targetedUnits.Add(unitInRange);
                    }
                }
            }

            return targetedUnits;
        }

        public static MMF_Player GetTargetFeedback(AffinityFeedback feedbacks, Affinity affinity)
        {
            switch (affinity)
            {
                case Affinity.Absorb:
                    return feedbacks.attackAbsorbedFeedback;
                case Affinity.Reflect:
                    return feedbacks.attackReflectedFeedback;
                case Affinity.Evade:
                    return feedbacks.attackEvadedFeedback;
                case Affinity.Immune:
                    return feedbacks.attackNulledFeedback;
                default:
                    //For Weak, None, Resist
                    return feedbacks.attackConnectedFeedback;
            }
        }

        //UI Methods
        public static void VerticalScrollToHighlighted(RectTransform itemTransform, ScrollRect scrollRect, int currentIndex, int listCount)
        {
            if (currentIndex == 0)
            {
                scrollRect.verticalNormalizedPosition = 1;
            }
            else if (currentIndex == listCount - 1)
            {
                scrollRect.verticalNormalizedPosition = 0;
            }
            else
            {
                scrollRect.verticalNormalizedPosition = 1 - (Mathf.Abs(itemTransform.anchoredPosition.y) / scrollRect.content.rect.height);
            }
        }

        public static void VerticalScrollHighlightedInView(RectTransform itemTransform, ScrollRect scrollRect, int currentIndex, int listCount)
        {
            if (currentIndex == 0)
            {
                scrollRect.verticalNormalizedPosition = 1;
            }
            else if (currentIndex == listCount - 1)
            {
                scrollRect.verticalNormalizedPosition = 0;
            }
            else
            {
                //NOTES FOR THIS TO WORK
                //itemTransform Y Pivot must be 1;
                //Content Y Pivot must be 1;
                //Viewport Y Pivot must be 1;

                float viewPortHeight = scrollRect.viewport.rect.height;
                float contentAnchoredYpos = Mathf.Abs(scrollRect.content.anchoredPosition.y);

                float maxView = viewPortHeight + contentAnchoredYpos;
                float minView = contentAnchoredYpos;

                //Scroll to it if out of View
                if ((Mathf.Abs(itemTransform.anchoredPosition.y) + itemTransform.rect.height) > maxView || Mathf.Abs(itemTransform.anchoredPosition.y) < minView)
                    scrollRect.verticalNormalizedPosition = 1 - (Mathf.Abs(itemTransform.anchoredPosition.y) / scrollRect.content.rect.height);
            }
        }

        public static void HorizontallScrollToHighlighted(RectTransform itemTransform, ScrollRect scrollRect, int currentIndex, int listCount)
        {
            if (currentIndex == 0)
            {
                scrollRect.horizontalNormalizedPosition = 0;
            }
            else if (currentIndex == listCount - 1)
            {
                scrollRect.horizontalNormalizedPosition = 1;
            }
            else
            {
                scrollRect.horizontalNormalizedPosition = 0 + (Mathf.Abs(itemTransform.anchoredPosition.x) / scrollRect.content.rect.width);
            }
        }

        //Feedback
        public static void PlayAttackFeedback(Affinity attackAffinity, AffinityFeedback attackFeedbacks)
        {
            switch (attackAffinity)
            {
                case Affinity.Absorb:
                    attackFeedbacks.attackAbsorbedFeedback?.PlayFeedbacks();
                    break;
                case Affinity.Evade:
                    attackFeedbacks.attackEvadedFeedback?.PlayFeedbacks();
                    break;
                case Affinity.Reflect:
                    attackFeedbacks.attackReflectedFeedback?.PlayFeedbacks();
                    break;
                default:
                    //For None, Weak, Resist & Immnune.
                    attackFeedbacks.attackConnectedFeedback?.PlayFeedbacks();

                    if (!attackFeedbacks.attackConnectedFeedback)
                        Debug.Log("NO ATTACK CONNECTED FEEDBACK");

                    break;
            }
        }

        public static Transform GetVFXSpawnTransform(List<Transform> hitVFXSpawnOffsets, GridUnit target)
        {
            //We Calculate the closest Spawn Offset to Target
            float closestDistance = Mathf.Infinity;
            Transform closestOffset = null;

            foreach (Transform offset in hitVFXSpawnOffsets)
            {
                float calculatedDistance = Vector3.Distance(offset.transform.position, target.transform.position);

                if (calculatedDistance < closestDistance)
                {
                    closestDistance = calculatedDistance;
                    closestOffset = offset;
                }
            }

            return closestOffset;
        }

        public static Vector3 GetAttackLookDirection(GridUnit attacker, GridUnit target) //Returns Direction target should look at to face attacker.
        {
            bool isAttackDiagonal = IsAttackDiagonal(attacker, target);

            if (isAttackDiagonal)
            {
                //The Target is at a diagonal position.
                Vector3 attackDirection = (target.transform.position - attacker.transform.position).normalized;
                Direction intercardinalDirection = GetLocalDiagonalDirectionFromUnitForward(attackDirection, attacker.transform); //Based on Attacker's Forward.

                //Debug.Log("Attack Intercardinal Direction for: " + target.unitName + " " + intercardinalDirection.ToString());

                if (intercardinalDirection == Direction.NorthEast || intercardinalDirection == Direction.NorthWest)
                {
                    //Debug.Log("Facing: " + GetDirection(RoundDirectionToCardinalDirection(-attacker.transform.forward)).ToString());
                    return RoundDirectionToCardinalDirection(-attacker.transform.forward);
                }
                else
                {
                    //Debug.Log("Facing: " + GetDirection(RoundDirectionToCardinalDirection(attacker.transform.forward)).ToString());
                    return RoundDirectionToCardinalDirection(attacker.transform.forward);
                }
            }
            else
            {
                Vector3 attackLookDirection = (attacker.transform.position - target.transform.position).normalized;
                return RoundDirectionToCardinalDirection(attackLookDirection);
            }
        }

        public static bool IsAttackDiagonal(GridUnit attacker, GridUnit target)
        {
            GridPosition attackerPos = attacker.GetGridPositionsOnTurnStart()[0];
            GridPosition targetPos = target.GetGridPositionsOnTurnStart()[0];

            return !(attackerPos.x == targetPos.x || attackerPos.z == targetPos.z);
        }

        public static bool IsGridPositionAdjacent(GridPosition posA, GridPosition posB, bool includeDiagonals)
        {
            /*Equates to 1 if is 1 unit away from each other in any cardinal direction
              Equates to 2 if is 1 unit away from each other in any intercardinal direction */

            int xDifference = Mathf.Abs(posA.x - posB.x);
            int zDifference = Mathf.Abs(posA.z - posB.z);

            int value = xDifference + zDifference;

            if (includeDiagonals)
            {
                return value == 1 || (value == 2 && xDifference == zDifference);
            }

            return value == 1;
        }

        public static bool IsGridPositionOnDiagonalAxis(GridPosition posA, GridPosition posB)
        {
            return Mathf.Abs(posA.x - posB.x) == Mathf.Abs(posA.z - posB.z);
        }

        //Direction Methods

        public static Direction GetCardinalDirection(Vector3 directionVector)
        {
            if (Vector3.Angle(directionVector, Vector3.forward) < 45)
            {
                //Facing Vector3.Forward

                return Direction.North;
            }
            else if (Vector3.Angle(directionVector, Vector3.right) < 45)
            {
                //Facing Vector3.Right
                return Direction.East;
            }
            else if (Vector3.Angle(directionVector, Vector3.left) < 45)
            {
                //Facing Vector3.Left
                return Direction.West;
            }
            else if (Vector3.Angle(directionVector, Vector3.back) < 45)
            {
                //Facing Vector3.Back
                return Direction.South;
            }
            else
            {
                Debug.Log("UNIT FACING INVALID DIRECTION");
                return Direction.North;
            }
        }

        public static Direction GetCardinalDirection(Transform unitTransform)
        {
            if (Vector3.Angle(unitTransform.forward, Vector3.forward) < 45)
            {
                //Facing Vector3.Forward

                return Direction.North;
            }
            else if (Vector3.Angle(unitTransform.forward, Vector3.right) < 45)
            {
                //Facing Vector3.Right
                return Direction.East;
            }
            else if (Vector3.Angle(unitTransform.forward, Vector3.left) < 45)
            {
                //Facing Vector3.Left
                return Direction.West;
            }
            else if (Vector3.Angle(unitTransform.forward, Vector3.back) < 45)
            {
                //Facing Vector3.Back
                return Direction.South;
            }
            else
            {
                Debug.Log("UNIT FACING INVALID DIRECTION");
                return Direction.North;
            }
        }

        public static Direction GetDiagonalDirection(Transform unitTransform)
        {
            if (Vector3.Angle(unitTransform.forward, new Vector3(1, 0, 1)) < 45)
            {
                //Facing NorthEast
                return Direction.NorthEast;
            }
            else if (Vector3.Angle(unitTransform.forward, new Vector3(-1, 0, 1)) < 45)
            {
                //Facing SouthEast
                return Direction.SouthEast;

            }
            else if (Vector3.Angle(unitTransform.forward, new Vector3(-1, 0, -1)) < 45)
            {
                //Facing SouthWest
                return Direction.SouthWest;
            }
            else if (Vector3.Angle(unitTransform.forward, new Vector3(1, 0, -1)) < 45)
            {
                //Facing NorthWest
                return Direction.NorthWest;

            }
            else
            {
                return Direction.NorthEast;
            }
        }

        public static Direction GetDiagonalDirection(Vector3 direction)
        {
            if (Vector3.Angle(direction, new Vector3(1, 0, 1)) < 45)
            {
                //Facing NorthEast
                return Direction.NorthEast;
            }
            else if (Vector3.Angle(direction, new Vector3(-1, 0, 1)) < 45)
            {
                //Facing SouthEast
                return Direction.SouthEast;

            }
            else if (Vector3.Angle(direction, new Vector3(-1, 0, -1)) < 45)
            {
                //Facing SouthWest
                return Direction.SouthWest;
            }
            else if (Vector3.Angle(direction, new Vector3(1, 0, -1)) < 45)
            {
                //Facing NorthWest
                return Direction.NorthWest;

            }
            else
            {
                return Direction.NorthEast;
            }
        }

        public static Direction GetLocalDiagonalDirectionFromUnitForward(Vector3 direction, Transform unitTransform)
        {
            if (Vector3.Angle(direction, (unitTransform.forward + unitTransform.right).normalized) < 45)
            {
                //Facing NorthEast
                return Direction.NorthEast;
            }
            else if (Vector3.Angle(direction, (-unitTransform.forward + unitTransform.right).normalized) < 45)
            {
                //Facing SouthEast
                return Direction.SouthEast;

            }
            else if (Vector3.Angle(direction, (-unitTransform.forward + -unitTransform.right).normalized) < 45)
            {
                //Facing SouthWest
                return Direction.SouthWest;
            }
            else if (Vector3.Angle(direction, (unitTransform.forward + -unitTransform.right).normalized) < 45)
            {
                //Facing NorthWest
                return Direction.NorthWest;

            }
            else
            {
                Debug.Log("INVALID DIRECTION");
                return Direction.NorthEast;
            }
        }

        public static Vector3 GetCardinalDirectionAsVector(Transform unitTransform)
        {
            if (Vector3.Angle(unitTransform.forward, Vector3.forward) < 45)
            {
                //Facing Vector3.Forward

                return Vector3.forward;
            }
            else if (Vector3.Angle(unitTransform.forward, Vector3.right) < 45)
            {
                //Facing Vector3.Right
                return Vector3.right;
            }
            else if (Vector3.Angle(unitTransform.forward, Vector3.left) < 45)
            {
                //Facing Vector3.Left
                return Vector3.left;
            }
            else if (Vector3.Angle(unitTransform.forward, Vector3.back) < 45)
            {
                //Facing Vector3.Back
                return Vector3.back;
            }
            else
            {
                return Vector3.forward;
            }
        }

        public static Vector3 GetDirectionAsVector(Direction direction)
        {
            switch (direction)
            {
                case Direction.North:
                    return Vector3.forward;
                case Direction.NorthEast:
                    return new Vector3(1, 0, 1);
                case Direction.East:
                    return Vector3.right;
                case Direction.SouthEast:
                    return new Vector3(-1, 0, 1);
                case Direction.South:
                    return Vector3.back;
                case Direction.SouthWest:
                    return new Vector3(-1, 0, -1);
                case Direction.West:
                    return Vector3.left;
                case Direction.NorthWest:
                    return new Vector3(1, 0, -1);
                default:
                    return Vector3.forward;
            }
        }
        public static Direction GetDirectionFromVector(Vector3 directionVector)
        {
            float maxDifference = 22.5f;

            if (Vector3.Angle(directionVector, Vector3.forward) < maxDifference)
            {
                return Direction.North;
            }
            else if (Vector3.Angle(directionVector, Vector3.right) < maxDifference)
            {
                return Direction.East;
            }
            else if (Vector3.Angle(directionVector, Vector3.left) < maxDifference)
            {
                return Direction.West;
            }
            else if (Vector3.Angle(directionVector, Vector3.back) < maxDifference)
            {
                return Direction.South;
            }
            else if (Vector3.Angle(directionVector, new Vector3(1, 0, 1)) < maxDifference)
            {
                return Direction.NorthEast;
            }
            else if (Vector3.Angle(directionVector, new Vector3(-1, 0, 1)) < maxDifference)
            {
                return Direction.SouthEast;
            }
            else if (Vector3.Angle(directionVector, new Vector3(-1, 0, -1)) < maxDifference)
            {
                return Direction.SouthWest;
            }
            else if (Vector3.Angle(directionVector, new Vector3(1, 0, -1)) < maxDifference)
            {
                return Direction.NorthWest;

            }
            else
            {
                Debug.Log("ISSUE CALCULATING VECTOR");
                return Direction.North;
            }
        }

        public static Vector3 RoundDirection(Vector3 directionVector)
        {
            Direction direction = Direction.North;
            float maxDifference = 22.5f;

            if (Vector3.Angle(directionVector, Vector3.forward) < maxDifference)
            {
                direction = Direction.North;
            }
            else if (Vector3.Angle(directionVector, Vector3.right) < maxDifference)
            {
                direction = Direction.East;
            }
            else if (Vector3.Angle(directionVector, Vector3.left) < maxDifference)
            {
                direction = Direction.West;
            }
            else if (Vector3.Angle(directionVector, Vector3.back) < maxDifference)
            {
                direction = Direction.South;
            }
            else if (Vector3.Angle(directionVector, new Vector3(1, 0, 1)) < maxDifference)
            {
                direction = Direction.NorthEast;
            }
            else if (Vector3.Angle(directionVector, new Vector3(-1, 0, 1)) < maxDifference)
            {
                direction = Direction.SouthEast;
            }
            else if (Vector3.Angle(directionVector, new Vector3(-1, 0, -1)) < maxDifference)
            {
                direction = Direction.SouthWest;
            }
            else if (Vector3.Angle(directionVector, new Vector3(1, 0, -1)) < maxDifference)
            {
                direction = Direction.NorthWest;

            }
            else
            {
                Debug.Log("ISSUE CALCULATING VECTOR");
                direction = Direction.North;
            }

            return GetDirectionAsVector(direction);
        }

        public static GridPosition GetGridPositionInDirection(GridPosition originGridPos, Direction direction, int distance)
        {
            Vector3 directionVector = GetDirectionAsVector(direction) * distance;
            return new GridPosition(originGridPos.x + (int)directionVector.x, originGridPos.z + (int)directionVector.z);
        }

        public static GridPosition GetGridPositionInDirection(GridPosition originGridPos, Vector3 directionVector, int distance)
        {
            Direction direction = GetDirectionFromVector(directionVector);
            return GetGridPositionInDirection(originGridPos, direction, distance);
        }

        public static Vector3 RoundDirectionToCardinalDirection(Vector3 direction)
        {
            //Cardinal Directions
                //North, East, South, West
            //Intercardinal directions
                //Northeast (NE), southeast (SE), southwest (SW) and northwest (NW).
            return GetDirectionAsVector(GetCardinalDirection(direction));
        }

    }
}
