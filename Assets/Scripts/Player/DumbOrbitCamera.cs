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
    private const float ZOOM_SPEED = 10;

    // Services
    private IPlayerInput _input;

    // State variables
    private float _hAngleDeg;
    private float _vAngleDeg;
    private float _zoomDistance = ORBIT_RADIUS;

    void Awake()
    {
        _input = GetComponent<IPlayerInput>();
    }

    void Update()
    {
        DebugDisplay.PrintLine(_input.RightStick.ToString());

        // Adjust the angles with the right stick
        Vector3 rightStick = _input.RightStick;
        if (INVERT_HORIZONTAL) rightStick.x *= -1;
        if (INVERT_VERTICAL) rightStick.y *= -1;

        _hAngleDeg += rightStick.x * MAX_ROTSPEED_DEG * Time.deltaTime;
        _vAngleDeg += rightStick.y * MAX_ROTSPEED_DEG * Time.deltaTime;

        if (_vAngleDeg > MAX_VANGLE_DEG)
            _vAngleDeg = MAX_VANGLE_DEG;

        if (_vAngleDeg < MIN_VANGLE_DEG)
            _vAngleDeg = MIN_VANGLE_DEG;

        // Calculate the where the position would be (if we didn't zoom)
        Vector3 dir = SphericalToCartesian(_hAngleDeg, _vAngleDeg, 1);
        Vector3 pos = _target.position + (dir * ORBIT_RADIUS);

        // Zoom in if the view is obstructed
        float targetZoomDistance = GetTargetZoomDistance(pos, dir);

        _zoomDistance = Mathf.MoveTowards(
            _zoomDistance,
            targetZoomDistance,
            ZOOM_SPEED * Time.deltaTime
        );

        pos = _target.position + (_zoomDistance * dir);
        
        // Jump to the position and look at the thing
        transform.position = pos;
        transform.LookAt(_target);
    }

    private float GetTargetZoomDistance(
        Vector3 pos,
        Vector3 dir
    )
    {
        RaycastHit[] hits = Physics.RaycastAll(
            pos,
            -dir,
            ORBIT_RADIUS
        );

        // If there were no obstructions, use the default distance
        if (hits.Length == 0)
            return ORBIT_RADIUS;

        // Find the obstruction that's closest to the player
        // (IE: furthest from the camera)
        float furthestDist = float.MinValue;
        foreach (var hit in hits)
        {
            if (hit.distance > furthestDist)
                furthestDist = hit.distance;
        }
        
        return ORBIT_RADIUS - furthestDist;
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
