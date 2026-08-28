using System;
using System.Collections.Generic;
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
    private bool _isHeadshot;

    // 쏜 사람의 크리 배율. 데미지를 넣는 클라(호스트)가 발사자 스탯을 보고 채운다.
    private float _critMultiplier = StatBase.DefaultCritMultiplier;
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

    // 탄환 식별 — 쏜 클라가 발급한 (shooterId, seq).
    // 게스트는 적과의 충돌을 스스로 판정하지 않으므로, 호스트가 이 키로 "네 탄환이 맞았다"를 알려준다.
    private int _shooterId;
    private int _seq;
    private bool _hasNetworkId;

    private static readonly Dictionary<(int shooterId, int seq), Bullet> Registry = new();

    // 쏜 클라가 자기 탄환에 붙이는 순번. shooterId와 짝이라 클라끼리 겹쳐도 무방하다.
    private static int _nextSeq;
    public static int NextSeq() => ++_nextSeq;

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
        _critMultiplier = StatBase.DefaultCritMultiplier;   // 풀 재사용 — 이전 발사의 배율이 남지 않게
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

        Unregister();   // 풀 재사용 — 이전 발사의 식별자가 남아 있으면 엉뚱한 탄환이 지목된다

        // 풀에서 돌려쓰는 오브젝트라 이전 발사의 유도/억제 상태를 반드시 지운다
        _homingTarget = null;
        _homingTurnRate = 0f;
        _suppressHitEvent = false;
    }

    // 네트워크 식별자 부여. Initialize 다음에 호출한다.
    public void SetNetworkId(int shooterId, int seq)
    {
        if (shooterId == 0 || seq == 0) return;

        Unregister();
        _shooterId = shooterId;
        _seq = seq;
        _hasNetworkId = true;
        Registry[(shooterId, seq)] = this;
    }

    private void Unregister()
    {
        if (!_hasNetworkId) return;

        if (Registry.TryGetValue((_shooterId, _seq), out var b) && b == this)
            Registry.Remove((_shooterId, _seq));

        _hasNetworkId = false;
    }

    // 쏜 사람의 크리 배율 주입. Initialize 다음에 호출한다.
    public void SetCritMultiplier(float value) => _critMultiplier = value;

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
                // 게스트는 적과의 충돌을 판정하지 않는다 — 명중 여부는 호스트만 정하고
                // 결과를 H_BulletHit으로 알려준다. 여기서 멈추면 호스트가 관통시킨 탄환이
                // 게스트 화면에서만 사라지고, 헛피가 튄다.
                if (!_applyDamage)
                {
                    transform.position = origin + _direction * step;
                    _traveledDistance += step;
                    if (_traveledDistance >= _range)
                        Release();
                    return;
                }

                if (_applyDamage)
                {
                    float damage = _isHeadshot ? _damage * _critMultiplier : _damage;

                    // 행운의 사격 / 팀원 공격력 버프 — 데미지를 넣는 이 클라(호스트)가 발사자 스탯을 보고
                    // 직접 적용한다. 값 자체(확률/배율)만 StatSync로 신뢰하는 구조라(크리 배율과 동일한
                    // 신뢰 모델) 행운의 사격 확률은 여기서 매번 새로 굴려야 한다.
                    if (_attacker != null && _attacker.TryGetComponent<StatBase>(out var attackerStat))
                    {
                        if (attackerStat.LuckyShotChance > 0f && UnityEngine.Random.value < attackerStat.LuckyShotChance)
                            damage *= attackerStat.LuckyShotMultiplier;

                        damage *= attackerStat.AttackPowerMultiplier;
                    }

                    // 무적 등으로 데미지가 무효면 관통(대상이 반사 중이면 역벡터로 반사) — 충돌을 무시하고 계속 진행
                    if (!damageable.TakeDamage(damage, _attacker))
                    {
                        StatBase blockedStat = ResolveStat(damageable);
                        if (blockedStat != null && blockedStat.IsBulletReflecting)
                        {
                            ReflectOff(blockedStat, hit.point);
                        }
                        else
                        {
                            transform.position = origin + _direction * step;
                        }

                        _traveledDistance += step;
                        if (_traveledDistance >= _range)
                            Release();
                        return;
                    }
                    // 데미지가 실제로 적용된 이 클라이언트(호스트)에서만 사망 여부를 알 수 있음
                    isKill = ResolveStat(damageable) is { IsDead: true };
                    _onDamageResult?.Invoke(isKill, hit.collider);

                    // 게스트는 이 통보로만 혈흔을 뿌리고 탄환을 지운다
                    if (_hasNetworkId)
                        RoomSync.BulletHit(_shooterId, _seq, hit.point, hit.normal, isKill, _isHeadshot);
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

    // 맞은 대상의 실제 StatBase를 찾는다 — 방어막 콜라이더(ShieldHitboxLink)는 소유자를 대신 들고 있을 뿐이라
    // 직접 StatBase가 아니므로, 무적/반사/사망 판정을 하려면 이 경로로 한 번 더 풀어야 한다.
    private static StatBase ResolveStat(IDamageable damageable)
    {
        return damageable as StatBase ?? (damageable as ShieldHitboxLink)?.Owner;
    }

    // 반사 스킬로 막힌 총알을 대상 쪽에서 튕겨낸다 — 방향을 반전하고, 반사시킨 대상을 새 발사자로
    // 취급한다. TryGetHit이 발사자와 같은 레이어를 건너뛰므로, 이 재할당만으로 반사시킨 플레이어(와
    // 아군)는 다시 맞지 않고 적은 맞출 수 있게 된다 — 남은 사거리(_range - _traveledDistance)만큼
    // 그대로 날아간다.
    private void ReflectOff(StatBase reflector, Vector3 hitPoint)
    {
        _direction = -_direction;
        transform.rotation = Quaternion.LookRotation(_direction);

        // hitPoint에 그대로 두면 다음 프레임 스윕(SphereCast)의 시작점이 여전히 쉴드 콜라이더 표면과
        // 겹쳐서, 반전된 방향인데도 같은 콜라이더를 거리 0에 가깝게 즉시 재충돌로 잡는다(반사가 안 보이는 원인).
        // 콜리전 반경만큼 반전된 방향으로 밀어내 표면에서 확실히 벗어난 지점에서 다음 스윕을 시작하게 한다.
        transform.position = hitPoint + _direction * (_collisionRadius + 0.01f);

        _attacker = reflector.gameObject;
        _attackerLayer = reflector.gameObject.layer;
        _isHeadshot = false; // 원래 피격 판정 기준이라 새 대상엔 의미가 없다
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

    // 호스트가 알려준 명중을 반영 — 그 자리에서 혈흔을 뿌리고 탄환을 지운다
    public static void ApplyNetworkHit(int shooterId, int seq, Vector3 point, Vector3 normal, bool isKill, bool isHeadshot)
    {
        BloodVFXPool.Instance?.Spawn(point, normal);

        if (!Registry.TryGetValue((shooterId, seq), out var bullet) || bullet == null) return;

        if (bullet._showHitMarker)
            CrosshairUI.Instance?.ShowHitMarker(isKill, isHeadshot);

        bullet.transform.position = point;
        bullet.Release();
    }

    private void Release()
    {
        if (_isReleased) return;
        _isReleased = true;
        Unregister();
        _trail?.Clear();
        _onRelease?.Invoke();
    }
}
