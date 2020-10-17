using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class PathFollower : MonoBehaviour
{
    public CinemachinePath path;
    public float speed;     // In path distance units per second
    public float pauseTime; // In seconds

    private System.Action _currentState;
    private float _timer = 0;
    private float _pathPos = 0;

    private float MoveTime => path.PathLength / speed;

    void Awake()
    {
        _currentState = PausingAtBeginning;
    }

    void FixedUpdate()
    {
        _currentState();
        transform.position = path.EvaluatePositionAtUnit(_pathPos, CinemachinePathBase.PositionUnits.Distance);
    }

    private void PausingAtBeginning()
    {
        _timer += Time.deltaTime;
        _pathPos = 0;
        if (_timer > pauseTime)
        {
            _timer = 0;
            _currentState = MovingToEnd;
        }
    }

    private void MovingToEnd()
    { 
        _pathPos = Mathf.MoveTowards(_pathPos, path.PathLength, speed * Time.deltaTime);

        if (_pathPos == path.PathLength)
        {
            _timer = 0;
            _currentState = PausingAtEnd;
        }
    }

    private void PausingAtEnd()
    {
        _timer += Time.deltaTime;
        _pathPos = path.PathLength;
        if (_timer > pauseTime)
        {
            _timer = 0;
            _currentState = MovingToBeginning;
        }
    }

    private void MovingToBeginning()
    {
        _pathPos = Mathf.MoveTowards(_pathPos, 0, speed * Time.deltaTime);

        if (_pathPos == 0)
        {
            _timer = 0;
            _currentState = PausingAtBeginning;
        }
    }
}
