using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpinningPlatform : MonoBehaviour
{
    // Degrees per second
    public Vector3 RotSpeed = Vector3.zero;

    public void FixedUpdate()
    {
        transform.Rotate(RotSpeed * Time.deltaTime);
    }
}
