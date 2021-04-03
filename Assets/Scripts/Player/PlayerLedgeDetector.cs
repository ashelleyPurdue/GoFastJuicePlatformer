using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLedgeDetector : MonoBehaviour
{
    private const float BIG_NUMBER = 10000;
    
    public bool LedgePresent {get; private set;}
    public float LastLedgeHeight {get; private set;}

    public void UpdateLedgeDetectorState()
    {
        const float bodyRadius = PlayerConstants.BODY_RADIUS;
        const float distance = 0.13f;

        // TODO: Add comments, some of them clever.
        RaycastHit? ceilingHit = CylinderPhysics.CircleCast(
            transform.position,
            bodyRadius,
            BIG_NUMBER,
            Vector3.up
        );
        float ceilingHeight = ceilingHit?.distance ?? BIG_NUMBER;

        Vector3 echoStart = transform.position + (Vector3.up * ceilingHeight);
        RaycastHit? echoHit = CylinderPhysics.CircleCast(
            echoStart,
            bodyRadius + distance,
            BIG_NUMBER,
            Vector3.down
        );

        LedgePresent = echoHit.HasValue;
        if (LedgePresent)
            LastLedgeHeight = echoHit.Value.point.y - transform.position.y;
    }
}
