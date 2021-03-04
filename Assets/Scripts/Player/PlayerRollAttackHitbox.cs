using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRollAttackHitbox : MonoBehaviour
{
    private const float HITBOX_HEIGHT = PlayerConstants.BODY_HEIGHT / 2;
    private const float HITBOX_DEPTH = PlayerConstants.BODY_RADIUS + 0.5f;
    private const float HITBOX_WIDTH = PlayerConstants.BODY_RADIUS * 2;

    private const float Y_OFFSET = 0.1f;    // How far off the ground the hitbox is

    private float _displayTimer = 0;

    void Update()
    {
        // DEBUG: Draw the box
        if (_displayTimer > 0)
        {
            DebugDisplay.DrawCube(
                Color.red,
                GetBoxCenter(),
                GetBoxHalfExtents(),
                GetBoxOrientation()
            );
        }
        _displayTimer -= Time.deltaTime;
    }

    public void ApplyDamage()
    {
        // Find everything inside the hitbox
        var hits = Physics.OverlapBox(
            GetBoxCenter(),
            GetBoxHalfExtents(),
            GetBoxOrientation()
        );

        // Send the damaged event to all hits
        foreach (var hit in hits)
        {
            // Don't damange ourselves
            if (hit.transform.root == transform.root)
                continue;

            hit.transform.SendMessage("OnDamaged", SendMessageOptions.DontRequireReceiver);
        }

        // Enable the display
        _displayTimer = Time.deltaTime;
    }

    private Vector3 GetBoxCenter()
    {
        var forward = GetComponent<PlayerMovement>().Forward;
        var orientation = Quaternion.LookRotation(forward);

        var pos = transform.position;
        pos.y += HITBOX_HEIGHT / 2;
        pos.y += Y_OFFSET;

        pos += forward * (HITBOX_DEPTH / 2);

        return pos;
    }

    private Vector3 GetBoxHalfExtents()
    {
        return new Vector3(
            HITBOX_WIDTH / 2,
            HITBOX_HEIGHT / 2,
            HITBOX_DEPTH / 2
        );
    }

    private Quaternion GetBoxOrientation()
    {
        var forward = GetComponent<PlayerMovement>().Forward;
        return Quaternion.LookRotation(forward);
    }
}
