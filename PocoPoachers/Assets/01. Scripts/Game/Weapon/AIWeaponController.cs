using Unity.Behavior;
using UnityEngine;

public class AIWeaponController : MonoBehaviour
{
    [SerializeField] private Transform _mountPoint;
    // 타겟 위치에 더할 조준 높이 오프셋 (발밑 피벗 기준 눈/가슴 높이 보정) — LineTraceToWallCondition/ReachTargetAction의 EyeHeight와 동일한 관례값
    [SerializeField] private float _aimHeightOffset = 1.5f;

    private GunBase _gun;
    private TargetDetector _targetDetector;

    public GunBase Gun => _gun;
    public bool HasGun => _gun != null;

    private void Awake()
    {
        _targetDetector = GetComponent<TargetDetector>();
    }

    private void Start()
    {
    }

    public void TryShoot()
    {
        _gun?.TryShoot();
    }

    public void StartReload()
    {
        if (_gun != null)
            _gun.StartReload(_gun.Stat.MaxMagazine);
    }

    public void EquipGun(int itemId)
    {
        if (_gun != null)
        {
            _gun.OnReloadRequested -= OnReloadRequested;
            Destroy(_gun.gameObject);
        }

        var itemData = ItemTable.Instance.Get(itemId);
        if (itemData == null) return;
        _gun = ResourceManager.Instance.Spawn<GunBase>(itemData.prefab, _mountPoint);

        if (_gun != null)
        {
            _gun.Owner = gameObject;
            _gun.OnReloadRequested += OnReloadRequested;
            _gun.AimDirectionProvider = GetAimDirection;
            UpdateBlackboardGunStat(_gun.Stat);
        }
    }

    // 플레이어 쪽 크로스헤어 높이 보정(WeaponController.GetCrosshairAimDirection)과 같은 목적.
    // 이 콜백이 없으면 GunBase의 폴백이 발사 방향의 y를 0으로 눌러버려서
    // 타겟과 높이차(언덕/계단 등)가 있으면 총알이 수평으로만 나가 무조건 빗나갔다.
    // 적은 화면 크로스헤어가 없는 대신 TargetDetector가 실제 타겟 오브젝트를 알고 있으므로
    // 타겟 위치(+눈높이 오프셋)를 직접 조준점으로 사용한다.
    private Vector3 GetAimDirection()
    {
        Transform muzzle = _gun.Muzzle;
        GameObject target = _targetDetector != null ? _targetDetector.CurrentTarget : null;
        if (target == null)
        {
            Vector3 flat = muzzle.up;
            flat.y = 0f;
            return flat.sqrMagnitude < 0.001f ? muzzle.up : flat.normalized;
        }

        Vector3 targetPoint = target.transform.position + Vector3.up * _aimHeightOffset;
        Vector3 dir = targetPoint - muzzle.position;
        return dir.sqrMagnitude < 0.001f ? muzzle.up : dir.normalized;
    }

    private void OnDestroy()
    {
        if (_gun != null)
            _gun.OnReloadRequested -= OnReloadRequested;
    }

    private void OnReloadRequested()
    {
        _gun.StartReload(_gun.Stat.MaxMagazine);
    }

    private void UpdateBlackboardGunStat(GunStatData stat)
    {
        var agent = GetComponent<BehaviorGraphAgent>();
        if (agent == null) return;

        if (agent.BlackboardReference.GetVariable("AttackRange", out BlackboardVariable<float> attackRange))
            attackRange.Value = stat.BulletRange;

        if (agent.BlackboardReference.GetVariable("HalfAttackRange", out BlackboardVariable<float> halfAttackRange))
            halfAttackRange.Value = stat.BulletRange * 0.5f;

        if (agent.BlackboardReference.GetVariable("ReloadingTime", out BlackboardVariable<float> reloadingTime))
            reloadingTime.Value = stat.ReloadTime;

        var detector = GetComponent<TargetDetector>();
        detector?.SetDetectRange(stat.BulletRange);
    }
}
