using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerStates
{
    public class StandardJumpingState : AbstractPlayerState
    {
        protected virtual float MinDuration => PlayerConstants.STANDARD_JUMP_MIN_DURATION;

        public StandardJumpingState(PlayerStateMachine player) : base(player) {}

        public override void OnStateEnter()
        {
            // HACK: Double jump instead
            bool isDoubleJump = _player.DoubleJumpArmed && _player.IsInDoubleJumpWindow();
            if (isDoubleJump)
            {
                _player.ChangeState(_player.DoubleJumping);
                return;
            }

            _player.Motor.RelativeVSpeed = PlayerConstants.STANDARD_JUMP_VSPEED;
            _player.InstantlyFaceLeftStick();

            // If we just recently landed, restore their stored hspeed
            if (_player.IsInDoubleJumpWindow())
                _player.HSpeed = _player.StoredAirHSpeed;

            _player.SyncWalkVelocityToHSpeed();

            // Book keeping
            _player.RecordJumpStarted();

            // Trigger animation
            _player.Anim.Set(PlayerAnims.STANDARD_JUMP);
        }

        public override void EarlyFixedUpdate()
        {
            // Transition to free falling if we're moving downwards
            if (_player.Motor.RelativeVSpeed <= 0)
            {
                _player.ChangeState(_player.FreeFall);
                
                // HACK: Do FreeFall's state changes in the same frame, to
                // preserve the old behavior from before the refactor.
                _player.FreeFall.EarlyFixedUpdate();
                return;
            }
        }

        public override void FixedUpdate()
        {
            _player.DebugRecordWhileJumping();

            Physics();
            _player.AirStrafingControls();
            ButtonControls();
        }

        protected void Physics()
        {
            // Apply gravity
            _player.Motor.RelativeVSpeed -= PlayerConstants.JUMP_RISE_GRAVITY * Time.deltaTime;

            // Start going downwards if you bonk your head on the ceiling.
            // Don't bonk your head!
            if (_player.Motor.IsBonkingHead)
            {
                _player.Motor.RelativeVSpeed = PlayerConstants.BONK_SPEED;
                _player.ChangeState(_player.FreeFall);
            }
        }

        protected void ButtonControls()
        {
            if (!_player.Input.JumpHeld)
                _player.JumpReleased = true;

            // Cut the jump short if the button was released on the way up
            // Immediately setting the VSpeed to 0 looks jarring, so instead
            // we'll exponentially decay it every frame.
            // Once it's decayed below a certain threshold, we'll let gravity do 
            // the rest of the work so it still looks natural.
            bool shouldDecay =
                _player.JumpReleased &&
                IsPastMinDuration() &&
                _player.Motor.RelativeVSpeed > (PlayerConstants.STANDARD_JUMP_VSPEED / 2);

            if (shouldDecay)
                _player.Motor.RelativeVSpeed *= PlayerConstants.SHORT_JUMP_DECAY_RATE;
            
            // Dive when the attack button is pressed.
            if (_player.AttackPressedRecently())
            {
                _player.ChangeState(_player.Diving);
                return;
            }
        }

        private bool IsPastMinDuration()
        {
            return (Time.time - _player.LastJumpStartTime >= MinDuration);
        }
    }
}