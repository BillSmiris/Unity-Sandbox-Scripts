using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Elevator : MonoBehaviour
{
    [SerializeField] private int _initialFloor;
    [SerializeField] private float _floorHeight; //height of each floor
    [SerializeField] private float _floorClimbDuration; //time to pass each floor
    [SerializeField] private GameObject _groundPointObject;

    private bool _moving;
    private float _currentDistance;
    private float _currentMovementDuration;
    private int _floorCalled;
    private Vector3 _groundPointPos;
    private Vector3 _calledInitialPos;
    private Vector3 _calledTargetPos;
    private float _movementStartTime;
    private float _movementProgress;

    // Start is called before the first frame update
    void Start()
    {
        _groundPointPos = _groundPointObject.transform.position;
        transform.position = new Vector3(transform.position.x, _groundPointPos.y + _floorHeight * _initialFloor, transform.position.z);
        _floorCalled = 0;
        _movementStartTime = 0f;
        _movementProgress = 0.0f;
        _moving = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (_moving)
        {
            _movementProgress = (Time.time - _movementStartTime) / _currentMovementDuration;
            transform.localPosition = Vector3.Lerp(_calledInitialPos, _calledTargetPos, _movementProgress);
            if (_movementProgress >= 1.0f)
            {
                _moving = false;
                _movementProgress = 0.0f;
                transform.position = _calledTargetPos;
                _initialFloor = _floorCalled;
            }
        }
    }

    public void Call(int floor)
    {
        if (!_moving)
        {
            _floorCalled = floor;
            _calledInitialPos = transform.position;
            _calledTargetPos = new Vector3(_calledInitialPos.x, _groundPointPos.y + _floorHeight * _floorCalled, _calledInitialPos.z);
            _movementStartTime = Time.time;
            _currentMovementDuration = _floorClimbDuration * Mathf.Abs(_initialFloor - floor);
            _moving = true;
        }
    }
}
