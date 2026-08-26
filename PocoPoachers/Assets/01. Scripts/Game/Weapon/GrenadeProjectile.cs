using System.Collections;
using UnityEngine;

// 스킬로 던지는 수류탄. 포물선으로 착지 지점까지 날아간 뒤, 퓨즈(fuse) 후 폭발해
// 반경(radius) 안의 IDamageable에게 damage만큼 피해를 준다.
//
// 총알과 같은 권위 구조 — applyDamage는 호스트가 시뮬레이션한 사본에서만 true다.
// 게스트가 던진 수류탄은 로컬 연출용(applyDamage=false)이고, 호스트가 별도로 스폰한
// 권위 사본이 실제 폭발 피해를 넣는다(GrenadeSkill/PacketHandler.Combat 참고).
// 전용 프리팹/이펙트가 없어 시각은 기본 도형으로 대체한다.
public class GrenadeProjectile : MonoBehaviour
{
    private const float MinFlightTime = 0.25f;
    private const float ArcHeight = 2f;
    private const float FlashDuration = 0.15f;
    private const float DestroyDelay = 0.5f;

    private Vector3 _origin;
    private Vector3 _target;
    private float _flightTime;
    private float _elapsed;
    private float _fuse;
    private float _damage;
    private float _radius;
    private GameObject _attacker;
    private bool _applyDamage;
    private bool _landed;
    private bool _exploded;

    public static GrenadeProjectile Launch(Vector3 origin, Vector3 target, GameObject attacker, PlayerSkillData data, bool applyDamage)
    {
        var go = new GameObject("GrenadeProjectile");
        go.transform.position = origin;
        BuildVisual(go.transform);

        var grenade = go.AddComponent<GrenadeProjectile>();
        grenade.Init(origin, target, attacker, data, applyDamage);
        return grenade;
    }

    private static void BuildVisual(Transform parent)
    {
        var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.transform.SetParent(parent, false);
        body.transform.localScale = Vector3.one * 0.2f;
        Destroy(body.GetComponent<Collider>());
    }

    private void Init(Vector3 origin, Vector3 target, GameObject attacker, PlayerSkillData data, bool applyDamage)
    {
        _origin = origin;
        _target = target;
        _attacker = attacker;
        _damage = data.power;
        _radius = Mathf.Max(0.1f, data.radius);
        _fuse = Mathf.Max(0f, data.duration);
        _applyDamage = applyDamage;

        float distance = Vector3.Distance(origin, target);
        _flightTime = data.speed > 0f ? Mathf.Max(MinFlightTime, distance / data.speed) : MinFlightTime;
    }

    private void Update()
    {
        if (_landed) return;

        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _flightTime);

        Vector3 pos = Vector3.Lerp(_origin, _target, t);
        pos.y += ArcHeight * Mathf.Sin(t * Mathf.PI); // 포물선
        transform.position = pos;

        if (t >= 1f)
        {
            _landed = true;
            StartCoroutine(FuseThenExplode());
        }
    }

    private IEnumerator FuseThenExplode()
    {
        yield return new WaitForSeconds(_fuse);
        Explode();
    }

    private void Explode()
    {
        if (_exploded) return;
        _exploded = true;

        SpawnFlash();
        if (_applyDamage) ApplyExplosionDamage();

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
        var flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.transform.position = transform.position;
        flash.transform.localScale = Vector3.one * (_radius * 2f);
        Destroy(flash.GetComponent<Collider>());
        Destroy(flash, FlashDuration);
    }
}
