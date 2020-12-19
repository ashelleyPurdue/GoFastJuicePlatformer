using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(IPlayerInput))]
public class DumbOrbitCamera : MonoBehaviour
{
    // Inspector parameters
    public Transform _target;

    // Constants
    private const bool INVERT_HORIZONTAL = true;
    private const bool INVERT_VERTICAL = true;

    private const float MAX_ROTSPEED_DEG = 180;
    private const float MIN_VANGLE_DEG = -180;
    private const float MAX_VANGLE_DEG = 180;
    private const float ORBIT_RADIUS = 15;

    // Services
    private IPlayerInput _input;

    // State variables
    private float _hAngleDeg;
    private float _vAngleDeg;

    void Awake()
    {
        _input = GetComponent<IPlayerInput>();
    }

    void Update()
    {
        DebugDisplay.PrintLine(_input.RightStick.ToString());

        // Orbit with the right stick
        Vector3 rightStick = _input.RightStick;
        if (INVERT_HORIZONTAL) rightStick.x *= -1;
        if (INVERT_VERTICAL) rightStick.y *= -1;

        _hAngleDeg += rightStick.x * MAX_ROTSPEED_DEG * Time.deltaTime;
        _vAngleDeg += rightStick.y * MAX_ROTSPEED_DEG * Time.deltaTime;

        if (_vAngleDeg > MAX_VANGLE_DEG)
            _vAngleDeg = MAX_VANGLE_DEG;

        if (_vAngleDeg < MIN_VANGLE_DEG)
            _vAngleDeg = MIN_VANGLE_DEG;

        // Jump to the position
        Vector3 offset = SphericalToCartesian(_hAngleDeg, _vAngleDeg, ORBIT_RADIUS);
        transform.position = _target.position + offset;

        // Look at the thing
        transform.LookAt(_target);
    }

    private Vector3 SphericalToCartesian(float hAngleDeg, float vAngleDeg, float orbitRaidus)
    {
        float hAngleRad = hAngleDeg * Mathf.Deg2Rad;
        float vAngleRad = vAngleDeg * Mathf.Deg2Rad;

        float hRadius = Mathf.Cos(vAngleRad) * orbitRaidus;
        float height = Mathf.Sin(vAngleRad) * orbitRaidus;

        return new Vector3(
            Mathf.Cos(hAngleRad) * hRadius,
            height,
            Mathf.Sin(hAngleRad) * hRadius
        );
    }
}
