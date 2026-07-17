using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(PlayerStat))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float _acceleration = 10f;
    [SerializeField] private float _itemUseSpeedMultiplier = 0.4f; // 아이템 사용 중 이동속도 배율

    [Header("중력")]
    [SerializeField] private float _gravity = -20f;
    [SerializeField] private float _groundedGravity = -2f; // 접지 유지용 하강 속도

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
    private float _verticalVelocity;
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

    public bool IsSprinting => _isSprinting;

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
      // isGrounded는 직전 Move() 결과 기준 — 접지 시 소량 하강을 유지해야 판정이 안정적
      if (_characterController.isGrounded)
          _verticalVelocity = _groundedGravity;
      else
          _verticalVelocity += _gravity * Time.deltaTime;

      if (_playerDodge.IsRolling)
      {
          // 구르기 중 수평 이동은 PlayerDodge가 담당, 여기서는 중력만 적용
          _characterController.Move(Vector3.up * (_verticalVelocity * Time.deltaTime));
          return;
      }

      Vector2 input = _inputHandler.MoveInput;
      Vector3 moveDir = new Vector3(input.x, 0f, input.y).normalized;

      bool isSprinting = _inputHandler.IsSprintPressed && moveDir != Vector3.zero && CanSprint();

      if (isSprinting)
          _playerStat.DrainSprintStamina();

      float weaponMultiplier = _weaponController != null ? _weaponController.MoveSpeedMultiplier : 1f;
      // 기준 속도(방어구 배율 포함)는 PlayerStat에서, 무기/아이템 사용 배율은 여기서 곱한다
      float baseSpeed = isSprinting ? _playerStat.SprintSpeed : _playerStat.MoveSpeed;
      float targetSpeed = moveDir == Vector3.zero ? 0f
          : baseSpeed * weaponMultiplier * _currentItemUseMultiplier;
      Vector3 targetVelocity = moveDir * targetSpeed;

      // 입력 없으면 즉시 정지, 입력 있으면 부드럽게 가속
      if (targetVelocity == Vector3.zero)
          _currentVelocity = Vector3.zero;
      else
          _currentVelocity = Vector3.MoveTowards(_currentVelocity, targetVelocity, _acceleration * Time.deltaTime);

      Vector3 localVelocity = transform.InverseTransformDirection(_currentVelocity);
      _localVelX = localVelocity.x / _playerStat.BaseMoveSpeed;
      _localVelZ = localVelocity.z / _playerStat.BaseMoveSpeed;
      _isSprinting = isSprinting;
      _animator.SetFloat("VelocityX", _localVelX, 0.1f, Time.deltaTime);
      _animator.SetFloat("VelocityZ", _localVelZ, 0.1f, Time.deltaTime);
      _animator.SetBool("IsSprinting", _isSprinting);

      Vector3 motion = _currentVelocity;
      motion.y = _verticalVelocity;
      _characterController.Move(motion * Time.deltaTime);
    }

    private bool CanSprint() => _playerStat != null && !_playerStat.IsDead && _playerStat.CurrentStamina > 0f;

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

        bool isAiming    = _weaponController != null && _weaponController.IsAiming;
        bool isReloading = _weaponController != null && _weaponController.IsReloading;
        RoomSync.Move(pos, yaw, moveType, _localVelX, _localVelZ, _isSprinting, _playerDodge.IsRolling, isAiming, isReloading);

        _lastSentPos  = pos;
        _lastSentYaw  = yaw;
        _lastMoveType = moveType;
        _hasSent      = true;
    }
}

