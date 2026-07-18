using System;
using UnityEngine.EventSystems;

public class GeneratorFuelDropHandler : ItemHolderDropHandler
{
    public event Action<ItemData, int> OnItemSet;
    public event Action OnItemCleared;

    // 드롭 시에는 슬롯에 보관만 하고(RepairSlotDropHandler와 동일), 실제 투입은 GeneratorUI의 "넣기" 버튼에서 확정한다.
    protected override bool OnItemDropped(ItemData data, int amount, int uid)
    {
        if (GeneratorFuelTable.Instance.Get(data.id) == null) return false;

        SetDisplay(data, amount);
        OnItemSet?.Invoke(data, amount);
        return true;
    }

    protected override void ClearDisplay()
    {
        base.ClearDisplay();
        OnItemCleared?.Invoke();
    }

    // 기본 구현은 SlotInteractionManager를 거쳐 장비 컨텍스트 메뉴(무기/방어구용)를 띄운다 — 연료 슬롯엔 맞지 않으므로
    // RepairSlotDropHandler처럼 우클릭 시 바로 인벤토리로 반환한다.
    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;
        if (!IsSetted) return;

        Unequip();
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
