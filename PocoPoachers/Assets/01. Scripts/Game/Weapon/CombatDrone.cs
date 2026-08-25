using System.Collections.Generic;
using UnityEngine;

// 추가탄 스킬이 소환하는 드론. 플레이어 주변을 돌면서, 그 플레이어의 총알이 적을 맞출 때마다
// 대상을 향해 유도탄을 한 발 더 쏜다.
//
// 발사 판단은 전부 호스트가 한다. 게스트는 자기 드론이라도 추측 발사하지 않고
// H_DroneShoot을 받아서 그린다 — 그래야 연출과 실제 피해가 항상 일치하고,
// 발사 간격(_fireInterval)도 호스트 한 곳에서만 돌아 어긋나지 않는다.
//
// 호스트가 명중을 아는 경로는 둘이다.
// - 자기 총알: Bullet.OnLocalPlayerHitTarget
// - 게스트 총알: 호스트가 대신 시뮬레이션하는 권위 총알의 onDamageResult (PacketHandler.Combat)
public class CombatDrone : MonoBehaviour
{
    private const string RemotePrefabPath = "Skill/CombatDrone";

    // 소유자 playerId → 드론. 내 드론과 남의 드론을 같은 방식으로 찾기 위해 둘 다 등록한다.
    private static readonly Dictionary<int, CombatDrone> ByOwner = new();

    [Header("배치")]
    [SerializeField] private float _orbitRadius = 1.2f;
    [SerializeField] private float _orbitHeight = 1.6f;
    [SerializeField] private float _orbitSpeed = 90f;      // 초당 각도

    [Header("사격")]
    [SerializeField, Tooltip("유도탄 사이 최소 간격(초). 호스트에서만 적용된다.")]
    private float _fireInterval = 0.3f;

    [SerializeField, Tooltip("유도탄이 방향을 트는 속도(초당 각도). 낮으면 크게 돌아 들어간다.")]
    private float _homingTurnRate = 540f;

    [SerializeField] private float _bulletSpeed = 30f;
    [SerializeField] private float _bulletRange = 40f;

    private Transform _owner;
    private int _ownerPlayerId;
    private float _damage;
    private float _angle;
    private float _nextFireTime;
    private GameObject _bulletPrefab;
    private bool _listenLocalHits;

    // listenLocalHits는 호스트에서만 true다 — 발사 판단을 호스트로 모으기 위해서다.
    // bulletPrefab이 null이면 발사 시점에 소유자의 장착 무기에서 가져온다.
    public void Setup(Transform owner, int ownerPlayerId, float damage, GameObject bulletPrefab, bool listenLocalHits)
    {
        _owner = owner;
        _damage = damage;
        _bulletPrefab = bulletPrefab;

        if (_ownerPlayerId != ownerPlayerId)
        {
            Unregister();
            _ownerPlayerId = ownerPlayerId;
            ByOwner[ownerPlayerId] = this;
        }

        if (_listenLocalHits == listenLocalHits) return;

        _listenLocalHits = listenLocalHits;
        if (!isActiveAndEnabled) return;

        if (listenLocalHits) Bullet.OnLocalPlayerHitTarget += HandleLocalHit;
        else Bullet.OnLocalPlayerHitTarget -= HandleLocalHit;
    }

    private void OnEnable()
    {
        if (_listenLocalHits) Bullet.OnLocalPlayerHitTarget += HandleLocalHit;
    }

    private void OnDisable() => Bullet.OnLocalPlayerHitTarget -= HandleLocalHit;

    private void OnDestroy() => Unregister();

    private void Unregister()
    {
        if (ByOwner.TryGetValue(_ownerPlayerId, out var d) && d == this)
            ByOwner.Remove(_ownerPlayerId);
    }

    private void Update()
    {
        if (_owner == null) return;

        _angle += _orbitSpeed * Time.deltaTime;
        float rad = _angle * Mathf.Deg2Rad;
        transform.position = _owner.position
            + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * _orbitRadius
            + Vector3.up * _orbitHeight;
    }

    private void HandleLocalHit(Collider target, Vector3 hitPoint) => TryFireAt(target);

    // 호스트 전용 판단 경로. 쿨다운을 통과하면 쏘고, 나머지 클라에게 그리라고 알린다.
    public bool TryFireAt(Collider target)
    {
        if (target == null) return false;
        if (Time.time < _nextFireTime) return false;
        if (!Fire(target)) return false;

        _nextFireTime = Time.time + _fireInterval;

        // 유도 대상을 지목할 수 없으면(네트워크에 없는 더미 타깃 등) 전파는 생략하고 로컬로만 쏜다
        var netSync = target.GetComponentInParent<EnemyNetSync>();
        if (netSync != null)
            RoomSync.DroneShoot(_ownerPlayerId, netSync.EnemyId);

        return true;
    }

    // 호스트가 알려준 발사를 그대로 그린다. 쿨다운도 재전파도 없다 — 권위가 이미 결정한 발사다.
    public void FireVisual(Collider target) => Fire(target);

    private bool Fire(Collider target)
    {
        if (target == null || _owner == null) return false;

        GameObject prefab = ResolveBulletPrefab();
        if (prefab == null) return false;

        var pool = BulletPool.GetInstance();
        if (pool == null) return false;

        Vector3 origin = transform.position;

        // 드론은 플레이어보다 위에 떠 있다. 적의 Transform.position(발밑)을 노리면
        // 아래로 꽂혀서 적이 아니라 바닥에 맞는다 — 반드시 몸통 중심을 겨눠야 한다.
        Vector3 dir = target.bounds.center - origin;
        if (dir.sqrMagnitude < 0.0001f) return false;
        dir.Normalize();

        Bullet bullet = pool.Get(prefab, origin, Quaternion.LookRotation(dir));
        bullet.Initialize(
            _bulletSpeed,
            _damage,
            _bulletRange,
            dir,
            () => pool.Release(prefab, bullet),
            _owner.gameObject);

        bullet.SetHoming(target, _homingTurnRate);

        // 유도탄이 또 유도탄을 부르면 무한 연쇄가 된다
        bullet.SuppressHitEvent();
        return true;
    }

    // 남의 드론은 프리팹을 받지 않고 만들어지므로, 쏠 때 소유자의 장착 무기에서 가져온다.
    // 무기를 바꾸면 다음 발사부터 바뀐 총알을 쓴다.
    private GameObject ResolveBulletPrefab()
    {
        if (_bulletPrefab != null) return _bulletPrefab;

        var mount = _owner.GetComponent<WeaponMount>();
        return mount != null ? mount.GetActiveGun()?.BulletPrefab : null;
    }

    // ── 원격 플레이어의 드론 관리 (H_DroneState 수신 시) ──────────

    // 해당 플레이어 옆의 드론을 켜고 끈다. 소유자 오브젝트의 자식으로 붙여 수명을 함께 간다.
    public static void SetActiveFor(int playerId, bool active, float damage)
    {
        var om = ObjectManager.Instance;
        if (om == null || !om.TryGet(ObjectKind.Player, playerId, out var playerObj)) return;

        var existing = FindFor(playerId);

        if (!active)
        {
            if (existing != null) Destroy(existing.gameObject);
            return;
        }

        if (existing != null)
        {
            existing.Setup(playerObj.transform, playerId, damage, null, listenLocalHits: false);
            return;
        }

        var prefab = Resources.Load<GameObject>(RemotePrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[CombatDrone] 드론 프리팹을 찾을 수 없습니다: Resources/{RemotePrefabPath}");
            return;
        }

        var drone = Instantiate(prefab, playerObj.transform);
        drone.GetComponent<CombatDrone>()?.Setup(playerObj.transform, playerId, damage, null, listenLocalHits: false);
    }

    public static CombatDrone FindFor(int playerId)
    {
        if (!ByOwner.TryGetValue(playerId, out var drone)) return null;
        if (drone != null) return drone;

        ByOwner.Remove(playerId);   // 씬 전환 등으로 파괴된 항목 정리
        return null;
    }
}
