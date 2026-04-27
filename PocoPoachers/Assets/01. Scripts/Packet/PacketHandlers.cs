using System.Collections.Generic;
using Google.FlatBuffers;
using UnityEngine;

public static class PacketHandlers
{
    // S_* 패킷 수신 시 아래 추가

    public static void OnS_LoginRes(FlatPacket root)
    {
        var pkt = root.TypeAsS_LoginRes();
        bool success = pkt.Success;
        int playerId = pkt.UserInfo?.Id ?? 0;
        string userName = pkt.UserInfo?.Name ?? string.Empty;
        int level = pkt.UserInfo?.Level ?? 0;

        MainThreadDispatcher.Enqueue(() =>
        {
            NetworkManager.Instance?.OnLoginResult(success, playerId, userName, level);
        });
    }

    public static void OnS_MoveNtf(FlatPacket root)
    {
        var pkt = root.TypeAsS_MoveNtf();
        float x = pkt.Pos?.X ?? 0f;
        float y = pkt.Pos?.Y ?? 0f;
        float z = pkt.Pos?.Z ?? 0f;
        int playerId = pkt.PlayerId;
        Vector3 pos = new Vector3(x, y, z);
        float rotation = pkt.Rotation;
        sbyte moveType = pkt.MoveType;

        ObjectManager.Instance?.QueueMove(ObjectKind.Player, playerId, pos, rotation, moveType);
    }

    public static void OnS_WorldItemSpawnNtf(FlatPacket root)
    {
        var pkt = root.TypeAsS_WorldItemSpawnNtf();
        int uid = pkt.Uid;
        int typeId = pkt.TypeId;
        float x = pkt.Pos?.X ?? 0f;
        float y = pkt.Pos?.Y ?? 0f;
        float z = pkt.Pos?.Z ?? 0f;
        Vector3 pos = new Vector3(x, y, z);
        float rotation = pkt.Rotation;

        MainThreadDispatcher.Enqueue(() =>
        {
            ObjectManager.Instance?.QueueWorldItemSpawn(uid, typeId, pos, rotation);
        });
    }

    public static void OnS_WorldItemDespawnNtf(FlatPacket root)
    {
        var pkt = root.TypeAsS_WorldItemDespawnNtf();
        int uid = pkt.Uid;

        MainThreadDispatcher.Enqueue(() =>
        {
            ObjectManager.Instance?.Despawn(ObjectKind.WorldItem, uid);
        });
    }

    public static void OnS_SpawnItemBoxNtf(FlatPacket root)
    {
        var pkt = root.TypeAsS_SpawnItemBoxNtf();
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

    public static void OnS_AddItemRes(FlatPacket root)
    {
        var pkt = root.TypeAsS_AddItemRes();
        bool success = pkt.Success;
        int itemId = pkt.ItemId;
        int amount = pkt.Amount;

        MainThreadDispatcher.Enqueue(() =>
        {
            // TODO: InventoryManager에 결과 전달 (itemId로 ItemData 조회 후 AddItem)
            Debug.Log($"[Inventory] AddItem {(success ? "성공" : "실패")}: id={itemId} x{amount}");
        });
    }

    public static void OnS_RemoveItemRes(FlatPacket root)
    {
        var pkt = root.TypeAsS_RemoveItemRes();
        bool success = pkt.Success;
        int itemId = pkt.ItemId;
        int amount = pkt.Amount;

        MainThreadDispatcher.Enqueue(() =>
        {
            // TODO: InventoryManager에 결과 전달
            Debug.Log($"[Inventory] RemoveItem {(success ? "성공" : "실패")}: id={itemId} x{amount}");
        });
    }

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

    public static void OnS_SuccessGainItemNtf(FlatPacket root)
    {
        var pkt = root.TypeAsS_SuccessGainItemNtf();
        var item = ItemTable.Instance.Get(pkt.TypeId);
        int amount = pkt.Amount;

        MainThreadDispatcher.Enqueue(() =>
        {
            GameManager.GetInstance().GainedInventory.AddItem(item, amount);
            GameManager.GetInstance().GiveInventory.RemoveItem(item, amount);
        });
    }

}
