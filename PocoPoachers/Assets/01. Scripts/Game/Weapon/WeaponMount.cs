using UnityEngine;

public class WeaponMount : MonoBehaviour
{
    [SerializeField] private Transform _mountPoint;

    private readonly GunBase[] _guns = new GunBase[2];
    private readonly int[] _equippedItemIds = new int[2];
    private readonly int[] _equippedUids = new int[2];
    private int _activeSlot = -1;

    public GunBase GetGun(int slotIndex) =>
        (uint)slotIndex < (uint)_guns.Length ? _guns[slotIndex] : null;

    public GunBase GetActiveGun()
    {
        if (_activeSlot >= 0)
        {
            var active = GetGun(_activeSlot);
            if (active != null) return active;
        }

        for (int i = 0; i < _guns.Length; i++)
        {
            var gun = GetGun(i);
            if (gun != null && gun.gameObject.activeSelf) return gun;
        }

        for (int i = 0; i < _guns.Length; i++)
        {
            var gun = GetGun(i);
            if (gun != null) return gun;
        }

        return null;
    }

    public void SetActiveSlot(int slotIndex) => _activeSlot = slotIndex;

    public int GetEquippedItemId(int slotIndex) =>
        (uint)slotIndex < (uint)_equippedItemIds.Length ? _equippedItemIds[slotIndex] : 0;

    // 씬 언로드 중(OnDestroy) gun 오브젝트가 먼저 파괴돼도 안전하도록 uid를 별도 보관
    public int GetEquippedUid(int slotIndex) =>
        (uint)slotIndex < (uint)_equippedUids.Length ? _equippedUids[slotIndex] : 0;

    public GunBase ApplyEquip(int itemId, int slotIndex, int uid = 0)
    {
        GunBase gun = SpawnGun(itemId, slotIndex);
        if (gun == null) return null;

        gun.SetUid(uid);
        _equippedItemIds[slotIndex] = itemId;
        _equippedUids[slotIndex] = uid;
        _activeSlot = slotIndex;
        for (int i = 0; i < _guns.Length; i++)
        {
            if (_guns[i] != null)
                _guns[i].gameObject.SetActive(i == slotIndex);
        }
        return gun;
    }

    public void ApplyUnequip(int slotIndex)
    {
        if ((uint)slotIndex >= (uint)_guns.Length || _guns[slotIndex] == null) return;
        Destroy(_guns[slotIndex].gameObject);
        _guns[slotIndex] = null;
        _equippedItemIds[slotIndex] = 0;
        _equippedUids[slotIndex] = 0;
        if (_activeSlot == slotIndex) _activeSlot = -1;
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
