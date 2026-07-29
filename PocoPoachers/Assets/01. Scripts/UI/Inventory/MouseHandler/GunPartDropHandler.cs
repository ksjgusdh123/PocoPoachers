using UnityEngine;

// 총기 파츠를 드래그&드롭으로 장착하는 슬롯 핸들러. 슬롯 하나(=SlotType)당 1개.
// 인스펙터에서 _itemType = GunPart, _slotType 지정. 대상 총은 패널이 SetGun으로 주입.
public class GunPartDropHandler : ItemHolderDropHandler
{
    [SerializeField] private SlotType _slotType;

    private GunBase _gun;
    private int _droppedPartUid;

    public SlotType SlotType => _slotType;
    public void SetGun(GunBase gun) => _gun = gun;

    // 총 지정 + 현재 이 슬롯에 장착된 파츠를 아이콘으로 표시 (없으면 비움)
    public void Bind(GunBase gun)
    {
        SetGun(gun);

        GunPartData equipped = gun != null ? gun.GetPart(_slotType) : null;
        if (equipped != null)
        {
            SetDisplay(ItemTable.Instance.Get(equipped.id), 1);
            // 저장된 파츠 uid를 복원해 해제 시 강화/상태를 잃지 않도록 한다.
            // 호스트: WEM에서 실제 uid를 얻는다. 게스트: WEM에 파츠가 없어 0이 나오므로,
            // 이번 세션에 직접 장착해 이미 들고 있는 _droppedPartUid를 그대로 유지한다.
            int restoredUid = gun.Uid != 0 ? WorldEquipmentManager.GetPartUid(gun.Uid, _slotType) : 0;
            if (restoredUid != 0)
                _droppedPartUid = restoredUid;
            ShowEnhancementLabel(equipped.id, _droppedPartUid);
        }
        else
        {
            ClearDisplay();
            _droppedPartUid = 0;
        }
    }

    protected override bool OnItemDropped(ItemData data, int amount, int uid)
    {
        // 표시(SetDisplay) 부작용 전에 먼저 검증 — 슬롯/호환이 안 맞으면 거부
        GunPartData part = GunPartTable.Instance.Get(data.id);
        if (part == null || part.slot_type != _slotType)
            return false;
        if (_gun == null || !GunPartUtil.IsCompatible(part, _gun.Stat.GunType))
            return false;

        // ItemType(GunPart) 검사 + 아이콘 표시는 base가 처리
        if (!base.OnItemDropped(data, amount, uid))
            return false;

        _droppedPartUid = uid;
        GunPartData enhanced = WorldEquipmentManager.GetEnhancedGunPart(part, uid);
        _gun.EquipPart(enhanced);
        SyncPart(part.id, uid);
        ShowEnhancementLabel(part.id, uid);
        return true;
    }

    // 슬롯의 이름 텍스트 자리에 파츠 이름 대신 강화도(+N)를 표시한다
    private void ShowEnhancementLabel(int partId, int partUid)
    {
        if (_nameText == null) return;
        int level = WorldEquipmentManager.GetEnhancementLevel(partUid, partId);
        _nameText.text = $"+{level}";
    }

    // 해제 시 base가 인벤토리로 반납할 때 이 uid로 되돌려야 파츠의 강화/상태가 유지된다
    protected override int GetUnequipUid() => _droppedPartUid;

    public override void Unequip()
    {
        base.Unequip();              // 인벤토리로 반납
        if (_gun == null) return;

        _gun.UnequipPart(_slotType); // 총에서 제거 + 스탯 재계산
        SyncPart(0, 0);
        _droppedPartUid = 0;
    }

    // 파츠 변경(장착=partId, 해제=0) + 바뀐 장탄수를 호스트에 저장.
    // 호스트 본인이면 즉시 로컬 저장, 게스트면 호스트에게 패킷으로 요청한다
    private void SyncPart(int partId, int partUid)
    {
        // 게스트가 로컬에서 강화한 레벨을 호스트에 함께 알린다 (호스트가 partUid 기준으로 저장·복원)
        int partLevel = partId != 0 ? WorldEquipmentManager.GetEnhancementLevel(partUid, partId) : 0;
        RoomSync.GunPartEquip(_gun.Uid, _slotType, partId, partUid, partLevel, _gun.CurrentAmmo, _gun.Stat.MaxMagazine);
    }
}
