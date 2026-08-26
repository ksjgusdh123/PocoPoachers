using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 스킬로 던지는 수류탄. 세 가지 역할로 나뉜다(EnemyNetSync와 같은 "호스트 시뮬레이션 + 게스트 보간" 구조):
//
// - Authoritative: 호스트에서만 존재. 실제 Rigidbody 물리로 날아가/구르고 다른 콜라이더와 진짜로 상호작용한다.
//   반경(radius) 안의 IDamageable에게 damage를 주는 것도 이 사본뿐이다. 위치를 주기적으로,
//   폭발 시점을 1회 게스트에 방송한다.
// - Remote: 호스트가 아닌 클라에서, 남이(또는 호스트가) 던진 수류탄을 보여주는 사본. 물리 없이
//   Authoritative가 보내주는 위치를 향해 보간만 한다(EnemyNetSync.UpdateGuest와 동일한 방식).
// - Cosmetic: 게스트가 자기 스킬을 쓴 그 순간, 왕복 지연 없이 즉시 보여주는 자기 전용 예측 사본.
//   물리를 쓰지 않는 결정론적 포물선+구르기로 흉내만 낸다 — 피해도 없고 서버와 위치가 정확히 맞지도 않는다
//   (총알을 쏜 클라의 로컬 총알이 연출 전용인 것과 같은 타협).
//
// 본체·폭발 시각은 Resources/Skill/Grenade, Resources/Skill/GrenadeExplosion 프리팹을 우선 쓰고,
// 없으면 기본 도형(구)으로 대체한다(BuildVisual/SpawnFlash 참고).
public class GrenadeProjectile : MonoBehaviour
{
    private enum Role { Cosmetic, Authoritative, Remote }
    private enum State { Flying, Rolling, Landed, Exploded }

    private static readonly Dictionary<int, GrenadeProjectile> _registry = new();
    private static int _nextId = 1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetRegistry() { _nextId = 1; _registry.Clear(); }

    private const float BodyRadius = 0.1f;
    private const float FlashDuration = 0.15f;
    private const float DestroyDelay = 0.3f;
    private const float MinFlightTime = 0.25f;

    // Resources/Skill/ 아래에 두면 자동으로 쓰인다. 없으면 기본 도형(구)으로 대체한다.
    private const string BodyPrefabPath = "Skill/Grenade";
    private const string ExplosionPrefabPath = "Skill/GrenadeExplosion";
    private const float PrefabFlashLifetime = 3f; // 폭발 프리팹이 스스로 안 지워질 때의 안전망

    // Cosmetic 전용 예측 파라미터 (물리 없는 결정론적 포물선+구르기)
    private const float ArcHeight = 2f;
    private const float RollSpeedFactor = 0.5f;
    private const float RollDeceleration = 8f;
    private const float RollStopSpeed = 0.3f;
    private const string WallLayerName = "Wall";

    // Authoritative/Remote 공통 — 호스트 위치 방송 주기, 게스트 보간 속도(EnemyNetSync와 동일)
    private const float SyncInterval = 0.05f;
    private const float SmoothRate = 14f;

    private Role _role;
    private int _id;
    private float _damage;
    private float _radius;
    private float _fuse;
    private GameObject _attacker;
    private State _state;
    private float _speed;

    // Cosmetic 전용
    private Vector3 _origin;
    private Vector3 _target;
    private float _flightTime;
    private float _elapsed;
    private Vector3 _rollDirection;
    private float _rollSpeed;
    private LayerMask _wallMask;

    // Authoritative 전용
    private Rigidbody _rb;
    private float _syncTimer;

    // Remote 전용
    private Vector3 _netTargetPos;

    // ── 스폰 ──────────────────────────────────────────────────

    // 게스트 자신이 스킬을 쓴 순간의 즉시 예측 사본 — 네트워크 없음, 피해 없음.
    public static void LaunchCosmetic(Vector3 origin, Vector3 target, GameObject attacker, PlayerSkillData data)
    {
        var grenade = CreateInstance(origin);
        grenade._role = Role.Cosmetic;
        grenade.InitCommon(attacker, data);
        grenade.InitCosmeticArc(origin, target);
        grenade.StartCoroutine(grenade.FuseThenExplode());
    }

    // 호스트 전용 — 실제 물리로 시뮬레이션하는 권위 사본. 새 grenade_id를 발급해 반환한다.
    public static int LaunchAuthoritative(Vector3 origin, Vector3 target, GameObject attacker, PlayerSkillData data)
    {
        var grenade = CreateInstance(origin);
        grenade._role = Role.Authoritative;
        grenade._id = _nextId++;
        _registry[grenade._id] = grenade;

        grenade.InitCommon(attacker, data);
        grenade.InitPhysics(origin, target, attacker, data);
        grenade.StartCoroutine(grenade.FuseThenExplode());

        return grenade._id;
    }

    // 호스트가 아닌 클라 — 남의(또는 호스트의) 권위 수류탄을 보여주는 보간 사본.
    public static void SpawnRemote(int grenadeId, Vector3 origin, PlayerSkillData data)
    {
        var grenade = CreateInstance(origin);
        grenade._role = Role.Remote;
        grenade._id = grenadeId;
        _registry[grenadeId] = grenade;

        grenade.InitCommon(null, data);
        grenade._netTargetPos = origin;
    }

    public static void OnNetMove(int grenadeId, Vector3 pos)
    {
        if (_registry.TryGetValue(grenadeId, out var g) && g != null)
            g._netTargetPos = pos;
    }

    public static void OnNetExplode(int grenadeId, Vector3 pos)
    {
        if (!_registry.TryGetValue(grenadeId, out var g) || g == null) return;
        g.transform.position = pos;
        g.Explode(applyDamage: false); // 호스트가 이미 적용한 피해 — 연출만 재생
    }

    private static GrenadeProjectile CreateInstance(Vector3 origin)
    {
        var go = new GameObject("GrenadeProjectile");
        go.transform.position = origin;
        BuildVisual(go.transform);
        return go.AddComponent<GrenadeProjectile>();
    }

    private static void BuildVisual(Transform parent)
    {
        GameObject prefab = Resources.Load<GameObject>(BodyPrefabPath);
        GameObject body = prefab != null
            ? Object.Instantiate(prefab, parent, false)
            : GameObject.CreatePrimitive(PrimitiveType.Sphere);

        if (prefab == null)
        {
            body.transform.SetParent(parent, false);
            body.transform.localScale = Vector3.one * (BodyRadius * 2f);
        }

        // 물리 판정은 코드로 붙이는 SphereCollider(BodyRadius) 하나로만 한다 —
        // 프리팹에 콜라이더가 딸려 있으면 중복 판정이 되므로 시각 전용으로 걷어낸다.
        foreach (var c in body.GetComponentsInChildren<Collider>())
            Destroy(c);
    }

    private void InitCommon(GameObject attacker, PlayerSkillData data)
    {
        _attacker = attacker;
        _damage = data.power;
        _radius = Mathf.Max(0.1f, data.radius);
        _fuse = Mathf.Max(0f, data.duration);
        _speed = data.speed;
    }

    private void OnDestroy()
    {
        if (_role != Role.Cosmetic && _registry.TryGetValue(_id, out var g) && g == this)
            _registry.Remove(_id);
    }

    // ── Authoritative: 실제 물리 ──────────────────────────────

    private void InitPhysics(Vector3 origin, Vector3 target, GameObject attacker, PlayerSkillData data)
    {
        var collider = gameObject.AddComponent<SphereCollider>();
        collider.radius = BodyRadius;
        collider.material = new PhysicsMaterial("Grenade")
        {
            bounciness = 0.25f,
            dynamicFriction = 0.6f,
            staticFriction = 0.6f,
            frictionCombine = PhysicsMaterialCombine.Average,
            bounceCombine = PhysicsMaterialCombine.Average,
        };

        // CharacterController 하나만이 아니라 자식에 달린 다른 콜라이더(피격 판정 등)도 있을 수 있어 전부 무시한다.
        if (attacker != null)
        {
            foreach (var attackerCollider in attacker.GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(collider, attackerCollider);
        }

        _rb = gameObject.AddComponent<Rigidbody>();
        _rb.mass = 0.4f;
        _rb.linearDamping = 0f;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.linearVelocity = ComputeLaunchVelocity(origin, target, data);
    }

    // 원하는 목표 지점·비행 시간을 만족하는 초기 속도를 역산 — 이후는 실제 중력/충돌로 날아간다.
    private static Vector3 ComputeLaunchVelocity(Vector3 origin, Vector3 target, PlayerSkillData data)
    {
        Vector3 horizontal = target - origin;
        horizontal.y = 0f;
        float horizDist = horizontal.magnitude;
        Vector3 horizDir = horizDist > 0.0001f ? horizontal.normalized : Vector3.forward;

        float speed = data.speed > 0f ? data.speed : horizDist / MinFlightTime;
        float flightTime = Mathf.Max(MinFlightTime, horizDist / speed);

        float g = Mathf.Abs(Physics.gravity.y);
        float heightDiff = target.y - origin.y;
        float vHorizontal = horizDist / flightTime;
        float vVertical = heightDiff / flightTime + 0.5f * g * flightTime;

        return horizDir * vHorizontal + Vector3.up * vVertical;
    }

    private void UpdateAuthoritative()
    {
        if (!RoomManager.HasGuests) return;

        _syncTimer -= Time.deltaTime;
        if (_syncTimer > 0f) return;
        _syncTimer = SyncInterval;

        RoomSync.GrenadeMove(_id, transform.position);
    }

    // ── Remote: 보간만 ────────────────────────────────────────

    private void UpdateRemote()
    {
        float t = 1f - Mathf.Exp(-SmoothRate * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, _netTargetPos, t);
    }

    // ── Cosmetic: 결정론적 포물선 + 구르기 예측 ───────────────

    private void InitCosmeticArc(Vector3 origin, Vector3 target)
    {
        _origin = origin;
        _target = target;

        float distance = Vector3.Distance(origin, target);
        float speed = _speed > 0f ? _speed : distance / MinFlightTime;
        _flightTime = Mathf.Max(MinFlightTime, distance / speed);

        _rollDirection = target - origin;
        _rollDirection.y = 0f;
        _rollDirection = _rollDirection.sqrMagnitude > 0.0001f ? _rollDirection.normalized : Vector3.forward;
        _rollSpeed = speed * RollSpeedFactor;

        int wallLayer = LayerMask.NameToLayer(WallLayerName);
        _wallMask = wallLayer >= 0 ? 1 << wallLayer : 0;
    }

    private void UpdateCosmetic()
    {
        switch (_state)
        {
            case State.Flying:
                UpdateFlying();
                break;
            case State.Rolling:
                UpdateRolling();
                break;
        }
    }

    private void UpdateFlying()
    {
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _flightTime);

        Vector3 pos = Vector3.Lerp(_origin, _target, t);
        pos.y += ArcHeight * Mathf.Sin(t * Mathf.PI);
        transform.position = pos;

        if (t >= 1f)
            _state = _rollSpeed > RollStopSpeed ? State.Rolling : State.Landed;
    }

    private void UpdateRolling()
    {
        float step = _rollSpeed * Time.deltaTime;

        if (_wallMask != 0 && Physics.SphereCast(transform.position, BodyRadius, _rollDirection, out RaycastHit hit, step, _wallMask))
        {
            transform.position += _rollDirection * Mathf.Max(0f, hit.distance);
            _state = State.Landed;
            return;
        }

        transform.position += _rollDirection * step;

        Vector3 spinAxis = Vector3.Cross(Vector3.up, _rollDirection);
        float spinDegrees = (step / (2f * Mathf.PI * BodyRadius)) * 360f;
        transform.Rotate(spinAxis, spinDegrees, Space.World);

        _rollSpeed = Mathf.Max(0f, _rollSpeed - RollDeceleration * Time.deltaTime);
        if (_rollSpeed <= RollStopSpeed)
            _state = State.Landed;
    }

    // ── 공통 ──────────────────────────────────────────────────

    private void Update()
    {
        if (_state == State.Exploded) return;

        switch (_role)
        {
            case Role.Authoritative:
                UpdateAuthoritative();
                break;
            case Role.Remote:
                UpdateRemote();
                break;
            case Role.Cosmetic:
                UpdateCosmetic();
                break;
        }
    }

    // 퓨즈는 던진 순간부터 흐른다 — 아직 날아가는 중이거나 구르는 중이어도 시간이 되면 그 자리에서 터진다.
    private IEnumerator FuseThenExplode()
    {
        yield return new WaitForSeconds(_fuse);
        Explode(applyDamage: _role == Role.Authoritative);
    }

    private void Explode(bool applyDamage)
    {
        if (_state == State.Exploded) return;
        _state = State.Exploded;

        SpawnFlash();
        if (applyDamage) ApplyExplosionDamage();

        if (_role == Role.Authoritative && RoomManager.HasGuests)
            RoomSync.GrenadeExplode(_id, transform.position);

        Destroy(gameObject, DestroyDelay);
    }

    private void ApplyExplosionDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _radius);
        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<IDamageable>(out var damageable)) continue;
            if (damageable is PlayerStat || damageable is RemotePlayerStat) continue; // 아군 오사 방지

            damageable.TakeDamage(_damage, _attacker);
        }
    }

    private void SpawnFlash()
    {
        GameObject prefab = Resources.Load<GameObject>(ExplosionPrefabPath);
        if (prefab != null)
        {
            var vfx = Object.Instantiate(prefab, transform.position, Quaternion.identity);
            Destroy(vfx, PrefabFlashLifetime); // 프리팹이 스스로 정리되지 않을 경우의 안전망
            return;
        }

        // 폭발 프리팹이 없을 때의 기본 도형 폴백 — 폭발 반경(radius)만큼 잠깐 표시
        var flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.transform.position = transform.position;
        flash.transform.localScale = Vector3.one * (_radius * 2f);
        Destroy(flash.GetComponent<Collider>());
        Destroy(flash, FlashDuration);
    }
}
