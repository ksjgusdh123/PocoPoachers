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

        float targetSpeed = _inputHandler.IsSprintPressed ? _sprintSpeed : _moveSpeed;
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, _acceleration * Time.deltaTime);

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

        var session = nm.Session;
        if (session == null) return;
        session.Send(MakePacket.CMoveReq(pos, yaw, moveType));
        _lastSentPos = pos;
        _lastSentYaw = yaw;
        _lastMoveType = moveType;
        _hasSent = true;
    }
}
