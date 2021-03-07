using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class PlayerMovement
{
    private class FreeFallState : AbstractPlayerState
    {
        public FreeFallState(PlayerMovement shared)
            : base(shared) {}

        public override void ResetState() {}

        public override void EarlyFixedUpdate()
        {
            // Transition to walking if we're on the ground
            if (_ground.IsGrounded)
            {
                ChangeState(State.Walking);

                // Reduce the HSpeed based on the stick magnitude.
                // This lets you avoid sliding(AKA: "sticking" the landing) by
                // moving the left stick to neutral.
                // This doesn't take away *all* of your momentum, because that would
                // look stiff and unnatural.  
                float hSpeedMult = _input.LeftStick.magnitude + PlayerConstants.MIN_LANDING_HSPEED_MULT;
                if (hSpeedMult > 1)
                    hSpeedMult = 1;
                HSpeed *= hSpeedMult;

                return;
            }

            // Transition to either ledge grabbing or wall sliding
            bool isWallSliding =
                VSpeed < 0 &&
                _wall.IsTouchingWall &&
                _shared.Forward.ComponentAlong(-_wall.LastWallNormal) > 0;

            bool inLedgeGrabSweetSpot = 
                _ledge.LedgePresent &&
                _ledge.LastLedgeHeight >= PlayerConstants.BODY_HEIGHT / 2 &&
                _ledge.LastLedgeHeight <= PlayerConstants.BODY_HEIGHT;

            if (isWallSliding && inLedgeGrabSweetSpot)
            {
                ChangeState(State.LedgeGrabbing);
                return;
            }

            if (isWallSliding && !inLedgeGrabSweetSpot)
            {
                ChangeState(State.WallSliding);
                return;
            }
        }
        public override void FixedUpdate()
        {
            // DEBUG: Record stats
            if (_shared.transform.position.y > _shared._debugJumpMaxY)
                _shared._debugJumpMaxY = _shared.transform.position.y;

            Physics();
            StrafingControls();
            ButtonControls();
        }
        
        protected void Physics()
        {
            // Apply gravity
            // Use more gravity when we're falling so the jump arc feels "squishier"
            float gravity = VSpeed > 0
                ? _shared._riseGravity
                : _shared._fallGravity;

            VSpeed -= gravity * Time.deltaTime;

            // Cap the VSpeed at the terminal velocity
            if (VSpeed < PlayerConstants.TERMINAL_VELOCITY_AIR)
                VSpeed = PlayerConstants.TERMINAL_VELOCITY_AIR;

            // Start going downwards if you bonk your head on the ceiling.
            // Don't bonk your head!
            if (VSpeed > 0 && _ground.IsBonkingHead)
                VSpeed = PlayerConstants.BONK_SPEED;
        }
        protected void ButtonControls()
        {
            if (!_input.JumpHeld)
                _shared._jumpReleased = true;

            // Cut the jump short if the button was released on the way u
            // Immediately setting the VSpeed to 0 looks jarring, so instead we'll
            // exponentially decay it every frame.
            // Once it's decayed below a certain threshold, we'll let gravity do the
            // rest of the work so it still looks natural.
            if (VSpeed > (_shared._jumpSpeed / 2) && _shared._jumpReleased)
                VSpeed *= PlayerConstants.SHORT_JUMP_DECAY_RATE;

            // Let the player jump for a short period after walking off a ledge,
            // because everyone is human.  
            // This is called "coyote time", named after the tragic life of the late
            // Wile E. Coyote.
            if (VSpeed < 0 && WasGroundedRecently() && JumpPressedRecently())
            {
                _shared.StartGroundJump();
                DebugDisplay.PrintLine("Coyote-time jump!");
            }

            // Dive when the attack button is pressed.
            if (AttackPressedRecently())
            {
                ChangeState(State.Diving);
                return;
            }
        }
        protected void StrafingControls()
        {
            // Allow the player to change their direction for free for a short time
            // after jumping.  After that time is up, air strafing controls kick in
            if (_shared._jumpRedirectTimer >= 0)
            {
                InstantlyFaceLeftStick();
                SyncWalkVelocityToHSpeed();

                _shared._jumpRedirectTimer -= Time.deltaTime;
                return;
            }

            // In the air, we let the player "nudge" their velocity by applying a
            // force in the direction the stick is being pushed.
            // Unlike on the ground, you *will* lose speed and slide around if you
            // try to change your direction.
            var inputVector = GetWalkInput();

            Vector3 forward = _shared.AngleForward(HAngleDeg);
            bool pushingBackwards = inputVector.ComponentAlong(forward) < -0.5f;
            bool pushingForwards = inputVector.ComponentAlong(forward) > 0.75f;
            bool movingForwards = _shared._walkVelocity.normalized.ComponentAlong(forward) > 0;

            float accel = PlayerConstants.HACCEL_AIR;
            float maxSpeed = PlayerConstants.HSPEED_MAX_AIR;

            // Reduce the speed limit when moving backwards.
            // If you're wanna go fast, you gotta go forward.
            if (!movingForwards)
                maxSpeed = PlayerConstants.HSPEED_MAX_GROUND;

            // Give them a little bit of help if they're pushing backwards
            // on the stick, so it's easier to "abort" a poorly-timed jum
            if (pushingBackwards)
                accel = PlayerConstants.HACCEL_AIR_BACKWARDS;

            // Apply a force to get our new velocity.
            var oldVelocity = _shared._walkVelocity;
            var newVelocity = _shared._walkVelocity + (inputVector * accel * Time.deltaTime);
            
            // Only let the player accellerate up to the normal ground speed.
            // We won't slow them down if they're already going faster than that,
            // though (eg: due to the speed boost from chained jumping)
            float oldSpeed = oldVelocity.magnitude;
            float newSpeed = newVelocity.magnitude;

            bool wasAboveGroundSpeedLimit = oldSpeed > PlayerConstants.HSPEED_MAX_GROUND;
            bool nowAboveGroundSpeedLimit = newSpeed > PlayerConstants.HSPEED_MAX_GROUND;

            if (newSpeed > oldSpeed)
            {
                if (wasAboveGroundSpeedLimit)
                    newSpeed = oldSpeed;
                else if (nowAboveGroundSpeedLimit)
                    newSpeed = PlayerConstants.HSPEED_MAX_GROUND;
            }

            // We WILL, however, slow them down if they're going past the max air
            // speed.  That's a hard maximum.
            if (newSpeed > maxSpeed)
                _shared._walkVelocity = _shared._walkVelocity.normalized * maxSpeed;

            _shared._walkVelocity = newVelocity.normalized * newSpeed;

            // Keep HSpeed up-to-date, so it'll be correct when we land.
            HSpeed = _shared._walkVelocity.ComponentAlong(_shared.Forward);
        }
    }
}