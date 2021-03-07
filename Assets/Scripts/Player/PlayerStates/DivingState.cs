using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class PlayerMovement
{
    private class DivingState : AbstractPlayerState
    {
        public DivingState(PlayerMovement shared)
            : base(shared) {}

        public override void OnStateEnter()
        {
            InstantlyFaceLeftStick();

            HSpeed = PlayerConstants.DIVE_HSPEED_INITIAL;
            VSpeed = _shared._diveJumpVspeed;

            _shared._chainedJumpCount = 0;
            _shared.StartedDiving?.Invoke();
        }

        public override void EarlyFixedUpdate()
        {
            // Roll when we hit the ground
            if (_ground.IsGrounded)
                ChangeState(State.Rolling);

            // Bonk if we hit a wall
            if (_shared.ShouldBonkAgainstWall())
                ChangeState(State.Bonking);
        }
        public override void FixedUpdate()
        {
            // Damage things
            _shared._diveHitbox.ApplyDamage();

            // Apply gravity
            // Use more gravity when we're falling so the jump arc feels "squishier"
            VSpeed -= PlayerConstants.DIVE_GRAVITY * Time.deltaTime;

            // TODO: This logic is copy/pasted from WhileAirborn().  Refactor.
            // Cap the VSpeed at the terminal velocity
            if (VSpeed < PlayerConstants.TERMINAL_VELOCITY_AIR)
                VSpeed = PlayerConstants.TERMINAL_VELOCITY_AIR;

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