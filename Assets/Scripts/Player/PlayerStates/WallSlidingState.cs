using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class PlayerStateMachine
{
    private class WallSlidingState : AbstractPlayerState
    {
        public WallSlidingState(PlayerStateMachine shared, PlayerMotor motor)
            : base(shared, motor) {}

        public override AnimationHint GetAnimationHint() => AnimationHint.WallSliding;

        public override void EarlyFixedUpdate()
        {
            bool keepWallSliding = 
                !_motor.IsGrounded &&
                _motor.IsTouchingWall &&
                Forward.ComponentAlong(-_motor.LastWallNormal) > 0 &&
                _motor.RelativeVSpeed < 0;

            if (keepWallSliding)
                ChangeState(_sm.WallSliding);
            else if (_motor.IsGrounded)
                ChangeState(_sm.Walking);
            else
                ChangeState(_sm.FreeFall);
        }

        public override void FixedUpdate()
        {
            Physics();
            Controls();
        }

        private void Physics()
        {
            // Apply gravity
            float gravity = PlayerConstants.WALL_SLIDE_GRAVITY;
            _motor.RelativeVSpeed -= gravity * Time.deltaTime;

            if (_motor.RelativeVSpeed < PlayerConstants.TERMINAL_VELOCITY_WALL_SLIDE)
                _motor.RelativeVSpeed = PlayerConstants.TERMINAL_VELOCITY_WALL_SLIDE;

            // Cancel all walking velocity pointing "inside" the wall.
            // We're intentionally letting _walkVelocity and HSpeed get out of sync
            // here, so that the original HSpeed will be resumed when we wall kick.
            _motor.RelativeFlatVelocity = _motor.RelativeFlatVelocity.ProjectOnPlane(_motor.LastWallNormal);

            // Apply horizontal friction, since sliding on a wall naturally slows
            // you down.
            float slidingHSpeed = _motor.RelativeFlatVelocity.magnitude;
            slidingHSpeed -= PlayerConstants.FRICTION_WALL_SLIDE * Time.deltaTime;
            if (slidingHSpeed < 0)
                slidingHSpeed = 0;

            _motor.RelativeFlatVelocity = slidingHSpeed * _motor.RelativeFlatVelocity.normalized;
        }

        private void Controls()
        {
            // Wall kick when we press the jump button
            if (JumpPressedRecently())
                StartWallJump();
        }
    }
}