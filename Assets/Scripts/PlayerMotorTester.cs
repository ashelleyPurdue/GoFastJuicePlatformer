using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerMotor))]
[RequireComponent(typeof(IPlayerInput))]
public class PlayerMotorTester : MonoBehaviour
{
    public const float MAX_SPEED = 10;

    private PlayerMotor _motor;
    private IPlayerInput _input;

    void Awake()
    {
        _motor = GetComponent<PlayerMotor>();
        _input = GetComponent<IPlayerInput>();
    }

    void FixedUpdate()
    {
        _motor.RelativeVelocity = _input.LeftStick * MAX_SPEED;
        _motor.Move();
    }
}
