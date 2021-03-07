using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class PlayerMovement
{
    private abstract class AbstractPlayerState
    {
        protected PlayerMovement _shared;
        protected PlayerGroundDetector _ground => _shared._ground;
        protected PlayerWallDetector _wall => _shared._wall;
        protected PlayerLedgeDetector _ledge => _shared._ledge;

        protected IPlayerInput _input => _shared._input;

        protected float HAngleDeg
        {
            get => _shared.HAngleDeg;
            set => _shared.HAngleDeg = value;
        }
        protected float HSpeed
        {
            get => _shared.HSpeed;
            set => _shared.HSpeed = value;
        }
        protected float VSpeed
        {
            get => _shared.VSpeed;
            set => _shared.VSpeed = value;
        }
        
        public AbstractPlayerState(PlayerMovement shared)
        {
            _shared = shared;
        }

        public virtual void ResetState() {}

        public virtual void OnStateEnter() {}
        public virtual void OnStateExit() {}

        public virtual void EarlyFixedUpdate() {}
        public virtual void FixedUpdate() {}
        public virtual void LateFixedUpdate() {}

        protected void ChangeState(State newState)
        {
            _shared.ChangeState(newState);
        }

        protected void SyncWalkVelocityToHSpeed() => _shared.SyncWalkVelocityToHSpeed();
        protected void InstantlyFaceLeftStick() => _shared.InstantlyFaceLeftStick();

        protected bool WasGroundedRecently() => _shared.WasGroundedRecently();

        protected Vector3 GetWalkInput() => _shared.GetWalkInput();
        protected float GetHAngleDegInput() => _shared.GetHAngleDegInput();

        protected bool IsLeftStickNeutral() => _shared.IsLeftStickNeutral();
        protected float LeftStickForwardsComponent() => _shared.LeftStickForwardsComponent();

        protected bool JumpPressedRecently() => _shared.JumpPressedRecently();
        protected bool AttackPressedRecently() => _shared.AttackPressedRecently();
    }
}
