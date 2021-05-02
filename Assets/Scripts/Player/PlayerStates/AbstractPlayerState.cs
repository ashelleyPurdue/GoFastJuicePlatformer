using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerStates
{
    public abstract class AbstractPlayerState
    {
        protected PlayerStateMachine _player;

        public AbstractPlayerState(PlayerStateMachine shared)
        {
            _player = shared;
        }

        public virtual void ResetState() {}

        public virtual void OnStateEnter() {}
        public virtual void OnStateExit() {}

        public virtual void EarlyFixedUpdate() {}
        public virtual void FixedUpdate() {}
    }
}
