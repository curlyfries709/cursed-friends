using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FantasyCombatMovement : MonoBehaviour
{
    [Header("Player Unit")]
    [Tooltip("How fast the character turns to face movement direction")]
    [Range(0.0f, 0.3f)]
    public float RotationSmoothTime = 0.12f;
    public float rotationSpeed = 10f;

    [Tooltip("Acceleration and deceleration")]
    public float SpeedChangeRate = 10.0f;

    //FootSteps Audio
    //public AudioClip LandingAudioClip;
    //public AudioClip[] FootstepAudioClips;
    //[Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    [Space(10)]
    [Tooltip("The height the player can jump")]
    public float JumpHeight = 1.2f;

    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    public float Gravity = -15.0f;

    [Space(10)]
    [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
    public float JumpTimeout = 0.50f;

    [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
    public float FallTimeout = 0.15f;

    [Header("Player Grounded")]
    [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
    public bool Grounded = true;

    [Tooltip("Useful for rough ground")]
    public float GroundedOffset = -0.14f;

    [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    public float GroundedRadius = 0.28f;

    [Tooltip("What layers the character uses as ground")]
    public LayerMask GroundLayers;

    [Header("Grid Movement Behaviour")]
    [SerializeField] LayerMask gridVisualLayer;
    [SerializeField] float raycastLength;
    [SerializeField] float allowAngleDifferenceBetweenForwardAndBannedDirection = 2f;

    //Important Variables
    private int currentMovingUnitMaxMoveDistance;
    private CharacterGridUnit currentUnit;

    // player
    private float currentSpeed;
    private float animationBlend;
    private float targetRotation = 0.0f;
    
    private float rotationVelocity;
    private float verticalVelocity;
    private float _terminalVelocity = 53.0f;
    private Quaternion targetRotationQua;

    private bool rotationOnlyMode = false;

    // timeout deltatime
    private float jumpTimeoutDelta;
    private float fallTimeoutDelta;

    //Input Variables
    private Vector2 inputMoveValue;
    private bool inputSprint;
    private bool inputJump;

    //Cache
    PlayerInput playerInput; 
    GridSystem<GridObject> gridSystem;
    Camera mainCam;

    private void Awake()
    {
        playerInput = ControlsManager.Instance.GetPlayerInput();
        mainCam = Camera.main;
    }

    private void OnEnable()
    {
        playerInput.onActionTriggered += OnMove;
        playerInput.onActionTriggered += OnSprint;
    }

    // Start is called before the first frame update
    void Start()
    {
        gridSystem = LevelGrid.Instance.gridSystem;

        // reset our timeouts on start
        jumpTimeoutDelta = JumpTimeout;
        fallTimeoutDelta = FallTimeout;
    }


    //Movement Functionality
    public void BasicUnitMovement(bool enableMovement)
    {
        if(currentUnit is PlayerGridUnit)
        {
            JumpAndGravity();
            UnitGroundedCheck();

            MoveSelectedUnit(enableMovement);   
        }
    }

    public void SetMovementAsCurrentPosOnly(CharacterGridUnit selectedUnit)
    {
        rotationOnlyMode = true;

        SetMovingUnit(selectedUnit);
        GridSystemVisual.Instance.ShowValidMovementGridPositions(selectedUnit.GetGridPositionsOnTurnStart(), currentUnit, currentUnit is PlayerGridUnit);
    }

    public void ShowMovementGridPos(CharacterGridUnit selectedUnit)
    {
        rotationOnlyMode = false;

        SetMovingUnit(selectedUnit);
        GridSystemVisual.Instance.ShowValidMovementGridPositions(GetValidMovementGridPositions(selectedUnit), currentUnit, currentUnit is PlayerGridUnit);
    }

    private void RotateSelectedUnit()
    {

        PlayerGridUnit currentPlayerUnit = currentUnit as PlayerGridUnit;
        // normalise input direction
        Vector3 inputDirection = new Vector3(inputMoveValue.x, 0.0f, inputMoveValue.y).normalized;

        if (inputMoveValue == Vector2.zero) { return; }

        targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + mainCam.transform.eulerAngles.y;
        float rotation = Mathf.SmoothDampAngle(currentPlayerUnit.transform.eulerAngles.y, targetRotation, ref rotationVelocity, RotationSmoothTime);

        // rotate to face input direction relative to camera position
        currentPlayerUnit.transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);

    }

    private void MoveSelectedUnit(bool enableMovement)
    {
        if(rotationOnlyMode)
        {
            RotateSelectedUnit();
            return;
        }


        PlayerGridUnit currentPlayerUnit = currentUnit as PlayerGridUnit;

        CharacterController controller = currentPlayerUnit.unitController;

        // set target speed based on move speed, sprint speed and if sprint is pressed
        //float targetSpeed = inputSprint ? currentUnit.sprintSpeed : currentUnit.moveSpeed;
        float targetSpeed = currentPlayerUnit.moveSpeed;

        // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

        // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        // if there is no input, set the target speed to 0
        if (inputMoveValue == Vector2.zero) targetSpeed = 0.0f;

        // a reference to the players current horizontal velocity
        float currentHorizontalSpeed = new Vector3(controller.velocity.x, 0.0f, controller.velocity.z).magnitude;

        float speedOffset = 0.1f;
        float inputMagnitude = ControlsManager.Instance.IsMoveInputAnalog() ? inputMoveValue.magnitude : 1f;

        // accelerate or decelerate to target speed
        if (currentHorizontalSpeed < targetSpeed - speedOffset ||
            currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            // creates curved result rather than a linear one giving a more organic speed change
            // note T in Lerp is clamped, so we don't need to clamp our speed
            currentSpeed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                Time.deltaTime * SpeedChangeRate);

            // round speed to 3 decimal places
            currentSpeed = Mathf.Round(currentSpeed * 1000f) / 1000f;
        }
        else
        {
            currentSpeed = targetSpeed;
        }

        // normalise input direction
        Vector3 inputDirection = new Vector3(inputMoveValue.x, 0.0f, inputMoveValue.y).normalized;

        // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        // if there is a move input rotate player when the player is moving
        if (inputMoveValue != Vector2.zero && enableMovement)
        {
            targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                              mainCam.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(currentPlayerUnit.transform.eulerAngles.y, targetRotation, ref rotationVelocity,
                RotationSmoothTime);

            // rotate to face input direction relative to camera position
            currentPlayerUnit.transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }

        Vector3 targetDirection = Quaternion.Euler(0.0f, targetRotation, 0.0f) * Vector3.forward;

        // move the unit
        if (IsMoveDirectionAllowed(targetDirection) && enableMovement)
        {
            controller.Move(targetDirection.normalized * (currentSpeed * Time.deltaTime) +
                                     new Vector3(0.0f, verticalVelocity, 0.0f) * Time.deltaTime);
        }
        else
        {
            targetSpeed = 0f;
        }

        animationBlend = Mathf.Lerp(animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
        if (animationBlend < 0.01f) animationBlend = 0f;


        // update unit animator
        currentPlayerUnit.unitAnimator.SetSpeed(animationBlend);
        currentPlayerUnit.unitAnimator.SetMotionSpeed(inputMagnitude);
    }

    private bool IsMoveDirectionAllowed(Vector3 direction)
    {
        PlayerGridUnit currentPlayerUnit = currentUnit as PlayerGridUnit;

        Transform raycastPoint = currentPlayerUnit.raycastPoint;

        Debug.DrawRay(raycastPoint.position, Vector3.down * raycastLength, Color.red);

        if (Physics.Raycast(raycastPoint.transform.position, Vector3.down, raycastLength, gridVisualLayer))
        {
            return true;
        }

        return false;

        



        //Debug.DrawRay(raycastPoint.position, direction * raycastLength, Color.red);

        /*if (Physics.Raycast(raycastPoint.transform.position, direction, out RaycastHit hitInfo, raycastLength))
        {
            if (hitInfo.collider.CompareTag("InvisibleCube"))
            {
                return false;
            }
        }*/
        /*if ((currentUnit.unitController.collisionFlags & CollisionFlags.Sides) != 0)
        {
            Debug.Log("Angle Difference: " + Vector3.Angle(direction.normalized, currentUnit.transform.forward));
            return !(Vector3.Angle(direction.normalized, currentUnit.transform.forward) < allowAngleDifferenceBetweenForwardAndBannedDirection);
        }*/
    }

    private void UnitGroundedCheck()
    {
        // set sphere position, with offset
        Vector3 spherePosition = new Vector3(currentUnit.transform.position.x, currentUnit.transform.position.y - GroundedOffset,
            currentUnit.transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
            QueryTriggerInteraction.Ignore);

        // update animator if using character
        currentUnit.unitAnimator.SetBool(currentUnit.unitAnimator.animIDGrounded, Grounded);
    }

    private void JumpAndGravity()
    {
        if (Grounded)
        {
            // reset the fall timeout timer
            fallTimeoutDelta = FallTimeout;

            //currentUnit.unitAnimator.SetJump(false);
            //currentUnit.unitAnimator.SetFreeFall(false);

            // stop our velocity dropping infinitely when grounded
            if (verticalVelocity < 0.0f)
            {
                verticalVelocity = -2f;
            }

            // Jump
            if (inputJump && jumpTimeoutDelta <= 0.0f)
            {
                // the square root of H * -2 * G = how much velocity needed to reach desired height
                verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                // update animator if using character
                //currentUnit.unitAnimator.SetJump(true);
            }

            // jump timeout
            if (jumpTimeoutDelta >= 0.0f)
            {
                jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            // reset the jump timeout timer
            jumpTimeoutDelta = JumpTimeout;

            // fall timeout
            if (fallTimeoutDelta >= 0.0f)
            {
                fallTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                // update animator if using character
                //currentUnit.unitAnimator.SetFreeFall(true);
            }

            // if we are not grounded, do not jump
            inputJump = false;
        }

        // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
        if (verticalVelocity < _terminalVelocity)
        {
            verticalVelocity += Gravity * Time.deltaTime;
        }
    }
    

    private void OnDisable()
    {
        playerInput.onActionTriggered -= OnMove;
        playerInput.onActionTriggered -= OnSprint;
    }

    //Input Methods

    private void OnMove(InputAction.CallbackContext context)
    {
        if(context.action.name != "Move") { return; }
        inputMoveValue = context.ReadValue<Vector2>();
    }

    private void OnSprint(InputAction.CallbackContext context)
    {
        if (context.action.name != "Sprint") { return; }
        if (context.performed)
        {
            inputSprint = true;
        }
        else if (context.canceled)
        {
            inputSprint = false;
        }
    }





    //Getters && Setters
    /*public bool IsValidMovementGridPosition(GridPosition gridPosition)
    {
        return GetValidMovementGridPositions().Contains(gridPosition);
    }*/

    public List<GridPosition> GetValidMovementGridPositions(CharacterGridUnit unit)
    {
        List<GridPosition> validGridPositionsList = new List<GridPosition>();
        List<GridPosition> unitGridPositions = unit.GetGridPositionsOnTurnStart();

        int unitMaxMoveDistance = unit.MoveRange();

        for (int x = -unitMaxMoveDistance; x <= unitMaxMoveDistance; x++)
        {
            for(int z = -unitMaxMoveDistance; z <= unitMaxMoveDistance; z++)
            {
                GridPosition offsetGridPosition = new GridPosition(x, z);

                foreach (GridPosition unitGridPosition in unitGridPositions)
                {
                    GridPosition testGridPosition = unitGridPosition + offsetGridPosition;
                    if (!gridSystem.IsValidGridPosition(testGridPosition)) 
                    {
                        continue; 
                    }

                    GridObject gridObject = gridSystem.GetGridObject(testGridPosition);

                    if (PathFinding.Instance.IsMovementGridPositionOccupiedByAnotherUnit(testGridPosition, unit))
                    {
                        continue;
                    }

                    if (!PathFinding.Instance.IsWalkable(testGridPosition))
                    {
                        continue;
                    }

                    if (!PathFinding.Instance.HasPath(unitGridPosition, testGridPosition, unit, false))
                    {
                        continue;
                    }


                    if(PathFinding.Instance.DistanceInGridUnits(unitGridPosition, testGridPosition, unit) > unitMaxMoveDistance)
                    {
                        //Path Lenghth is too long
                        continue;
                    }

                    //Calculate Manhattan distance
                    //abs(x1 - x2) + abs(y1 - y2)

                    if (Mathf.Abs(unitGridPosition.x - testGridPosition.x) + Mathf.Abs(unitGridPosition.z - testGridPosition.z) <= unitMaxMoveDistance)
                    {
                        if(!validGridPositionsList.Contains(testGridPosition))
                            validGridPositionsList.Add(testGridPosition);
                    }
                }
            }
        }
        return validGridPositionsList;
    }



    private void SetMovingUnit(CharacterGridUnit unit)
    {
        currentUnit = unit;
        currentMovingUnitMaxMoveDistance = unit.MoveRange();
    }
}
