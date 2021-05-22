using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathPlane : MonoBehaviour
{
    public Transform _player;

    void FixedUpdate()
    {
        if (_player.position.y < transform.position.y)
            CheckpointManager.Respawn();
    }

    void OnDrawGizmos()
    {
        // Draw a grid of wire cubes
        var prevColor = Gizmos.color;
        Gizmos.color = Color.red;

        const float length = 100;
        const float squareSize = 1;
        Vector3 squareSizeVec = new Vector3(
            squareSize / 2,
            0.1f,
            squareSize / 2
        );
        for (float x = -(length / 2); x < (length / 2); x += squareSize)
        {
            for (float z = -(length / 2); z < (length / 2); z += squareSize)
            {
                Vector3 pos = Camera.current.transform.position;

                pos.x = Mathf.Ceil(pos.x) + x;
                pos.y = transform.position.y;
                pos.z = Mathf.Ceil(pos.z) + z;

                Gizmos.DrawWireCube(pos, squareSizeVec);
            }
        }

        
        Gizmos.color = prevColor;
    }
}
