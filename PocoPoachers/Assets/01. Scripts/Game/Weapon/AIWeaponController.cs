using Unity.Behavior;
using UnityEngine;

public class AIWeaponController : MonoBehaviour
{
    [SerializeField] private Transform _mountPoint;

    private GunBase _gun;

    public GunBase Gun => _gun;
    public bool HasGun => _gun != null;

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
            UpdateBlackboardGunStat(_gun.Stat);
        }
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
