using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(IPlayerInput))]
public class StickDisplay : MonoBehaviour
{
    private IPlayerInput _input;
    private Vector3 _lastStickDirection = Vector3.forward;

    private readonly float TWEEN_HALF_LIFE = 1f / 120;

    void Awake()
    {
        _input = GetComponent<IPlayerInput>();
    }

    void Update()
    {
        // Rotate with the left stick
        if (!IsLeftStickNeutral())
        {
            _lastStickDirection = InputUtils
                .LeftStickToWorldSpace(_input.LeftStick)
                .normalized;
        }

        var targetRot = Quaternion.LookRotation(_lastStickDirection, Vector3.up);
        transform.rotation = TweenUtils.DecayTowards(
            transform.rotation,
            targetRot,
            TWEEN_HALF_LIFE,
            Time.deltaTime
        );

        // Scale with the left stick
        var targetScale = Vector3.one * _input.LeftStick.magnitude;
        transform.localScale = TweenUtils.DecayTowards(
            transform.localScale,
            targetScale,
            TWEEN_HALF_LIFE,
            Time.deltaTime
        );
    }

    private bool IsLeftStickNeutral()
    {
        return _input.LeftStick.magnitude < PlayerConstants.LEFT_STICK_DEADZONE;
    }
}
