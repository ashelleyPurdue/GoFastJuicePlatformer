using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class PlayerStateMachine
{
    private class DivingState : AbstractPlayerState
    {
        public DivingState(PlayerStateMachine shared, PlayerMotor motor)
            : base(shared, motor) {}

        public override void OnStateEnter()
        {
            _sm._anim.Set(PlayerAnims.DIVE, 0.1f);

            InstantlyFaceLeftStick();

            HSpeed = PlayerConstants.DIVE_HSPEED_INITIAL;
            _motor.RelativeVSpeed = PlayerConstants.DIVE_JUMP_VSPEED;

            _sm._chainedJumpCount = 0;
        }

        public override void OnStateExit()
        {
            _sm._anim.ForwardTiltAngleDeg = 0;
        }

        public override void EarlyFixedUpdate()
        {
            // Roll when we hit the ground
            if (_motor.IsGrounded)
                ChangeState(_sm.Rolling);

            // Bonk if we hit a wall
            if (ShouldBonkAgainstWall())
                ChangeState(_sm.Bonking);
        }

        public override void FixedUpdate()
        {
            // Point the model in the direction we're moving
            _sm._anim.ForwardTiltAngleDeg =
                Quaternion.LookRotation(_sm._motor.TotalVelocity.normalized)
                .eulerAngles
                .x;

            // Damage things
            _sm._diveHitbox.ApplyDamage();

            // Apply gravity
            // Use more gravity when we're falling so the jump arc feels "squishier"
            _motor.RelativeVSpeed -= PlayerConstants.DIVE_GRAVITY * Time.deltaTime;

            // TODO: This logic is copy/pasted from WhileAirborn().  Refactor.
            // Cap the VSpeed at the terminal velocity
            if (_motor.RelativeVSpeed < PlayerConstants.TERMINAL_VELOCITY_AIR)
                _motor.RelativeVSpeed = PlayerConstants.TERMINAL_VELOCITY_AIR;

            // Reduce HSpeed until it's at the minimum
            // If the player is pushing backwards on the left stick, reduce the speed
            // faster and let them slow down more
            float initSpeed = PlayerConstants.DIVE_HSPEED_INITIAL;
            float finalSpeed = PlayerConstants.DIVE_HSPEED_FINAL_MAX;
            float slowTime = PlayerConstants.DIVE_HSPEED_SLOW_TIME;
            
            float stickBackwardsComponent = -LeftStickForwardsComponent();
            if (stickBackwardsComponent > 0)
            {
                finalSpeed = Mathf.Lerp(
                    PlayerConstants.DIVE_HSPEED_FINAL_MAX,
                    PlayerConstants.DIVE_HSPEED_FINAL_MIN,
                    stickBackwardsComponent
                );
            }

            float friction = (initSpeed - finalSpeed) / slowTime;
            HSpeed -= friction * Time.deltaTime;
            if (HSpeed < finalSpeed)
                HSpeed = finalSpeed;

            SyncWalkVelocityToHSpeed();
        }
    }

}