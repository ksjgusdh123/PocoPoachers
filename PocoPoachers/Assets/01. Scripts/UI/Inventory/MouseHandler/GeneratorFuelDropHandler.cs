public class GeneratorFuelDropHandler : ItemHolderDropHandler
{
    // 투입 즉시 소모되는 슬롯 — SetDisplay를 호출하지 않아 아이템을 보관하지 않는다 (RepairSlotDropHandler와 차이점).
    protected override bool OnItemDropped(ItemData data, int amount, int uid)
    {
        if (GeneratorFuelTable.Instance.Get(data.id) == null) return false;

        return Generator.Instance != null && Generator.Instance.TryInsertFuel(data, amount);
    }
}
