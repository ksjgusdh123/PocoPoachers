using UnityEngine;

public class RemoteWeaponDisplay : MonoBehaviour
{
    [SerializeField] private Transform _mountPoint;

    private readonly GunBase[] _guns = new GunBase[2];

    public void Equip(int itemId, int slotIndex)
    {
        if ((uint)slotIndex >= (uint)_guns.Length) return;

        if (_guns[slotIndex] != null)
        {
            Destroy(_guns[slotIndex].gameObject);
            _guns[slotIndex] = null;
        }

        var data = ItemTable.Instance.Get(itemId);
        if (data == null) return;

        GunBase gun = ResourceManager.Instance.Spawn<GunBase>(data.prefab, _mountPoint);
        if (gun == null) return;

        gun.gameObject.SetActive(true);
        _guns[slotIndex] = gun;
    }

    public void Unequip(int slotIndex)
    {
        if ((uint)slotIndex >= (uint)_guns.Length) return;
        if (_guns[slotIndex] == null) return;

        Destroy(_guns[slotIndex].gameObject);
        _guns[slotIndex] = null;
    }
}
