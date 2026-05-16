using UnityEngine;

public class WeaponMount : MonoBehaviour
{
    [SerializeField] private Transform _mountPoint;

    private readonly GunBase[] _guns = new GunBase[2];
    private readonly int[] _equippedItemIds = new int[2];

    public GunBase GetGun(int slotIndex) =>
        (uint)slotIndex < (uint)_guns.Length ? _guns[slotIndex] : null;

    public int GetEquippedItemId(int slotIndex) =>
        (uint)slotIndex < (uint)_equippedItemIds.Length ? _equippedItemIds[slotIndex] : 0;

    public GunBase ApplyEquip(int itemId, int slotIndex)
    {
        GunBase gun = SpawnGun(itemId, slotIndex);
        if (gun == null) return null;

        _equippedItemIds[slotIndex] = itemId;
        gun.gameObject.SetActive(true);
        return gun;
    }

    public void ApplyUnequip(int slotIndex)
    {
        if ((uint)slotIndex >= (uint)_guns.Length || _guns[slotIndex] == null) return;
        Destroy(_guns[slotIndex].gameObject);
        _guns[slotIndex] = null;
        _equippedItemIds[slotIndex] = 0;
    }

    private GunBase SpawnGun(int itemId, int slotIndex)
    {
        if ((uint)slotIndex >= (uint)_guns.Length) return null;

        if (_guns[slotIndex] != null)
        {
            Destroy(_guns[slotIndex].gameObject);
            _guns[slotIndex] = null;
        }

        var data = ItemTable.Instance.Get(itemId);
        if (data == null) return null;

        GunBase gun = ResourceManager.Instance.Spawn<GunBase>(data.prefab, _mountPoint);
        _guns[slotIndex] = gun;
        return gun;
    }
}
