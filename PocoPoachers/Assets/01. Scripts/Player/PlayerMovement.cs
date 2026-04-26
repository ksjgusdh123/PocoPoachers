using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _sprintSpeed = 8f;
    [SerializeField] private float _acceleration = 10f;

    [SerializeField] private float _sendInterval = 0.1f;
    [SerializeField] private float _minMoveSqrEpsilon = 0.0004f;
    [SerializeField] private float _minYawDelta = 0.5f;

    private CharacterController _characterController;
    private PlayerInputHandler _inputHandler;
    private Animator _animator;

    private float _currentSpeed;

    private float _nextSendTime;
    private Vector3 _lastSentPos;
    private float _lastSentYaw;
    private sbyte _lastMoveType = -1;
    private bool _hasSent;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _inputHandler = GetComponent<PlayerInputHandler>();
        _animator = GetComponentInChildren<Animator>();
        _currentSpeed = _moveSpeed;
    }

    private void Update()
    {
        Move();
        
    }

    private void LateUpdate()
    {
        SendMoveToServer();
    }

    private void Move()
    {
        Vector2 input = _inputHandler.MoveInput;
        Vector3 moveDir = new Vector3(input.x, 0f, input.y);

        float targetSpeed = moveDir == Vector3.zero ? 0f : (_inputHandler.IsSprintPressed ? _sprintSpeed :_moveSpeed);
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, _acceleration * Time.deltaTime);

        // 캐릭터 로컬 방향 기준으로 속도 분해 (조준 방향이 캐릭터 forward)
        Vector3 localMove = transform.InverseTransformDirection(moveDir * _currentSpeed);
        float normalizedX = localMove.x / _sprintSpeed;
        float normalizedZ = localMove.z / _sprintSpeed;

        _animator.SetFloat("VelocityX", normalizedX, 0.1f, Time.deltaTime);
        _animator.SetFloat("VelocityZ", normalizedZ, 0.1f, Time.deltaTime);

        _characterController.Move(moveDir * _currentSpeed * Time.deltaTime);
    }

    private void SendMoveToServer()
    {
        var nm = NetworkManager.Instance;
        if (nm == null || !nm.IsLoggedIn)
        {
            _hasSent = false;
            _lastMoveType = -1;
            return;
        }

        if (Time.unscaledTime < _nextSendTime)
            return;

        _nextSendTime = Time.unscaledTime + _sendInterval;

        var tr = transform;
        Vector3 pos = tr.position;
        float yaw = tr.eulerAngles.y;
        sbyte moveType = _inputHandler != null && _inputHandler.MoveInput.sqrMagnitude > 0.01f ? (sbyte)1 : (sbyte)0;

        if (_hasSent &&
            (pos - _lastSentPos).sqrMagnitude < _minMoveSqrEpsilon &&
            Mathf.Abs(Mathf.DeltaAngle(yaw, _lastSentYaw)) < _minYawDelta &&
            moveType == _lastMoveType)
        {
            return;
        }

        PacketSender.CMoveReq(pos, yaw, moveType);
        _lastSentPos = pos;
        _lastSentYaw = yaw;
        _lastMoveType = moveType;
        _hasSent = true;
    }
}
