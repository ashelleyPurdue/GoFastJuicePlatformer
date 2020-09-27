using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputImpl : MonoBehaviour, IPlayerInput
{
    public Vector2 LeftStick => new Vector3(
        Input.GetAxisRaw("Horizontal"),
        Input.GetAxisRaw("Vertical")
    );

    public bool JumpHeld => Input.GetButton("Jump");
    public bool JumpPressed => Input.GetButtonDown("Jump");
}
