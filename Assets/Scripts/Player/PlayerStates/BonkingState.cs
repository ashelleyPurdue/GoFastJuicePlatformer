using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class PlayerStateMachine
{
    private class BonkingState : AbstractPlayerState
    {
        private float _timer;
        private int _bounceCount;

        public BonkingState(PlayerStateMachine shared, PlayerMotor motor)
            : base(shared, motor) {}

        public override void ResetState()
        {
            _timer = 0;
            _bounceCount = 0;
        }

        public override void OnStateEnter()
        {
            _sm._anim.Set(PlayerAnims.BONK);

            _motor.RelativeVSpeed = PlayerConstants.BONK_START_VSPEED;
            HSpeed = PlayerConstants.BONK_START_HSPEED;
            HAngleDeg = GetHAngleDegFromForward(-_motor.LastWallNormal);
            SyncWalkVelocityToHSpeed();

            _timer = PlayerConstants.BONK_DURATION;
            _bounceCount = 0;
        }

        public override void EarlyFixedUpdate()
        {
            if (_timer <= 0)
            {
                if (!_motor.IsGrounded)
                    ChangeState(_sm.FreeFall);
                else
                    ChangeState(_sm.Walking);
            }
        }

        public override void FixedUpdate()
        {
            // Apply gravity
            _motor.RelativeVSpeed -= PlayerConstants.BONK_GRAVITY * Time.deltaTime;
            if (_motor.RelativeVSpeed < PlayerConstants.TERMINAL_VELOCITY_AIR)
                _motor.RelativeVSpeed = PlayerConstants.TERMINAL_VELOCITY_AIR;

            // Bounce against the floor
            if (_motor.IsGrounded && _motor.RelativeVSpeed < 0 && _bounceCount < PlayerConstants.BONK_MAX_BOUNCE_COUNT)
            {
                _motor.RelativeVSpeed *= -PlayerConstants.BONK_BOUNCE_MULTIPLIER;
                _bounceCount++;
            }

            // Apply friction
            float bonkFriction = Mathf.Abs(PlayerConstants.BONK_START_HSPEED / PlayerConstants.BONK_SLOW_TIME);
            HSpeed = Mathf.MoveTowards(HSpeed, 0, bonkFriction * Time.deltaTime);
            SyncWalkVelocityToHSpeed();

            // Tick the timer down.  It starts after we've bounced once.
            if (_bounceCount >= 1)
                _timer -= Time.deltaTime;
        }
    }


}