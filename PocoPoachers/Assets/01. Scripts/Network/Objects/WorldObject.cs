using System.Collections.Generic;
using UnityEngine;

public enum ObjectKind
{
    Player,
    Npc,
    WorldItem,
    ItemBox,
}

public class WorldObject : MonoBehaviour
{
    static readonly int HashVelX       = Animator.StringToHash("VelocityX");
    static readonly int HashVelZ       = Animator.StringToHash("VelocityZ");
    static readonly int HashSprinting  = Animator.StringToHash("IsSprinting");
    static readonly int HashRoll       = Animator.StringToHash("Roll");
    static readonly int HashAiming     = Animator.StringToHash("IsAiming");
    static readonly int HashReloading  = Animator.StringToHash("IsReloading");
    static readonly int HashDown       = Animator.StringToHash("IsDown");

    [SerializeField] float _smooth = 14f;
    [SerializeField] float _animSmooth = 0.1f;

    public int Id { get; private set; }
    public ObjectKind Kind { get; private set; }
    public int TypeId { get; private set; }

    // 원격 발소리 방출(RemoteFootstepEmitter)이 참조하는 이동 상태
    public bool IsSprintingState => _targetSprinting;
    public float PlanarMoveSqr => _targetVelX * _targetVelX + _targetVelZ * _targetVelZ;

    Vector3 _targetPos;
    float _targetYaw;
    bool _hasTarget;

    float _targetVelX;
    float _targetVelZ;
    bool _targetSprinting;
    bool _wasRolling;

    Animator _animator;
    HashSet<int> _animParams;

    public void Initialize(ObjectKind kind, int id, int typeId = 0)
    {
        Kind = kind;
        Id = id;
        TypeId = typeId;
        _animator = GetComponentInChildren<Animator>();
        CacheAnimatorParameters();
    }

    // Animator Controller에 실제 존재하는 파라미터만 세팅하기 위해 미리 해시를 모아둔다
    // (오브젝트마다 컨트롤러가 달라 없는 파라미터 세팅 시 "Parameter does not exist" 경고가 스팸됨)
    void CacheAnimatorParameters()
    {
        _animParams = null;
        if (_animator == null || _animator.runtimeAnimatorController == null) return;

        _animParams = new HashSet<int>();
        foreach (var p in _animator.parameters)
            _animParams.Add(p.nameHash);
    }

    bool HasParam(int hash) => _animParams != null && _animParams.Contains(hash);

    public void SetMoveTarget(Vector3 worldPos, float yawDegrees, float velX = 0f, float velZ = 0f, bool isSprinting = false, bool isRolling = false, bool isAiming = false, bool isReloading = false, bool isDown = false)
    {
        _targetPos       = worldPos;
        _targetYaw       = yawDegrees;
        _targetVelX      = velX;
        _targetVelZ      = velZ;
        _targetSprinting = isSprinting;
        _hasTarget       = true;

        if (_animator != null)
        {
            if (isRolling && !_wasRolling)
            {
                if (HasParam(HashRoll)) _animator.SetTrigger(HashRoll);
                _animator.SetLayerWeight(1, 0f);
            }
            else if (!isRolling && _wasRolling)
            {
                _animator.SetLayerWeight(1, 1f);
            }
            if (HasParam(HashAiming))    _animator.SetBool(HashAiming,    isAiming);
            if (HasParam(HashReloading)) _animator.SetBool(HashReloading, isReloading);
            if (HasParam(HashDown))      _animator.SetBool(HashDown,      isDown);
        }
        _wasRolling = isRolling;
    }

    void Update()
    {
        if (!_hasTarget) return;

        float t = 1f - Mathf.Exp(-_smooth * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, _targetPos, t);
        float y = Mathf.LerpAngle(transform.eulerAngles.y, _targetYaw, t);
        transform.rotation = Quaternion.Euler(0f, y, 0f);

        if (_animator == null) return;
        if (HasParam(HashVelX))      _animator.SetFloat(HashVelX, _targetVelX, _animSmooth, Time.deltaTime);
        if (HasParam(HashVelZ))      _animator.SetFloat(HashVelZ, _targetVelZ, _animSmooth, Time.deltaTime);
        if (HasParam(HashSprinting)) _animator.SetBool(HashSprinting, _targetSprinting);
    }
}
