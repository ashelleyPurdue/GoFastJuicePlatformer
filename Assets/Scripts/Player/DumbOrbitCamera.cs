using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    private const float ZOOM_IN_SPEED = 50;
    private const float ZOOM_OUT_SPEED = 10;

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
        float targetZoomDistance = GetZoomDistance(
            pos,
            _target.position,
            _target.position + (Vector3.up * PlayerConstants.BODY_HEIGHT)
        );

        float zoomSpeed = targetZoomDistance < _zoomDistance
            ? ZOOM_IN_SPEED
            : ZOOM_OUT_SPEED;

        _zoomDistance = Mathf.MoveTowards(
            _zoomDistance,
            targetZoomDistance,
            zoomSpeed * Time.deltaTime
        );

        pos = _target.position + (_zoomDistance * dir);
        
        // Jump to the position and look at the thing
        transform.position = pos;
        transform.LookAt(_target);
    }

    private float GetZoomDistance(
        Vector3 unzoomedCameraPos,
        Vector3 targetFeetPos,
        Vector3 targetHeadPos
    )
    {
        const int MAX_ITERATIONS = 10;

        Vector3 cameraPos = unzoomedCameraPos;
        for (int i = 0; i < MAX_ITERATIONS; i++)
        {
            RaycastHit headHit;
            RaycastHit feetHit;

            bool isHeadObstructed = Physics.Raycast(
                cameraPos,
                (targetHeadPos - cameraPos).normalized,
                out headHit,
                Vector3.Distance(targetHeadPos, cameraPos)
            );

            bool areFeetObstructed = Physics.Raycast(
                cameraPos,
                (targetFeetPos - cameraPos).normalized,
                out feetHit,
                Vector3.Distance(targetFeetPos, cameraPos)
            );

            // If either the head or the feet are visible, then this is a good
            // zoom distance.
            if (!isHeadObstructed || !areFeetObstructed)
                return Vector3.Distance(cameraPos, targetFeetPos);

            // Both the head and the feet are blocked, so we need to zoom in
            // more.  There are two points we can zoom to from here:
            // * A point determined by the thing obstructing the head
            // * A point determined by the thing obstructing the feet
            // We will choose whichever point is closer to the camera pos.

            // Find the point determined by the head obstructor.
            var headDelta = Vector3.Project(
                headHit.point - cameraPos,
                targetFeetPos - cameraPos
            );
            Vector3 headZoomPos = cameraPos + headDelta;
            float headZoomPosDist = Vector3.Distance(cameraPos, headZoomPos);

            // Find the point determined by the feet obstructor
            Vector3 feetZoomPos = feetHit.point;
            float feetZoomPosDist = feetHit.distance;

            // Move to whichever one is closest to the camera
            if (feetZoomPosDist <= headZoomPosDist)
                cameraPos = feetZoomPos;
            else
                cameraPos = headZoomPos;
        }

        // We couldn't find a good zoom point, so just give up.
        return Vector3.Distance(cameraPos, targetFeetPos);
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
