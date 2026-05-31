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
        EquipGun(204);
    }

    public void TryShoot()
    {
        _gun?.TryShoot();
    }

    public void StartReload()
    {
        if (_gun != null)
            _gun.StartReload(_gun.GunData.magazineSize);
    }

    public void EquipGun(int itemId)
    {
        if (_gun != null)
            Destroy(_gun.gameObject);

        var itemData = ItemTable.Instance.Get(itemId);
        if (itemData == null) return;
        _gun = ResourceManager.Instance.Spawn<GunBase>(itemData.prefab, _mountPoint);

        if (_gun != null)
            UpdateBlackboardGunData(_gun.GunData);
    }

    private void UpdateBlackboardGunData(GunData gunData)
    {
        var agent = GetComponent<BehaviorGraphAgent>();
        if (agent == null) return;

        if (agent.BlackboardReference.GetVariable("AttackRange", out BlackboardVariable<float> attackRange))
            attackRange.Value = gunData.range;

        if (agent.BlackboardReference.GetVariable("ReloadingTime", out BlackboardVariable<float> reloadingTime))
            reloadingTime.Value = gunData.reloadTime;

        var detector = GetComponent<TargetDetector>();
        detector?.SetDetectRange(gunData.range);
    }
}
