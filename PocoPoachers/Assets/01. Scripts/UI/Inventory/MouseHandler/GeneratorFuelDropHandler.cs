public class GeneratorFuelDropHandler : ItemHolderDropHandler
{
    // 드롭 시에는 슬롯에 보관만 하고(RepairSlotDropHandler와 동일), 실제 투입은 GeneratorUI의 "넣기" 버튼에서 확정한다.
    protected override bool OnItemDropped(ItemData data, int amount, int uid)
    {
        if (GeneratorFuelTable.Instance.Get(data.id) == null) return false;

        SetDisplay(data, amount);
        return true;
    }

    // "넣기" 버튼에서 호출 — 성공 시 슬롯을 비운다(아이템은 이미 드롭 시점에 인벤토리에서 빠져나갔으므로 반환하지 않음).
    public bool TryInsertToGenerator()
    {
        if (!IsSetted || Generator.Instance == null) return false;
        if (!Generator.Instance.TryInsertFuel(DroppedItemData, DroppedAmount)) return false;

        ClearDisplay();
        return true;
    }
}
