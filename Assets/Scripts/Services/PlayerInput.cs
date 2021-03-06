﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlayerInput
{
    /// <summary>
    /// The direction (and distance) the left stick is being pushed
    /// </summary>
    /// <value></value>
    Vector2 LeftStick {get;}

    Vector2 RightStick {get;}

    /// <summary>
    /// Whether or not the jump button is currently being held
    /// </summary>
    /// <value></value>
    bool JumpHeld {get;}

    /// <summary>
    /// Whether or not the jump button was pressed this update frame
    /// </summary>
    /// <value></value>
    bool JumpPressed {get;}

    /// <summary>
    /// Whether or not the attack button is currently being held
    /// </summary>
    /// <value></value>
    bool AttackHeld {get;}

    /// <summary>
    /// Whether or not the attack button was pressed this update frame
    /// </summary>
    /// <value></value>
    bool AttackPressed {get;}

    /// <summary>
    /// Whether or not the "slow time" button is being held.
    /// This is really only intended as a cheat used during development, to find
    /// bugs that require precise timing.
    /// </summary>
    /// <value></value>
    bool CheatSlowTimeHeld {get;}
}