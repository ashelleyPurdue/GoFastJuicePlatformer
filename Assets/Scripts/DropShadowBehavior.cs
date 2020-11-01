using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DropShadowBehavior : MonoBehaviour
{
    public PlayerGroundDetector groundDetector;

    void FixedUpdate()
    {
        var pos = transform.localPosition;
        pos.y = -groundDetector.HeightAboveGround;
        transform.localPosition = pos;
    }
}
