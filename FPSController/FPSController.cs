using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum PlayerMoveStatus { NotMoving, Walking, Running, NotGrounded, Landing, Crouching, Prone }

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    //temporary
    //public List<AudioSource> AudioSources = new List<AudioSource>();
    //private int _audioToUse = 0;
    //temporary end

    //Serialized Fields
    //Stance speeds
    [SerializeField] private float _walkSpeed = 2.0f; //Player speed while walking.
    [SerializeField] private float _runSpeed = 4.5f; //Player speed while running/sprinting with left shift.
    [SerializeField] private float _jumpSpeed = 7.5f; //Player speed while jumping.
    [SerializeField] private float _crouchSpeed = 1.0f; //Player speed while crouching.
    [SerializeField] private float _proneSpeed = 0.5f; //Player speed while prone.

    //Player gravity
    [SerializeField] private float _stickToGroundForce = 5.0f;
    [SerializeField] private float _gravityMultiplier = 2.5f;

    [SerializeField] private float _runStepLengthen = 0.75f;

    //Stance change times
    [SerializeField] private float _crouchTime = 0.1f; //Time it takes to switch to a crouching position.
    [SerializeField] private float _proneTime = 0.1f; //Time it takes to go prone from a crouching position.
    [SerializeField] private float _standToProneTime = 0.3f; //Time it takes to go prone from a standing position.

    [SerializeField] private FirstPerson.MouseLook _mouseLook;
    //End of serialized fields

    //Private fields
    private Camera _camera = null; //Player camera.
    private Text _interactionPrompt;
    private bool _jumpButtonPressed = false; //Boolean that shows wether the jump button is pressed.
    private Vector2 _inputVector = Vector2.zero;
    private Vector3 _moveDirection = Vector3.zero;
    private bool _previouslyGrounded = false;
    private bool _isWalking = true;
    private bool _isJumping = false;
    private bool _isCrouching = false;
    private bool _isProne = false;
    private Vector3 _localSpaceCameraPos = Vector3.zero;

    //Stance heights
    private float _standingHeight = 0.0f; //Player standing height.
    private float _crouchHeight = 0.0f; //Player crouching height.
    private float _proneHeight = 0.0f; //Player prone height.

    //Stance camera positions relative to the player body
    private Vector3 _standingCameraPos;
    private Vector3 _crouchCameraPos;
    private Vector3 _proneCameraPos;
    private float _stanceChangeStartTime;
    private float _standToProneStanceChangeSpeed;
    private float _changeStanceYOffset;
    private float _crouchYOffset = 0.4499999f;
    private float _proneYOffset = 0.049995f;
    private float _stanceChangeStartPlayerHeight;
    private Vector3 _stanceChangeStartCameraPos;
    private float _stanceChangeTargetPlayerHeight;
    private Vector3 _stanceChangeTargetCameraPos;
    private bool _changingStance = false;
    private float _stanceChangeDuration;

    private float _fallingTimer = 0.0f;

    private CharacterController _characterController;
    private PlayerMoveStatus _movementStatus = PlayerMoveStatus.NotMoving;

    public PlayerMoveStatus movementStatus { get { return _movementStatus; } }
    public float walkSpeed { get { return _walkSpeed; } }
    public float runSpeed { get { return _runSpeed; } }

    protected void Start()
    {
        _camera = Camera.main;
        _interactionPrompt = _camera.transform.GetChild(0).GetChild(0).GetComponent<Text>();

        _characterController = GetComponent<CharacterController>();
        _standingHeight = _characterController.height;
        _crouchHeight = _standingHeight / 2;
        _proneHeight = 0.1f;

        _localSpaceCameraPos = _camera.transform.localPosition;
        _standingCameraPos = _camera.transform.localPosition;
        _crouchCameraPos = new Vector3(0, _crouchHeight / 2 - 0.3f, 0);
        _proneCameraPos = new Vector3(0, _proneHeight / 2 - 0.3f, 0);

        _movementStatus = PlayerMoveStatus.NotMoving;

        _fallingTimer = 0.0f;

        _mouseLook.Init(transform, _camera.transform);
    }

    protected void Update()
    {
        //Count the time that the player is falling
        if (_characterController.isGrounded)
        {
            _fallingTimer = 0.0f;
        }
        else
        {
            _fallingTimer += Time.deltaTime;
        }

        //Handles mouse look.
        if (Time.timeScale > Mathf.Epsilon)
        {
            _mouseLook.LookRotation(transform, _camera.transform);
        }

        if (!_jumpButtonPressed)
        {
            if (_isProne && Input.GetButtonDown("Jump"))
            {
                ToggleProne();
            }
            else if (_isCrouching && Input.GetButtonDown("Jump"))
            {
                ToggleCrouch();
            }
            else
            {
                _jumpButtonPressed = Input.GetButtonDown("Jump");
            }
        }

        StanceChangeListener();

        InteractionManager();

        if (!_previouslyGrounded && _characterController.isGrounded)
        {
            if (_fallingTimer > 0.5f)
            {

            }

            _moveDirection.y = 0f;
            _isJumping = false;
            _movementStatus = PlayerMoveStatus.Landing;
        }
        else if (!_characterController.isGrounded)
        {
            _movementStatus = PlayerMoveStatus.NotGrounded;
        }
        else if (_characterController.velocity.sqrMagnitude < 0.01f)
        {
            _movementStatus = PlayerMoveStatus.NotMoving;
        }
        else if (_isProne)
        {
            _movementStatus = PlayerMoveStatus.Prone;
        }
        else if (_isCrouching)
        {
            _movementStatus = PlayerMoveStatus.Crouching;
        }
        else if (_isWalking)
        {
            _movementStatus = PlayerMoveStatus.Walking;
        }
        else
        {
            _movementStatus = PlayerMoveStatus.Running;
        }

        _previouslyGrounded = _characterController.isGrounded;
    }

    protected void FixedUpdate()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool waswalking = _isWalking;
        _isWalking = !Input.GetKey(KeyCode.LeftShift);

        float speed = SetSpeed();

        _inputVector = new Vector2(horizontal, vertical);

        if (_inputVector.sqrMagnitude > 1)
        {
            _inputVector.Normalize();
        }

        Vector3 desiredMove = transform.forward * _inputVector.y + transform.right * _inputVector.x;

        RaycastHit hitInfo;
        if (Physics.SphereCast(transform.position, _characterController.radius, Vector3.down, out hitInfo, _characterController.height / 2f, 1))
        {
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;
        }

        _moveDirection.x = desiredMove.x * speed;
        _moveDirection.z = desiredMove.z * speed;

        if (_characterController.isGrounded)
        {
            if (!_changingStance)
            {
                _moveDirection.y = -_stickToGroundForce;
            }

            if (_jumpButtonPressed)
            {
                _moveDirection.y = _jumpSpeed;
                _jumpButtonPressed = false;
                _isJumping = true;
            }
        }
        else
        {
            _moveDirection += Physics.gravity * _gravityMultiplier * Time.fixedDeltaTime;
        }

        _characterController.Move(_moveDirection * Time.fixedDeltaTime);

        //Vector3 speedXZ = new Vector3(_characterController.velocity.x, 0.0f, _characterController.velocity.y);
    }

    private void StanceChangeListener()
    {
        if (!_changingStance)
        {
            if (Input.GetButtonDown("Crouch"))
            {
                ToggleCrouch();
            }
            else if (Input.GetButtonDown("Prone"))
            {
                ToggleProne();
            }
        }

        if (_changingStance)
        {
            var f = (Time.time - _stanceChangeStartTime) / _stanceChangeDuration;
            _characterController.height = Mathf.SmoothStep(_stanceChangeStartPlayerHeight, _stanceChangeTargetPlayerHeight, f);
            _camera.transform.localPosition = Vector3.Slerp(_stanceChangeStartCameraPos, _stanceChangeTargetCameraPos, f);
            if (f >= 1.0f)
            {
                _changingStance = false;
                var curpos = _characterController.transform.position;
                _characterController.transform.position = new Vector3(curpos.x, curpos.y + _changeStanceYOffset, curpos.z);
            }
        }
    }

    //Function that checks if the player has enough space above them to change to a taller stance.
    private bool HasSpaceToStandUp(float targetHeight) //The function receives as a parameter the height of the stance that the player wants to switch to.
    {
        Vector3 position = transform.position; //Saves the current position of the player.
        Ray ray = new Ray(new Vector3(position.x, position.y + _characterController.height / 2, position.z), transform.up);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit)) //Casts a ray upwards from the top of the player and checks if the ray hit an object.
        {
            if (hit.distance > targetHeight - _characterController.height - 0.01f) //If the distance of the object is greater from the target stance height minus some space for the player to not get stuck,
            {
                return true; //the player has enough space to switch to a taller stance.
            }
            else
            {
                return false;
            }
        }
        return true; //If the ray didnt hit an object, the player has enough space.
    }

    private void ToggleCrouch()
    {
        if (_isProne && HasSpaceToStandUp(_crouchHeight))
        {
            _stanceChangeStartPlayerHeight = _proneHeight;
            _stanceChangeTargetPlayerHeight = _crouchHeight;
            _stanceChangeStartCameraPos = _proneCameraPos;
            _stanceChangeTargetCameraPos = _crouchCameraPos;
            _changeStanceYOffset = _proneYOffset;
            _stanceChangeDuration = _proneTime;
            _isProne = false;
            _changingStance = true;
            _stanceChangeStartTime = Time.time;
        }
        else if (!_isProne)
        {
            if (_isCrouching && HasSpaceToStandUp(_standingHeight))
            {
                _stanceChangeStartPlayerHeight = _crouchHeight;
                _stanceChangeTargetPlayerHeight = _standingHeight;
                _stanceChangeStartCameraPos = _crouchCameraPos;
                _stanceChangeTargetCameraPos = _standingCameraPos;
                _changeStanceYOffset = 0.0f;
                _stanceChangeDuration = _crouchTime;
                _isCrouching = false;
                _changingStance = true;
                _stanceChangeStartTime = Time.time;
            }
            else if (!_isCrouching)
            {
                _stanceChangeStartPlayerHeight = _standingHeight;
                _stanceChangeTargetPlayerHeight = _crouchHeight;
                _stanceChangeStartCameraPos = _standingCameraPos;
                _stanceChangeTargetCameraPos = _crouchCameraPos;
                _changeStanceYOffset = 0.0f;
                _stanceChangeDuration = _crouchTime;
                _isCrouching = true;
                _changingStance = true;
                _stanceChangeStartTime = Time.time;
            }
        }
    }

    private void ToggleProne()
    {
        if (!_isCrouching)
        {
            _stanceChangeStartPlayerHeight = _standingHeight;
            _stanceChangeTargetPlayerHeight = _proneHeight;
            _stanceChangeStartCameraPos = _standingCameraPos;
            _stanceChangeTargetCameraPos = _proneCameraPos;
            _changeStanceYOffset = 0.0f;
            _stanceChangeDuration = _standToProneStanceChangeSpeed;
            _isCrouching = true;
            _isProne = true;
            _changingStance = true;
            _stanceChangeStartTime = Time.time;
        }
        else
        {
            if (_isProne && HasSpaceToStandUp(_crouchHeight))
            {
                _stanceChangeStartPlayerHeight = _proneHeight;
                _stanceChangeTargetPlayerHeight = _crouchHeight;
                _stanceChangeStartCameraPos = _proneCameraPos;
                _stanceChangeTargetCameraPos = _crouchCameraPos;
                _changeStanceYOffset = _proneYOffset;
                _stanceChangeDuration = _proneTime;
                _isProne = false;
                _changingStance = true;
                _stanceChangeStartTime = Time.time;
            }
            else if (!_isProne)
            {
                _stanceChangeStartPlayerHeight = _crouchHeight;
                _stanceChangeTargetPlayerHeight = _proneHeight;
                _stanceChangeStartCameraPos = _crouchCameraPos;
                _stanceChangeTargetCameraPos = _proneCameraPos;
                _changeStanceYOffset = 0.0f;
                _isProne = true;
                _stanceChangeDuration = _proneTime;
                _changingStance = true;
                _stanceChangeStartTime = Time.time;
            }
        }
    }

    //Function that sets the player speed according to their current stance.
    public float SetSpeed()
    {
        if (_isProne)
        {
            return _proneSpeed;
        }
        else if (_isCrouching)
        {
            return _crouchSpeed;
        }
        else if (_isWalking)
        {
            return _walkSpeed;
        }
        return _runSpeed;
    }

    private void InteractionManager()
    {
        RaycastHit hit;
        Ray ray = _camera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        if (Physics.Linecast(ray.origin, ray.origin + ray.direction * 1.8f, out hit))
        {
            var hitInteraction = hit.transform.gameObject.GetComponent<Interactable>();
            if (hitInteraction)
            {
                _interactionPrompt.text = hitInteraction.Prompt();
                if (Input.GetButtonDown("Interact"))
                {
                    hitInteraction.Interact(gameObject);
                }
            }
        } 
        else
        {
            _interactionPrompt.text = "";
        }
    }
}
