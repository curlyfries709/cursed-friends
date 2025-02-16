using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerBaseState : State
{
    protected PlayerStateMachine stateMachine;
    protected Transform transform;
    protected float inputMagnitude;


    protected PlayerBaseState(PlayerStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
        transform = stateMachine.transform;
    }

    protected void JumpGravityAndGrounded()
    {
        JumpAndGravity();
        GroundedCheck();
    }

    protected void EnterStateConfig(bool useFreeRoamCam, bool allowStealthModeSwitch, bool showWeapon, int animatorLayer)
    {
        stateMachine.ActivateFreeRoamCam(useFreeRoamCam);
        stateMachine.allowStealthMode = allowStealthModeSwitch;
        stateMachine.ShowWeapon(showWeapon);
        stateMachine.animator.ChangeLayers(animatorLayer);

        if (useFreeRoamCam)
        {
            if(stateMachine.moveRestrictor)
                stateMachine.moveRestrictor.OnUnwalkableAreaDetected += DisableMovement;

            StoryManager.Instance.ActivateCinematicMode += DisableMovement;
        }
    }

    protected void ExitStateConfig(bool movementCalledInUpdate)
    {
        if (movementCalledInUpdate)
        {
            stateMachine.moveRestrictor.OnUnwalkableAreaDetected -= DisableMovement;
            StoryManager.Instance.ActivateCinematicMode -= DisableMovement;
        }      
    }

    //Basic Movement Variables
    protected void Move(bool canSprint, float walkSpeed, float runSpeed)
    {
        // set target speed based on move speed, sprint speed and if sprint is pressed
        float targetSpeed;

        if (stateMachine.IsAutoMoving)
        {
            targetSpeed = stateMachine.AutoMoveSpeed;
        }
        else
        {
            targetSpeed = canSprint ? runSpeed : walkSpeed;
        }

        // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

        // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        // if there is no input, set the target speed to 0
        if (stateMachine.moveValue == Vector2.zero && !stateMachine.IsAutoMoving) targetSpeed = 0.0f;

        // a reference to the players current horizontal velocity
        float currentHorizontalSpeed = new Vector3(stateMachine.controller.velocity.x, 0.0f, stateMachine.controller.velocity.z).magnitude;

        float speedOffset = 0.1f;
        inputMagnitude = ControlsManager.Instance.IsMoveInputAnalog() ? stateMachine.moveValue.magnitude : 1f;

        // accelerate or decelerate to target speed
        if (currentHorizontalSpeed < targetSpeed - speedOffset ||
            currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            // creates curved result rather than a linear one giving a more organic speed change
            // note T in Lerp is clamped, so we don't need to clamp our speed
            stateMachine.speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                Time.deltaTime * stateMachine.SpeedChangeRate);

            // round speed to 3 decimal places
            stateMachine.speed = Mathf.Round(stateMachine.speed * 1000f) / 1000f;
        }
        else
        {
            stateMachine.speed = targetSpeed;
        }

        // normalise input direction
        Vector3 inputDirection = new Vector3(stateMachine.moveValue.x, 0.0f, stateMachine.moveValue.y).normalized;

        // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        // if there is a move input rotate player when the player is moving
        if (stateMachine.moveValue != Vector2.zero)
        {
            stateMachine.targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                              stateMachine.mainCam.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, stateMachine.targetRotation, ref stateMachine.rotationVelocity,
                stateMachine.RotationSmoothTime);

            // rotate to face input direction relative to camera position
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }


        Vector3 targetDirection = Quaternion.Euler(0.0f, stateMachine.targetRotation, 0.0f) * Vector3.forward;

        if (IsMoveDirectionAllowed())
        {
            // move the player
            stateMachine.controller.Move(targetDirection.normalized * (stateMachine.speed * Time.deltaTime) +
                             new Vector3(0.0f, stateMachine.verticalVelocity, 0.0f) * Time.deltaTime);
        }
        else if(!stateMachine.IsAutoMoving)
        {
            targetSpeed = 0f;
        }

        stateMachine.animationBlend = Mathf.Lerp(stateMachine.animationBlend, targetSpeed, Time.deltaTime * stateMachine.SpeedChangeRate);
        if (stateMachine.animationBlend < 0.01f) stateMachine.animationBlend = 0f;

    }

    protected void DisableMovement(bool disableMovement)
    {
        stateMachine.disableMovement = disableMovement;

        if (disableMovement)
        {
            //Reset Movement
            stateMachine.ResetMoveValue();
        }
    }

    private bool IsMoveDirectionAllowed()
    {
        return !stateMachine.disableMovement;
    }


    private void JumpAndGravity()
    {
        if (stateMachine.Grounded)
        {
            // reset the fall timeout timer
            stateMachine.fallTimeoutDelta = stateMachine.FallTimeout;

            // update animator if using character
            //_animator.SetBool(_animIDJump, false);
            //_animator.SetBool(_animIDFreeFall, false);

            // stop our velocity dropping infinitely when grounded
            if (stateMachine.verticalVelocity < 0.0f)
            {
                stateMachine.verticalVelocity = -2f;
            }

            // Jump
            if (stateMachine.inputJump && stateMachine.jumpTimeoutDelta <= 0.0f)
            {
                // the square root of H * -2 * G = how much velocity needed to reach desired height
                stateMachine.verticalVelocity = Mathf.Sqrt(stateMachine.JumpHeight * -2f * stateMachine.Gravity);

                // update animator if using character
                //_animator.SetBool(_animIDJump, true);
            }

            // jump timeout
            if (stateMachine.jumpTimeoutDelta >= 0.0f)
            {
                stateMachine.jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            // reset the jump timeout timer
            stateMachine.jumpTimeoutDelta = stateMachine.JumpTimeout;

            // fall timeout
            if (stateMachine.fallTimeoutDelta >= 0.0f)
            {
                stateMachine.fallTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                // update animator if using character
                //stateMachine._animator.SetBool(stateMachine._animIDFreeFall, true);
            }

            // if we are not grounded, do not jump
            stateMachine.inputJump = false;
        }

        // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
        if (stateMachine.verticalVelocity < stateMachine.terminalVelocity)
        {
            stateMachine.verticalVelocity += stateMachine.Gravity * Time.deltaTime;
        }
    }

    private void GroundedCheck()
    {
        // set sphere position, with offset
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - stateMachine.GroundedOffset,
            transform.position.z);
        stateMachine.Grounded = Physics.CheckSphere(spherePosition, stateMachine.GroundedRadius, stateMachine.GroundLayers,
            QueryTriggerInteraction.Ignore);

        // update animator if using character
        /*if (_hasAnimator)
        {
            _animator.SetBool(_animIDGrounded, Grounded);
        }*/
    }


}
