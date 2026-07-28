using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(PlayerStat))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float _acceleration = 10f;
    [SerializeField] private float _itemUseSpeedMultiplier = 0.4f; // 아이템 사용 중 이동속도 배율

    [Header("발소리 (AI 인식용)")]
    [SerializeField] private float _walkSoundRange = 4f;      // 걷기 시 발소리 인식 반경
    [SerializeField] private float _sprintSoundRange = 10f;   // 달리기 시 발소리 인식 반경
    [SerializeField] private float _walkStepInterval = 0.5f;  // 걷기 발소리 방출 간격(초)
    [SerializeField] private float _sprintStepInterval = 0.3f; // 달리기 발소리 방출 간격(초)

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
    private bool _lastSentDown;

    private float _localVelX;
    private float _localVelZ;
    private bool _isSprinting;
    private float _nextStepTime;

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

        if (_playerStat != null)
        {
            _playerStat.OnDie    += HandleDowned;
            _playerStat.OnRevive += HandleRevived;
        }
    }

    private void OnDestroy()
    {
        PlayerController.OnUseStarted  -= OnItemUseStarted;
        PlayerController.OnUseCancelled -= ResetItemUseMultiplier;
        ItemUseSystem.OnItemUsed        -= OnItemUsed;

        if (_playerStat != null)
        {
            _playerStat.OnDie    -= HandleDowned;
            _playerStat.OnRevive -= HandleRevived;
        }
    }

    private void HandleDowned()  => SetDown(true);
    private void HandleRevived() => SetDown(false);

    // 쓰러짐 상태 토글 — Animator의 IsDown 파라미터로 쓰러짐 애니메이션을 재생/해제한다.
    // 이동은 막지 않는다 (다운 시 PlayerStat.DownedMultiplier가 0.1배로 느리게 유지).
    public void SetDown(bool down)
    {
        if (_animator != null) _animator.SetBool("IsDown", down);
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

      // 쓰러진 상태(HP 0): 수평 이동을 막고 중력만 적용해 그 자리에 눕는다.
      // 쓰러짐 애니메이션은 SetDown이 켠 IsDown 파라미터로 재생된다.
      if (_playerStat != null && _playerStat.IsDead)
      {
          _currentVelocity = Vector3.zero;
          _characterController.Move(Vector3.up * (_verticalVelocity * Time.deltaTime));
          return;
      }

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

      EmitFootstep(moveDir != Vector3.zero, isSprinting);
    }

    // 이동 중일 때 걷기/달리기에 따라 다른 반경으로 발소리(AI 인식용 소음)를 방출한다
    // 실제 오디오 재생과는 무관 — SoundEvent는 AI 청각 판정 전용
    private void EmitFootstep(bool isMoving, bool isSprinting)
    {
        // 게스트는 방출하지 않는다 — AI는 호스트에서만 돌고, 게스트 발소리는 호스트가 원격으로 방출한다
        if (RoomManager.Instance != null && !RoomManager.IsHost)
            return;

        if (!isMoving || !_characterController.isGrounded)
            return;

        if (Time.time < _nextStepTime)
            return;

        _nextStepTime = Time.time + (isSprinting ? _sprintStepInterval : _walkStepInterval);
        float range = isSprinting ? _sprintSoundRange : _walkSoundRange;
        SoundEvent.Emit(transform.position, range, gameObject);
    }

    private bool CanSprint() => _playerStat != null && !_playerStat.IsDead && _playerStat.CurrentStamina > 0f;

    private void SyncMove()
    {
        if (Time.unscaledTime < _nextSendTime) return;
        _nextSendTime = Time.unscaledTime + _sendInterval;

        Vector3 pos = transform.position;
        float yaw = transform.eulerAngles.y;
        sbyte moveType = _inputHandler != null && _inputHandler.MoveInput.sqrMagnitude > 0.01f ? (sbyte)1 : (sbyte)0;
        bool isDown = _playerStat != null && _playerStat.IsDead;

        // 정지 중이라도 쓰러짐 상태가 바뀌면 전송해야 원격에 즉시 반영된다 (제자리 쓰러짐 대응)
        if (_hasSent &&
            (pos - _lastSentPos).sqrMagnitude < _minMoveSqrEpsilon &&
            Mathf.Abs(Mathf.DeltaAngle(yaw, _lastSentYaw)) < _minYawDelta &&
            moveType == _lastMoveType &&
            isDown == _lastSentDown)
            return;

        bool isAiming    = _weaponController != null && _weaponController.IsAiming;
        bool isReloading = _weaponController != null && _weaponController.IsReloading;
        RoomSync.Move(pos, yaw, moveType, _localVelX, _localVelZ, _isSprinting, _playerDodge.IsRolling, isAiming, isReloading, isDown);

        _lastSentPos  = pos;
        _lastSentYaw  = yaw;
        _lastMoveType = moveType;
        _lastSentDown = isDown;
        _hasSent      = true;
    }
}

