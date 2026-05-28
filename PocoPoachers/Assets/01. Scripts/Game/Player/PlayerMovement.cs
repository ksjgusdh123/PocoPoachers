using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _sprintSpeed = 8f;
    [SerializeField] private float _acceleration = 10f;
    [SerializeField] private float _sprintStaminaDrain = 20f;
    [SerializeField] private float _itemUseSpeedMultiplier = 0.4f; // 아이템 사용 중 이동속도 배율

    [SerializeField] private float _sendInterval = 0.1f;
    [SerializeField] private float _minMoveSqrEpsilon = 0.0004f;
    [SerializeField] private float _minYawDelta = 0.5f;

    private CharacterController _characterController;
    private PlayerInputHandler _inputHandler;
    private PlayerDodge _playerDodge;
    private Animator _animator;
    private WeaponController _weaponController;
    private PlayerStat _playerStat;

    private Vector3 _currentVelocity; // _currentSpeed 대신 벡터로 교체
    private float _currentItemUseMultiplier = 1f;

    private float _nextSendTime;
    private Vector3 _lastSentPos;
    private float _lastSentYaw;
    private sbyte _lastMoveType = -1;
    private bool _hasSent;

    private float _localVelX;
    private float _localVelZ;
    private bool _isSprinting;

    public static Transform LocalTransform { get; private set; }

    private void OnEnable() { LocalTransform = transform; }
    private void OnDisable() { if (LocalTransform == transform) LocalTransform = null; }

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _inputHandler = GetComponent<PlayerInputHandler>();
        _playerDodge = GetComponent<PlayerDodge>();
        _animator = GetComponentInChildren<Animator>();
        _weaponController = GetComponent<WeaponController>();
        _playerStat = GetComponent<PlayerStat>();

        PlayerController.OnUseStarted  += OnItemUseStarted;
        PlayerController.OnUseCancelled += ResetItemUseMultiplier;
        ItemUseSystem.OnItemUsed        += OnItemUsed;
    }

    private void OnDestroy()
    {
        PlayerController.OnUseStarted  -= OnItemUseStarted;
        PlayerController.OnUseCancelled -= ResetItemUseMultiplier;
        ItemUseSystem.OnItemUsed        -= OnItemUsed;
    }

    private void OnItemUseStarted(float _) => _currentItemUseMultiplier = _itemUseSpeedMultiplier;
    private void OnItemUsed(ItemData _)    => ResetItemUseMultiplier();
    private void ResetItemUseMultiplier()  => _currentItemUseMultiplier = 1f;

    private void Update()
    {
        Move();

    }

    private void LateUpdate()
    {
        SyncMove();
    }

    private void Move()
    {
      if (_playerDodge.IsRolling) return;

      Vector2 input = _inputHandler.MoveInput;
      Vector3 moveDir = new Vector3(input.x, 0f, input.y).normalized;

      bool isSprinting = _inputHandler.IsSprintPressed && moveDir != Vector3.zero && CanSprint();

      if (isSprinting)
          _playerStat.DrainStamina(_sprintStaminaDrain * Time.deltaTime);

      float weaponMultiplier = _weaponController != null ? _weaponController.MoveSpeedMultiplier : 1f;
      float armorMultiplier = _playerStat != null ? _playerStat.ArmorMoveSpeedMultiplier : 1f;
      float targetSpeed = moveDir == Vector3.zero ? 0f
          : (isSprinting ? _sprintSpeed : _moveSpeed) * weaponMultiplier * _currentItemUseMultiplier * armorMultiplier;
      Vector3 targetVelocity = moveDir * targetSpeed;

      // 입력 없으면 즉시 정지, 입력 있으면 부드럽게 가속
      if (targetVelocity == Vector3.zero)
          _currentVelocity = Vector3.zero;
      else
          _currentVelocity = Vector3.MoveTowards(_currentVelocity, targetVelocity, _acceleration * Time.deltaTime);

      Vector3 localVelocity = transform.InverseTransformDirection(_currentVelocity);
      _localVelX = localVelocity.x / _moveSpeed;
      _localVelZ = localVelocity.z / _moveSpeed;
      _isSprinting = isSprinting;
      _animator.SetFloat("VelocityX", _localVelX, 0.1f, Time.deltaTime);
      _animator.SetFloat("VelocityZ", _localVelZ, 0.1f, Time.deltaTime);
      _animator.SetBool("IsSprinting", _isSprinting);

      _characterController.Move(_currentVelocity * Time.deltaTime);
    }

    private bool CanSprint() => _playerStat == null || _playerStat.CurrentStamina > 0f;

    private void SyncMove()
    {
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

        RoomSync.Move(pos, yaw, moveType, _localVelX, _localVelZ, _isSprinting, _playerDodge.IsRolling);

        _lastSentPos  = pos;
        _lastSentYaw  = yaw;
        _lastMoveType = moveType;
        _hasSent      = true;
    }
}

