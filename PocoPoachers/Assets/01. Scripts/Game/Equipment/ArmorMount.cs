using UnityEngine;

public class ArmorMount : MonoBehaviour
{
    [SerializeField] private Transform _helmetMountPoint;
    public void SetMountPoint(Transform mountPoint) => _helmetMountPoint = mountPoint;

    private ArmorBase _helmet;
    private int _equippedItemId;
    private int _equippedUid;

    public ArmorBase GetArmor() => _helmet;
    public int GetEquippedItemId() => _equippedItemId;

    // 씬 언로드 중(OnDestroy) armor 오브젝트가 먼저 파괴돼도 안전하도록 uid를 별도 보관
    public int GetEquippedUid() => _equippedUid;

    public ArmorBase ApplyEquip(int itemId, int uid = 0)
    {
        ArmorBase armor = SpawnArmor(itemId);
        if (armor == null) return null;

        armor.SetUid(uid);
        _equippedItemId = itemId;
        _equippedUid = uid;
        return armor;
    }

    public void ApplyUnequip()
    {
        if (_helmet == null) return;
        Destroy(_helmet.gameObject);
        _helmet = null;
        _equippedItemId = 0;
        _equippedUid = 0;
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
        if (armor != null) armor.SetItemId(itemId);
        _helmet = armor;
        return armor;
    }
}
