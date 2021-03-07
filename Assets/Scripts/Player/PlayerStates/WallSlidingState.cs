using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class PlayerMovement
{
    private class WallSlidingState : AbstractPlayerState
    {
        public WallSlidingState(PlayerMovement shared)
            : base(shared) {}

        public override void EarlyFixedUpdate()
        {
            bool keepWallSliding = 
                !_ground.IsGrounded &&
                _wall.IsTouchingWall &&
                _shared.Forward.ComponentAlong(-_wall.LastWallNormal) > 0 &&
                VSpeed < 0;

            if (keepWallSliding)
                ChangeState(State.WallSliding);
            else if (_ground.IsGrounded)
                ChangeState(State.Walking);
            else
                ChangeState(State.FreeFall);
        }

        public override void FixedUpdate()
        {
            Physics();
            Controls();
        }

        private void Physics()
        {
            // Apply gravity
            float gravity = _shared._wallSlideGravity;
            VSpeed -= gravity * Time.deltaTime;

            if (VSpeed < PlayerConstants.TERMINAL_VELOCITY_WALL_SLIDE)
                VSpeed = PlayerConstants.TERMINAL_VELOCITY_WALL_SLIDE;

            // Cancel all walking velocity pointing "inside" the wall.
            // We're intentionally letting _walkVelocity and HSpeed get out of sync
            // here, so that the original HSpeed will be resumed when we wall kick.
            _shared._walkVelocity = _shared._walkVelocity.ProjectOnPlane(_wall.LastWallNormal);

            // Apply horizontal friction, since sliding on a wall naturally slows
            // you down.
            float slidingHSpeed = _shared._walkVelocity.magnitude;
            slidingHSpeed -= PlayerConstants.FRICTION_WALL_SLIDE * Time.deltaTime;
            if (slidingHSpeed < 0)
                slidingHSpeed = 0;

            _shared._walkVelocity = slidingHSpeed * _shared._walkVelocity.normalized;
        }

        private void Controls()
        {
            // Wall kick when we press the jump button
            if (JumpPressedRecently())
                _shared.StartWallJump();
        }
    }
}