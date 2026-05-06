using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnH_ItemSpawn(FlatPacket root)
    {
        var pkt = root.TypeAsH_ItemSpawn();
        int uid = pkt.Uid;
        int typeId = pkt.TypeId;
        float x = pkt.Pos?.X ?? 0f;
        float y = pkt.Pos?.Y ?? 0f;
        float z = pkt.Pos?.Z ?? 0f;
        Vector3 pos = new Vector3(x, y, z);
        float rotation = pkt.Rotation;
        int[] item_ids = pkt.GetItemIdsArray();

        MainThreadDispatcher.Enqueue(() =>
        {
            var box = ObjectManager.Instance?.SpawnItemBox(uid, typeId, pos, rotation);
            box?.Initialize(item_ids);
        });
    }

    public static void OnH_ItemDespawn(FlatPacket root)
    {
        var pkt = root.TypeAsH_ItemDespawn();
        int uid = pkt.Uid;

        MainThreadDispatcher.Enqueue(() =>
        {
            ObjectManager.Instance?.Despawn(ObjectKind.WorldItem, uid);
        });
    }

    // 호스트가 요청자에게만 전송 — 내 인벤토리 업데이트 및 박스 시각 갱신
    public static void OnH_ItemGainResult(FlatPacket root)
    {
        var pkt = root.TypeAsH_ItemGainResult();
        if (!pkt.Success) return;

        var om       = ObjectManager.Instance;
        var itemData = ItemTable.Instance.Get(pkt.ItemTypeId);
        if (itemData == null) return;

        // 내 인벤토리에 아이템 추가
        var playerInv = FindLocalInventory();
        playerInv?.AddItemAtSlot(pkt.SlotIndex, itemData, pkt.Amount);

        // 박스에서 해당 슬롯 아이템 제거
        if (pkt.RemovedSlotIndex >= 0 && om != null
            && om.TryGet(ObjectKind.ItemBox, pkt.BoxUid, out var boxObj))
        {
            var boxInv = boxObj.GetComponent<Inventory>();
            boxInv?.RemoveItemAtSlot(pkt.RemovedSlotIndex, itemData, pkt.Amount);
        }
    }

    // 플레이어 인벤토리 전체 동기화 (PlayerId가 나와 일치할 때만 적용)
    public static void OnH_InventoryUpdate(FlatPacket root)
    {
        var pkt = root.TypeAsH_InventoryUpdate();
        if (pkt.PlayerId != NetworkManager.Instance?.MyPlayerId) return;

        // 전체 인벤토리 스냅샷 적용 — 개별 결과 패킷이 이미 처리하므로 UI 갱신만
        // 향후 서버 권위 인벤토리 동기화 시 확장
    }

    // 호스트가 요청자에게만 전송 — 교환 결과 적용
    public static void OnH_ItemExchangeResult(FlatPacket root)
    {
        var pkt = root.TypeAsH_ItemExchangeResult();
        if (!pkt.Success) return;

        var om          = ObjectManager.Instance;
        var boxItemData = ItemTable.Instance.Get(pkt.BoxItemId);
        var plrItemData = ItemTable.Instance.Get(pkt.PlayerItemId);

        // 내 인벤토리 업데이트
        var playerInv = FindLocalInventory();
        if (playerInv != null)
        {
            if (plrItemData != null && pkt.PlayerItemAmount > 0)
                playerInv.RemoveItemAtSlot(pkt.PlayerSlotIndex, plrItemData, pkt.PlayerItemAmount);

            if (boxItemData != null)
                playerInv.AddItemAtSlot(pkt.PlayerSlotIndex, boxItemData, pkt.BoxItemAmount);
        }

        // 박스 시각 동기화
        if (om != null && om.TryGet(ObjectKind.ItemBox, pkt.BoxUid, out var boxObj))
        {
            var boxInv = boxObj.GetComponent<Inventory>();
            if (boxInv != null)
            {
                if (boxItemData != null)
                    boxInv.RemoveItemAtSlot(pkt.BoxSlotIndex, boxItemData, pkt.BoxItemAmount);

                if (plrItemData != null && pkt.PlayerItemAmount > 0)
                    boxInv.AddItemAtSlot(pkt.BoxSlotIndex, plrItemData, pkt.PlayerItemAmount);
            }
        }
    }

    private static Inventory FindLocalInventory()
    {
        return Object.FindAnyObjectByType<PlayerController>()?.GetComponent<Inventory>();
    }
}
