using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackAndForth : MonoBehaviour
{
    public Vector3 EndPosition;
    public float PauseTime;
    public float MoveTime;

    private Vector3 _startPos;
    private Vector3 _endPos;
    private float _timer = 0;
    private bool isMoving = false;

    void Awake()
    {
        _startPos = transform.position;
        _endPos = EndPosition;
    }

    void FixedUpdate()
    {
        _timer += Time.deltaTime;

        if (!isMoving)
        {
            if (_timer >= PauseTime)
            {
                _timer -= PauseTime;
                isMoving = true;
            }
        }
        else
        {
            transform.position = Vector3.Lerp(_startPos, _endPos, _timer / MoveTime);

            if (_timer >= MoveTime)
            {
                _timer -= MoveTime;
                isMoving = false;
                transform.position = _endPos;

                // Swap the start and end
                Vector3 temp = _startPos;
                _startPos = _endPos;
                _endPos = temp;
            }
        }
    }
}
