using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class CrappyPlayerVisuals : MonoBehaviour
{
    public Transform _model;
    private PlayerMovement _movement;

    void Awake()
    {
        _movement = GetComponent<PlayerMovement>();

        _movement.StartedJumping.AddListener(() => StartCoroutine(JumpAnimation()));
    }

    void Update()
    {
        // Rotate the model in the direction we're moving
        _model.localEulerAngles = new Vector3(0, -_movement.HAngle * Mathf.Rad2Deg +90, 0);
    }

    private IEnumerator JumpAnimation()
    {
        const float stretchHeight = 1.25f;
        const float stretchWidth = 0.9f;

        Vector3 stretchScale = new Vector3(
            stretchWidth,
            stretchHeight,
            stretchWidth
        );
        Vector3 stretchPos = new Vector3(
            0,
            1 - stretchHeight,
            0
        );

        Vector3 restScale = Vector3.one;
        Vector3 restPos = Vector3.zero;

        // Recede back to the rest position as our jump velocity slows down
        float startVSpeed = _movement.VSpeed;
        while (_movement.VSpeed >= 0)
        {
            float t = _movement.VSpeed / startVSpeed;

            _model.localScale = Vector3.Lerp(restScale, stretchScale, t);
            _model.localPosition = Vector3.Lerp(restPos, stretchPos, t);

            yield return new WaitForEndOfFrame();
        }
        _model.localScale = restScale;
    }
}
