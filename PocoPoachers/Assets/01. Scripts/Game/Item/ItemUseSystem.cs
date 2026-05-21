using System;
using UnityEngine;

// 아이템 사용 효과 적용 담당
// 퀵슬롯(QuickSlotInventory)과 인벤토리 양쪽에서 호출한다
public static class ItemUseSystem
{
    // 아이템 사용 완료 시 발생 (UI 연출, 애니메이션 등이 구독)
    public static event Action<ItemData> OnItemUsed;
    // 인벤토리에서 직접 사용 (슬롯 UI 우클릭 경로)
    public static bool TryUse(ItemData itemData, Inventory inventory)
    {
        if (!CanUse(itemData)) return false;

        int slotIndex = inventory.FindItemSlotIndex(itemData);
        if (slotIndex < 0) return false;

        ApplyEffect(itemData);
        inventory.RemoveItemAtSlot(slotIndex, itemData, 1);
        return true;
    }

    public static bool CanUse(ItemData itemData)
    {
        return itemData != null && itemData.ItemType == ItemType.Consumable;
    }

    // QuickSlotInventory.ConsumeItem에서도 호출하므로 public
    public static void ApplyEffect(ItemData itemData)
    {
        // TODO: 아이템 ID 기반 효과 처리 (회복, 버프 등)
        // switch (itemData.id)
        // {
        //     case 1: PlayerHealth.GetInstance()?.Heal(10); break;
        // }
        Debug.Log($"[ItemUseSystem] {itemData.ItemName} 사용");
        OnItemUsed?.Invoke(itemData);
    }
}
