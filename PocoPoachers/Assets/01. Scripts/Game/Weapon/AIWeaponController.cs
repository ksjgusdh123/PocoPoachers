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
        _gun?.StartReload();
    }

    public void EquipGun(int itemId)
    {
        if (_gun != null)
            Destroy(_gun.gameObject);

        _gun = GunTable.Instance.Equip(itemId, _mountPoint);
    }
}
