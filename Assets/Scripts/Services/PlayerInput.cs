using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlayerInput
{
    /// <summary>
    /// The direction (and distance) the left stick is being pushed
    /// </summary>
    /// <value></value>
    Vector2 LeftStick {get;}

    /// <summary>
    /// Whether or not the jump button is currently being held
    /// </summary>
    /// <value></value>
    bool JumpHeld {get;}

    /// <summary>
    /// Whether or not the jump button was pressed this frame
    /// </summary>
    /// <value></value>
    bool JumpPressed {get;}
}