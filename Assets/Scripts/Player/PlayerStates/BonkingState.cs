using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerStates
{
    public class BonkingState : AbstractPlayerState
    {
        private float _timer;
        private int _bounceCount;

        public BonkingState(PlayerStateMachine shared)
            : base(shared) {}

        public override void ResetState()
        {
            _timer = 0;
            _bounceCount = 0;
        }

        public override void OnStateEnter()
        {
            _player.Anim.Set(PlayerAnims.BONK);

            _player.Motor.RelativeVSpeed = PlayerConstants.BONK_START_VSPEED;
            _player.HSpeed = PlayerConstants.BONK_START_HSPEED;
            _player.HAngleDeg = _player.GetHAngleDegFromForward(-_player.Motor.LastWallNormal);
            _player.SyncWalkVelocityToHSpeed();

            _timer = PlayerConstants.BONK_DURATION;
            _bounceCount = 0;
        }

        public override void EarlyFixedUpdate()
        {
            if (_timer <= 0)
            {
                if (!_player.Motor.IsGrounded)
                    _player.ChangeState(_player.FreeFall);
                else
                    _player.ChangeState(_player.Walking);
            }
        }

        public override void FixedUpdate()
        {
            // Apply gravity
            _player.Motor.RelativeVSpeed -= PlayerConstants.BONK_GRAVITY * Time.deltaTime;
            if (_player.Motor.RelativeVSpeed < PlayerConstants.TERMINAL_VELOCITY_AIR)
                _player.Motor.RelativeVSpeed = PlayerConstants.TERMINAL_VELOCITY_AIR;

            // Bounce against the floor
            if (_player.Motor.IsGrounded && _player.Motor.RelativeVSpeed < 0 && _bounceCount < PlayerConstants.BONK_MAX_BOUNCE_COUNT)
            {
                _player.Motor.RelativeVSpeed *= -PlayerConstants.BONK_BOUNCE_MULTIPLIER;
                _bounceCount++;
            }

            // Apply friction
            float bonkFriction = Mathf.Abs(PlayerConstants.BONK_START_HSPEED / PlayerConstants.BONK_SLOW_TIME);
            _player.HSpeed = Mathf.MoveTowards(_player.HSpeed, 0, bonkFriction * Time.deltaTime);
            _player.SyncWalkVelocityToHSpeed();

            // Tick the timer down.  It starts after we've bounced once.
            if (_bounceCount >= 1)
                _timer -= Time.deltaTime;
        }
    }


}