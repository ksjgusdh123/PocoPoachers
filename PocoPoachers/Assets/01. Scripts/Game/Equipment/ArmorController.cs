using UnityEngine;

public class ArmorController : EquipableController
{
    protected ArmorMount _mount;
    protected StatBase _stat;

    private int _equippedSlotIndex = -1;

    protected virtual void Awake()
    {
        _mount = GetComponent<ArmorMount>();
        _stat = GetComponent<StatBase>();
    }

    public override void Equip(ItemData data, int slotIndex, int uid)
    {
        ArmorBase current = _mount.GetArmor();
        if (current != null)
        {
            _stat.RemoveArmorStat(current.Stat);
            _stat.OnDamaged -= OnDamaged;
        }

        ArmorBase armor = _mount.ApplyEquip(data.id, uid);
        if (armor == null) return;

        // 호스트(싱글플레이 포함) 본인이 장착하는 경우, 기존 내구도를 바로 조회해서 복원
        if (RoomManager.IsHost && uid != 0)
        {
            var (restoredCurrent, _) = WorldEquipmentManager.GetOrCreate(uid, data.id, armor.MaxDurability);
            armor.SetDurability(restoredCurrent);
        }

        _stat.ApplyArmorStat(armor.Stat);
        _stat.OnDamaged += OnDamaged;
        _equippedSlotIndex = slotIndex;
        OnEquipped(slotIndex, data, uid);
    }

    public override void Unequip(int slotIndex)
    {
        ArmorBase current = _mount.GetArmor();
        if (current == null) return;

        _stat.RemoveArmorStat(current.Stat);
        _stat.OnDamaged -= OnDamaged;
        _mount.ApplyUnequip();
        _equippedSlotIndex = -1;
        RaiseUnequipped(slotIndex);
        OnUnequipped(slotIndex);
    }

    private void OnDamaged(float damage, Vector3 _, GameObject __)
    {
        var armor = _mount.GetArmor();
        if (armor == null) return;

        // 로컬 즉시 반영(낙관적) + 호스트에 통보해서 권위 있는 값으로 동기화
        float delta = -(damage / 10f);
        armor.DecreaseDurability(damage);
        RoomSync.Durability(armor.Uid, armor.ItemId, delta, armor.MaxDurability);
    }

    public override void UnequipAll()
    {
        if (_mount.GetArmor() != null)
            Unequip(_equippedSlotIndex);
    }

    public override int GetEquippedId(int slotIndex) => _mount.GetEquippedItemId();
    public override int GetEquippedUid(int slotIndex) => _mount.GetArmor()?.Uid ?? 0;

    protected virtual void OnEquipped(int slotIndex, ItemData data, int uid) { }
    protected virtual void OnUnequipped(int slotIndex) { }
}
