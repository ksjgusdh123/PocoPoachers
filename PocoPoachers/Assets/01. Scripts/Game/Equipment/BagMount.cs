using UnityEngine;

public class BagMount : MonoBehaviour
{
    [SerializeField] private Transform _bagMountPoint;
    public void SetMountPoint(Transform mountPoint) => _bagMountPoint = mountPoint;

    private Transform _bag;
    private int _equippedItemId;
    private int _equippedUid;

    public int GetEquippedItemId() => _equippedItemId;
    public int GetEquippedUid() => _equippedUid;

    public bool ApplyEquip(int itemId, int uid = 0)
    {
        if (!SpawnBag(itemId)) return false;

        _equippedItemId = itemId;
        _equippedUid = uid;
        return true;
    }

    public void ApplyUnequip()
    {
        if (_bag == null) return;
        Destroy(_bag.gameObject);
        _bag = null;
        _equippedItemId = 0;
        _equippedUid = 0;
    }

    private bool SpawnBag(int itemId)
    {
        if (_bag != null)
        {
            Destroy(_bag.gameObject);
            _bag = null;
        }

        var data = ItemTable.Instance.Get(itemId);
        if (data == null) return false;

        _bag = ResourceManager.Instance.Spawn<Transform>(data.prefab, _bagMountPoint);
        return _bag != null;
    }
}
