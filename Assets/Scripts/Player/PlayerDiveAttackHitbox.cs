using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDiveAttackHitbox : MonoBehaviour
{
    private const float SPHERE_RADIUS = 0.5f;
    private const float HEIGHT_OFFSET = PlayerConstants.BODY_RADIUS;

    private float _lastApplyDamageTime;

    void OnDrawGizmos()
    {
        // Only display the sphere if ApplyDamage() was called on the last
        // FixedUpdate.
        if (Time.fixedTime > _lastApplyDamageTime + Time.fixedDeltaTime)
            return;

        // GetSphereCenter() only works in play mode, because it accesses TotalVelocity.
        // Trying to use it in edit mode results in a null reference exception.
        if (!Application.isPlaying)
            return;

        var oldColor = Gizmos.color;
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(GetSphereCenter(), SPHERE_RADIUS);
        Gizmos.color = oldColor;
    }

    public void ApplyDamage()
    {
        var hits = GetHits();
        foreach (var hit in hits)
        {
            // Don't damange ourselves
            if (hit.transform.root == transform.root)
                continue;

            hit.transform.SendMessage("OnDamaged", SendMessageOptions.DontRequireReceiver);
        }

        // Enable the display
        _lastApplyDamageTime = Time.fixedTime;
    }

    private Collider[] GetHits()
    {
        return Physics.OverlapSphere(GetSphereCenter(), SPHERE_RADIUS);
    }

    private Vector3 GetSphereCenter()
    {
        var dir = GetComponent<PlayerMovement>().TotalVelocity.normalized;
        return transform.position + (dir * HEIGHT_OFFSET);
    }
}
