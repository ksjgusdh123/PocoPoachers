using System;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private const string WallLayerName = "Wall";
    private const int MaxHitCount = 8;
    private const float HeadshotDamageMultiplier = 2f;
    private static readonly RaycastHit[] HitBuffer = new RaycastHit[MaxHitCount];

    [SerializeField] private float _collisionRadius = 0.08f;
    [SerializeField] private LayerMask _hitMask = ~0;
    [SerializeField] private LayerMask _wallMask;

    private float _speed;
    private float _damage;
    private float _range;
    private bool _isHeadshot;
    private float _traveledDistance;
    private Vector3 _direction;
    private Action _onRelease;
    private bool _applyDamage = true;
    private bool _isReleased;
    private bool _showHitMarker;
    private TrailRenderer _trail;
    private GameObject _attacker;
    private int _attackerLayer = -1;
    private Color _color;
    private Action<bool, Collider> _onDamageResult;

    // 유도 대상. 지정되면 매 프레임 이쪽으로 방향을 튼다. 대상이 사라지면 탄이 소멸한다.
    // Transform이 아니라 Collider로 들고 있는 이유는 bounds.center(몸통 중심)를 조준해야 하기 때문이다.
    // Transform.position은 캐릭터 원점 = 대개 발밑이라, 그쪽을 노리면 적이 아니라 바닥에 맞는다.
    private Collider _homingTarget;
    private float _homingTurnRate;

    // 드론 유도탄처럼 "명중해도 추가 발사를 부르면 안 되는" 탄환 표시.
    // 없으면 유도탄이 또 유도탄을 불러 무한 연쇄가 된다.
    private bool _suppressHitEvent;

    // 로컬 플레이어가 쏜 총알이 적을 맞춘 순간 (피격 대상, 명중 지점).
    // 데미지 적용(_applyDamage)과 무관하게 발생한다 — 게스트의 연출용 총알도 명중은 판정하기 때문.
    public static event Action<Collider, Vector3> OnLocalPlayerHitTarget;

    private void Awake()
    {
        _trail = GetComponent<TrailRenderer>();
        EnsureWallMask();
        _applyDamage = RoomManager.IsHost;
    }

    public void Initialize(float speed, float damage, float range, Vector3 direction, Action onRelease, GameObject attacker = null, Color color = default, bool isHeadshot = false, Action<bool, Collider> onDamageResult = null)
    {
        _speed = speed;
        _damage = damage;
        _range = range;
        _isHeadshot = isHeadshot;
        _direction = direction.normalized;
        _onRelease = onRelease;
        _attacker = attacker;
        _attackerLayer = attacker != null ? attacker.layer : -1;
        _traveledDistance = 0f;
        _isReleased = false;
        _color = color == default ? Color.white : color;
        // 로컬 플레이어가 쏜 총알에만 히트마커 표시 (AI/원격 플레이어 총알은 attacker가 null이거나 PlayerController가 없음)
        _showHitMarker = attacker != null && attacker.TryGetComponent<PlayerController>(out _);
        // 호스트가 게스트의 권위 총알을 대신 시뮬레이션할 때, 데미지 적용 결과(킬 여부)를 원 발사자에게 알리는 용도
        _onDamageResult = onDamageResult;

        // 풀에서 돌려쓰는 오브젝트라 이전 발사의 유도/억제 상태를 반드시 지운다
        _homingTarget = null;
        _homingTurnRate = 0f;
        _suppressHitEvent = false;
    }

    // 유도 설정. Initialize 다음에 호출한다. turnRate는 초당 회전 각도.
    public void SetHoming(Collider target, float turnRate)
    {
        _homingTarget = target;
        _homingTurnRate = turnRate;
    }

    // 이 탄환의 명중이 추가 발사를 트리거하지 않게 한다 (드론이 쏜 탄환)
    public void SuppressHitEvent() => _suppressHitEvent = true;

    private void Update()
    {
        if (_isReleased) return;

        if (!UpdateHoming()) return;
        
        float remaining = _range - _traveledDistance;
        float step = Mathf.Min(_speed * Time.deltaTime, remaining);
        Vector3 origin = transform.position;

        if (TryGetHit(origin, step, out RaycastHit hit))
        {
            bool showVFX = false;
            bool isKill = false;

            if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
            {
                if (_applyDamage)
                {
                    float damage = _isHeadshot ? _damage * HeadshotDamageMultiplier : _damage;
                    // 무적 등으로 데미지가 무효면 관통 — 충돌을 무시하고 정상 전진
                    if (!damageable.TakeDamage(damage, _attacker))
                    {
                        transform.position = origin + _direction * step;
                        _traveledDistance += step;
                        if (_traveledDistance >= _range)
                            Release();
                        return;
                    }
                    // 데미지가 실제로 적용된 이 클라이언트(호스트)에서만 사망 여부를 알 수 있음
                    isKill = damageable is StatBase stat && stat.IsDead;
                    _onDamageResult?.Invoke(isKill, hit.collider);
                }
                showVFX = true;
            }

            if (showVFX)
            {
                if (hit.collider.TryGetComponent<Sandbag>(out _))
                    SandVFXPool.Instance?.Spawn(hit);
                else
                    BloodVFXPool.Instance?.Spawn(hit);
            }

            transform.position = hit.point;   // 실제로 멈추는 경우에만 충돌 지점에 붙임

            // showVFX는 IDamageable(적/샌드백)을 맞췄을 때만 true — 벽 등 다른 콜라이더는 히트마커 제외
            if (_showHitMarker && showVFX)
            {
                CrosshairUI.Instance?.ShowHitMarker(isKill, _isHeadshot);

                // 히트마커와 같은 조건 = "내가 쏜 총알이 적을 맞췄다". 드론이 이 신호로 유도탄을 쏜다.
                if (!_suppressHitEvent)
                    OnLocalPlayerHitTarget?.Invoke(hit.collider, hit.point);
            }

            if (IsWallHit(hit.collider))
                BulletDecalPool.Instance?.Spawn(hit, _color);

            Release();
            return;
        }

        transform.position = origin + _direction * step;
        _traveledDistance += step;

        if (_traveledDistance >= _range)
            Release();
    }

    // 유도 대상 쪽으로 방향을 튼다. 계속 진행해도 되면 true.
    // 대상이 사라졌으면(처치/디스폰) 탄을 없앤다 — 허공으로 날아가 봐야 의미가 없다.
    private bool UpdateHoming()
    {
        if (_homingTurnRate <= 0f) return true;   // 유도탄이 아님

        if (_homingTarget == null || !_homingTarget.gameObject.activeInHierarchy)
        {
            Release();
            return false;
        }

        // 발밑(Transform.position)이 아니라 몸통 중심을 노린다
        Vector3 toTarget = _homingTarget.bounds.center - transform.position;
        if (toTarget.sqrMagnitude > 0.0001f)
        {
            _direction = Vector3.RotateTowards(
                _direction,
                toTarget.normalized,
                _homingTurnRate * Mathf.Deg2Rad * Time.deltaTime,
                0f).normalized;
            transform.rotation = Quaternion.LookRotation(_direction);
        }

        return true;
    }

    private bool TryGetHit(Vector3 origin, float distance, out RaycastHit closestHit)
    {
        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            _collisionRadius,
            _direction,
            HitBuffer,
            distance,
            _hitMask,
            QueryTriggerInteraction.Ignore);

        closestHit = default;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = HitBuffer[i];
            if (hit.collider == null) continue;
            // 발사자와 같은 레이어(아군)는 충돌 무시 — 관통해서 계속 진행
            if (_attackerLayer >= 0 && hit.collider.gameObject.layer == _attackerLayer) continue;
            if (hit.distance >= closestDistance) continue;

            closestHit = hit;
            closestDistance = hit.distance;
        }

        return closestDistance < float.MaxValue;
    }

    private bool IsWallHit(Collider hitCollider)
    {
        return hitCollider != null && ((_wallMask.value & (1 << hitCollider.gameObject.layer)) != 0);
    }

    private void EnsureWallMask()
    {
        if (_wallMask.value != 0) return;

        int wallLayer = LayerMask.NameToLayer(WallLayerName);
        if (wallLayer >= 0)
            _wallMask = 1 << wallLayer;
    }

    private void OnValidate()
    {
        EnsureWallMask();
    }

    private void Release()
    {
        if (_isReleased) return;
        _isReleased = true;
        _trail?.Clear();
        _onRelease?.Invoke();
    }
}
