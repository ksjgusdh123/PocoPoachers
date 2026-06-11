using UnityEngine;

public class BagMount : MonoBehaviour
{
    [SerializeField] private Transform _bagMountPoint;
    public void SetMountPoint(Transform mountPoint) => _bagMountPoint = mountPoint;

    private Transform _bag;
    private int _equippedItemId;

    public int GetEquippedItemId() => _equippedItemId;

    public bool ApplyEquip(int itemId)
    {
        if (!SpawnBag(itemId)) return false;

        _equippedItemId = itemId;
        return true;
    }

    public void ApplyUnequip()
    {
        if (_bag == null) return;
        Destroy(_bag.gameObject);
        _bag = null;
        _equippedItemId = 0;
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
