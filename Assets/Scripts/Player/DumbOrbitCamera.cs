using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(IPlayerInput))]
public class DumbOrbitCamera : MonoBehaviour
{
    // Inspector parameters
    public Transform _target;
    public Material _obstructorMaterial;

    // Constants
    private const bool INVERT_HORIZONTAL = true;
    private const bool INVERT_VERTICAL = true;

    private const float MAX_ROTSPEED_DEG = 180;
    private const float MIN_VANGLE_DEG = -180;
    private const float MAX_VANGLE_DEG = 180;
    private const float ORBIT_RADIUS = 15;

    // Services
    private IPlayerInput _input;

    // State variables
    private float _hAngleDeg;
    private float _vAngleDeg;

    private Dictionary<Renderer, Material> _originalMaterials
        = new Dictionary<Renderer, Material>();

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

        // Jump to the position and look at the thing
        transform.position = pos;
        transform.LookAt(_target);

        ChangeObstructingObjectMaterials();
    }

    private void ChangeObstructingObjectMaterials()
    {
        // Return all previous obstructing objects to their original material
        foreach (Renderer obst in _originalMaterials.Keys)
            obst.material = _originalMaterials[obst];
        _originalMaterials.Clear();

        if (_obstructorMaterial == null)
            return;

        // Get all the objects that are currently obstructing our vision
        var obstructors = GetObstructingObjects();

        // Set them to the special obstructor material
        foreach (Renderer obst in obstructors)
        {
            var originalMat = obst.material;
            _originalMaterials.Add(obst, originalMat);
            
            var replacementMat = new Material(_obstructorMaterial);
            replacementMat.CopyPropertiesFromMaterial(originalMat);
            
            obst.material = replacementMat;
        }
    }

    private IEnumerable<Renderer> GetObstructingObjects()
    {
        RaycastHit[] hits = Physics.RaycastAll(
            _target.position,
            (transform.position - _target.position).normalized,
            Vector3.Distance(transform.position, _target.position)
        );

        foreach (var h in hits)
        {
            var renderer = h.collider.GetComponent<Renderer>();
            if (renderer != null)
                yield return renderer;
        }
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
