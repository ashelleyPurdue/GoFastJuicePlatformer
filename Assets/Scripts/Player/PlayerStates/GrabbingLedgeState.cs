using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class PlayerStateMachine
{
    private class GrabbingLedgeState : AbstractPlayerState
    {
        private float _timer;

        public GrabbingLedgeState(PlayerStateMachine shared, PlayerMotor motor)
            : base(shared, motor) {}

        public override void ResetState()
        {
            _timer = 0;
        }

        public override void OnStateEnter()
        {
            _timer = PlayerConstants.LEDGE_GRAB_DURATION;
            _sm.GrabbedLedge?.Invoke();
        }

        public override void EarlyFixedUpdate()
        {
            if (_timer <= 0)
                _sm.CurrentState = State.FreeFall;
        }
        
        public override void FixedUpdate()
        {
            _motor.RelativeVSpeed = PlayerConstants.LEDGE_GRAB_VSPEED;
            HSpeed = PlayerConstants.LEDGE_GRAB_HSPEED;
            SyncWalkVelocityToHSpeed();

            _timer -= Time.deltaTime;
        }
    }

}