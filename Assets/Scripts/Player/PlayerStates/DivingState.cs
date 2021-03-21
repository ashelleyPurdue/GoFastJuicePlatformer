using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class PlayerStateMachine
{
    private class DivingState : AbstractPlayerState
    {
        public DivingState(PlayerStateMachine shared, PlayerMotor motor)
            : base(shared, motor) {}

        public override AnimationHint GetAnimationHint() => AnimationHint.Diving;

        public override void OnStateEnter()
        {
            InstantlyFaceLeftStick();

            HSpeed = PlayerConstants.DIVE_HSPEED_INITIAL;
            _motor.RelativeVSpeed = PlayerConstants.DIVE_JUMP_VSPEED;

            _sm._chainedJumpCount = 0;
            _sm.StartedDiving?.Invoke();
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