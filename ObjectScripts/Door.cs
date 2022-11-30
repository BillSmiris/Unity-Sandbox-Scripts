using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : Interactable
{
    [SerializeField] private GameObject _hinge;
    [SerializeField] private bool _closed = true;
    [SerializeField] private bool _locked = false;
    [SerializeField] private float _movementRange = 90.0f;
    [SerializeField] private float _movementDuration  = 1.0f;
    [SerializeField] private bool _direction = true;

    private float _movementStartTime;
    private bool _changingStatus;
    private Quaternion _defaultRotation;
    private Quaternion _initialRotation;
    private Quaternion _targetRotation;
    private float _movementProgress;
    private float _currentMovementDuration;

    // Start is called before the first frame update
    void Start()
    {
        gameObject.transform.localPosition = new Vector3(gameObject.transform.localScale.x / 2, 0, 0);
        _movementStartTime = 0f;
        _movementProgress = 0.0f;
        _changingStatus = false;
        _defaultRotation = _hinge.transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        if (_changingStatus)
        {
            _movementProgress = (Time.time - _movementStartTime) / _currentMovementDuration;
            _hinge.transform.rotation = Quaternion.Slerp(_initialRotation, _targetRotation, _movementProgress);
            if (_movementProgress >= 1.0f)
            {
                _changingStatus = false;
                _movementProgress = 0.0f;
                _hinge.transform.rotation = _targetRotation;
            }
        }
    }

    public override void Interact(GameObject actor)
    {
        //if(!_changingStatus)
        //{
            _changingStatus = true;
            _initialRotation = _hinge.transform.rotation;
            if (_closed && !_locked)
            {
                if (_direction)
                {
                    _targetRotation = _defaultRotation * Quaternion.Euler(0, _movementRange, 0);
                }
                else
                {
                    _targetRotation = _defaultRotation * Quaternion.Euler(0, -_movementRange, 0);
                }
            }
            else
            {
                _targetRotation = _defaultRotation;
            }
            //_movementStartTime = Time.time + _movementDuration * (1 -_movementProgress);
            _movementStartTime = Time.time;
            if (_movementProgress > 0.0f) {
                _currentMovementDuration = _movementDuration * _movementProgress;
            }
            else {
                _currentMovementDuration = _movementDuration;
            }
            _closed = !_closed;
        //}
    }

    public override string Prompt()
    {
        if (_closed)
        {
            return "E) Open";
        }
        return "E) Close";
    }

    public void Lock()
    {
        if (_closed)
        {
            _locked = true;
        }
    }

    public void Unlock()
    {
        if (_closed)
        {
            _locked = false;
        }
    }
}
