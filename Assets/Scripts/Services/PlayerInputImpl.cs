using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputImpl : MonoBehaviour, IPlayerInput
{
    public Vector2 LeftStick => new Vector2(
        Input.GetAxisRaw("Horizontal"),
        Input.GetAxisRaw("Vertical")
    );

    public Vector2 RightStick => new Vector2(
        Input.GetAxisRaw("RightStickX"),
        Input.GetAxisRaw("RightStickY")
    );

    public bool JumpHeld => Input.GetButton("Jump");
    public bool JumpPressed => Input.GetButtonDown("Jump");
}
