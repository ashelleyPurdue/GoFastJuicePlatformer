using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CircleSweep : MonoBehaviour
{
    private static CircleSweep _instance = null;
    private static readonly Vector3 STASH_POSITION = new Vector3(
        float.MaxValue,
        float.MaxValue,
        float.MaxValue
    );
    private static readonly Vector3 STASH_FORWARD = Vector3.forward;
    private static readonly Vector3 STASH_SCALE = Vector3.zero;

    public static RaycastHit[] SweepTestAll(
        Vector3 origin,
        Vector3 direction,
        float radius,
        float maxDistance,
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal
    )
    {
        EnsureCreated();

        var rigidbody = _instance.GetComponent<Rigidbody>();
        try
        {
            // Un-stash the rigidbody and move it into position.
            rigidbody.position = origin;
            rigidbody.rotation = Quaternion.LookRotation(direction, Vector3.up);
            _instance.transform.localScale = Vector3.one * radius;

            // Do the sweep cast
            return _instance.GetComponent<Rigidbody>().SweepTestAll(
                direction,
                maxDistance,
                queryTriggerInteraction
            );
        }
        finally
        {
            // Move back to the far-off corner, so it can't accidentally interact
            // with anything
            rigidbody.position = STASH_POSITION;
            rigidbody.rotation = Quaternion.identity;
            _instance.transform.localScale = Vector3.one;
        }
    }

    private static void EnsureCreated()
    {
        if (_instance != null)
            return;

        var prefab = Resources.Load<GameObject>("Prefabs/circle_sweeper");
        var obj = GameObject.Instantiate(prefab, STASH_POSITION, Quaternion.identity);
        _instance = obj.GetComponent<CircleSweep>();
        var rigidbody = _instance.GetComponent<Rigidbody>();

        // Stash it away in a far corner, so it can't interact with anything
        rigidbody.position = STASH_POSITION;
        rigidbody.rotation = Quaternion.identity;
        _instance.transform.localScale = STASH_SCALE;
    }
}
