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
    private PlayerDodge _playerDodge;
    private Animator _animator;
    private WeaponController _weaponController;

    private Vector3 _currentVelocity; // _currentSpeed 대신 벡터로 교체

    private float _nextSendTime;
    private Vector3 _lastSentPos;
    private float _lastSentYaw;
    private sbyte _lastMoveType = -1;
    private bool _hasSent;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _inputHandler = GetComponent<PlayerInputHandler>();
        _playerDodge = GetComponent<PlayerDodge>();
        _animator = GetComponentInChildren<Animator>();
        _weaponController = GetComponent<WeaponController>();
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
      if (_playerDodge.IsRolling) return;

      Vector2 input = _inputHandler.MoveInput;
      Vector3 moveDir = new Vector3(input.x, 0f, input.y).normalized;

      float weaponMultiplier = _weaponController != null ? _weaponController.MoveSpeedMultiplier : 1f;
      float targetSpeed = moveDir == Vector3.zero ? 0f
          : (_inputHandler.IsSprintPressed ? _sprintSpeed : _moveSpeed) * weaponMultiplier;
      Vector3 targetVelocity = moveDir * targetSpeed;

      // 방향 포함 전체 벡터를 부드럽게 보간 → 반대 방향 전환 시 튀지 않음
      _currentVelocity = Vector3.MoveTowards(_currentVelocity, targetVelocity, _acceleration *Time.deltaTime);

      Vector3 localVelocity = transform.InverseTransformDirection(_currentVelocity);
      _animator.SetFloat("VelocityX", localVelocity.x / _moveSpeed, 0.1f, Time.deltaTime);
      _animator.SetFloat("VelocityZ", localVelocity.z / _moveSpeed, 0.1f, Time.deltaTime);
      _animator.SetBool("IsSprinting", _inputHandler.IsSprintPressed && moveDir != Vector3.zero);

      _characterController.Move(_currentVelocity * Time.deltaTime);
    }

    private void SendMoveToServer()
    {
        var p2p = P2PManager.Instance;
        if (p2p == null || !p2p.IsConnected) return;

        if (Time.unscaledTime < _nextSendTime) return;
        _nextSendTime = Time.unscaledTime + _sendInterval;

        Vector3 pos = transform.position;
        float yaw = transform.eulerAngles.y;
        sbyte moveType = _inputHandler != null && _inputHandler.MoveInput.sqrMagnitude > 0.01f ? (sbyte)1 : (sbyte)0;

        if (_hasSent &&
            (pos - _lastSentPos).sqrMagnitude < _minMoveSqrEpsilon &&
            Mathf.Abs(Mathf.DeltaAngle(yaw, _lastSentYaw)) < _minYawDelta &&
            moveType == _lastMoveType)
            return;

        int myId = NetworkManager.Instance?.MyPlayerId ?? 0;
        var vec = new Vec3T { X = pos.x, Y = pos.y, Z = pos.z };

        if (p2p.IsHost)
            p2p.SendToAll(new H_MoveT { PlayerId = myId, Pos = vec, Rotation = yaw, MoveType = moveType },
                          H_Move.Pack, PacketType.H_Move);
        else
            p2p.SendToAll(new G_MoveT { PlayerId = myId, Pos = vec, Rotation = yaw, MoveType = moveType },
                          G_Move.Pack, PacketType.G_Move);

        _lastSentPos  = pos;
        _lastSentYaw  = yaw;
        _lastMoveType = moveType;
        _hasSent      = true;
    }
}
