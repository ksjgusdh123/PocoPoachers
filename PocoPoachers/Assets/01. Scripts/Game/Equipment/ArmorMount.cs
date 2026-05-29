using UnityEngine;

public class ArmorMount : MonoBehaviour
{
    [SerializeField] private Transform _helmetMountPoint;

    private ArmorBase _helmet;
    private int _equippedItemId;

    public ArmorBase GetArmor() => _helmet;
    public int GetEquippedItemId() => _equippedItemId;

    public ArmorBase ApplyEquip(int itemId)
    {
        ArmorBase armor = SpawnArmor(itemId);
        if (armor == null) return null;

        _equippedItemId = itemId;
        return armor;
    }

    public void ApplyUnequip()
    {
        if (_helmet == null) return;
        Destroy(_helmet.gameObject);
        _helmet = null;
        _equippedItemId = 0;
    }

    private ArmorBase SpawnArmor(int itemId)
    {
        if (_helmet != null)
        {
            Destroy(_helmet.gameObject);
            _helmet = null;
        }

        var data = ItemTable.Instance.Get(itemId);
        if (data == null) return null;

        ArmorBase armor = ResourceManager.Instance.Spawn<ArmorBase>(data.prefab, _helmetMountPoint);
        _helmet = armor;
        return armor;
    }
}
