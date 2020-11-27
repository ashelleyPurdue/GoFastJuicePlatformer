using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerLedgeDetector : MonoBehaviour
{
    public bool UpperBodyTouchingWall {get; private set;}
    public bool LowerBodyTouchingWall {get; private set;}

    public void UpdateLedgeDetectorState()
    {
        // TODO: Fix this to depend on HAngle instead.
        var forward = GetComponent<PlayerMovement>().Forward;

        // Do 2 box casts in front of us: one for our upper body, and one for
        // our lower body.
        // The lower body should detect a wall, while the upper body should not.

        const float bodyRadius = 0.5f;
        const float bodyHeight = 2;
        const float distance = 0.13f;

        var lowerBodyStart = transform.position 
            + (forward * bodyRadius)
            + (forward * distance / 2)
            + (Vector3.up * bodyHeight / 4);
        var upperBodyStart = lowerBodyStart + (Vector3.up * bodyHeight);

        var halfExtents = new Vector3(
            bodyRadius,
            bodyHeight / 4,
            distance / 2
        );

        var orientation = Quaternion.LookRotation(forward, Vector3.up);

        LowerBodyTouchingWall = Physics.CheckBox(
            lowerBodyStart,
            halfExtents,
            orientation
        );
        UpperBodyTouchingWall = Physics.CheckBox(
            upperBodyStart,
            halfExtents,
            orientation
        );
    }
}
