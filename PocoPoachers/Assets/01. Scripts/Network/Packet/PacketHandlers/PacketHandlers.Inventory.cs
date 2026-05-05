using System.Collections.Generic;
using Google.FlatBuffers;
using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnS_InventoryNtf(FlatPacket root)
    {
        var pkt = root.TypeAsS_InventoryNtf();
        var items = new Dictionary<int, int>();
        for (int i = 0; i < pkt.ItemsLength; i++)
        {
            var item = pkt.Items(i);
            if (item.HasValue)
                items[item.Value.ItemId] = item.Value.Amount;
        }

        MainThreadDispatcher.Enqueue(() =>
        {
            // TODO: itemId → ItemData 변환 후 Inventory에 적용
            Debug.Log($"[Inventory] 초기 로드 완료: {items.Count}종");
        });
    }

    public static void OnS_ConsumeItemNtf(FlatPacket root)
    {
        var pkt = root.TypeAsS_ConsumeItemNtf();

        Debug.Log("아이템 사용 확인");
    }
}
