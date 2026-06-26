using System;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private const string WallLayerName = "Wall";
    private const int MaxHitCount = 8;
    private static readonly RaycastHit[] HitBuffer = new RaycastHit[MaxHitCount];

    [SerializeField] private float _collisionRadius = 0.08f;
    [SerializeField] private LayerMask _hitMask = ~0;
    [SerializeField] private LayerMask _wallMask;

    private float _speed;
    private float _damage;
    private float _range;
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

    private void Awake()
    {
        _trail = GetComponent<TrailRenderer>();
        EnsureWallMask();
        _applyDamage = RoomManager.IsHost;
    }

    public void Initialize(float speed, float damage, float range, Vector3 direction, Action onRelease, GameObject attacker = null, Color color = default)
    {
        _speed = speed;
        _damage = damage;
        _range = range;
        _direction = direction.normalized;
        _onRelease = onRelease;
        _attacker = attacker;
        _attackerLayer = attacker != null ? attacker.layer : -1;
        _traveledDistance = 0f;
        _isReleased = false;
        _color = color == default ? Color.white : color;
        // 로컬 플레이어가 쏜 총알에만 히트마커 표시 (AI/원격 플레이어 총알은 attacker가 null이거나 PlayerController가 없음)
        _showHitMarker = attacker != null && attacker.TryGetComponent<PlayerController>(out _);
    }

    private void Update()
    {
        if (_isReleased) return;
        
        float remaining = _range - _traveledDistance;
        float step = Mathf.Min(_speed * Time.deltaTime, remaining);
        Vector3 origin = transform.position;

        if (TryGetHit(origin, step, out RaycastHit hit))
        {
            if (_applyDamage && hit.collider.TryGetComponent<IDamageable>(out var damageable))
            {
                // 무적 등으로 데미지가 무효면 관통 — 충돌을 무시하고 정상 전진
                if (!damageable.TakeDamage(_damage, _attacker))
                {
                    transform.position = origin + _direction * step;
                    _traveledDistance += step;
                    if (_traveledDistance >= _range)
                        Release();
                    return;
                }

                if (hit.collider.TryGetComponent<Sandbag>(out _))
                    SandVFXPool.Instance?.Spawn(hit);
                else
                    BloodVFXPool.Instance?.Spawn(hit);
            }

            transform.position = hit.point;   // 실제로 멈추는 경우에만 충돌 지점에 붙임

            if (_showHitMarker)
                CrosshairUI.Instance?.ShowHitMarker();

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
