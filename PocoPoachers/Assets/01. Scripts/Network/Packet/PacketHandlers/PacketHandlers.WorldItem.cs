using Google.FlatBuffers;
using UnityEngine;

public static partial class PacketHandlers
{
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

    public static void OnS_ChangeItemBox(FlatPacket root)
    {
        var pkt = root.TypeAsS_ChangeItemBox();
        bool isGain = pkt.IsGain;
        int boxUid = pkt.BoxUid;
        int typeId = pkt.TypeId;
        int amount = pkt.Amount;
        int slotIndex = pkt.SlotIndex;

        MainThreadDispatcher.Enqueue(() =>
        {
            ItemData data = ItemTable.Instance.Get(typeId);
            if (data == null) return;

            if (!ObjectManager.Instance.TryGet(ObjectKind.ItemBox, boxUid, out var worldObj)) return;
            if (slotIndex == -1)
            {
                if (isGain) worldObj.GetComponent<Inventory>()?.AddItem(data, amount);
                else worldObj.GetComponent<Inventory>()?.RemoveItem(data, amount);
            }
            else
            {
                if (isGain) worldObj.GetComponent<Inventory>()?.AddItemAtSlot(slotIndex, data, amount);
                else worldObj.GetComponent<Inventory>()?.RemoveItemAtSlot(slotIndex, data, amount);
            }
        });
    }

    public static void OnS_SuccessGainItemNtf(FlatPacket root)
    {
        var pkt = root.TypeAsS_SuccessGainItemNtf();
        var item = ItemTable.Instance.Get(pkt.TypeId);
        int amount = pkt.Amount;
        int slotIndex = pkt.SlotIndex;
        int removedSlotIndex = pkt.RemovedSlotIndex;

        MainThreadDispatcher.Enqueue(() =>
        {
            if (slotIndex == -1)
            {
                GameManager.GetInstance().GainedInventory.AddItem(item, amount);
                GameManager.GetInstance().GiveInventory.RemoveItem(item, amount);
            }
            else
            {
                GameManager.GetInstance().GainedInventory.AddItemAtSlot(slotIndex, item, amount);
                GameManager.GetInstance().GiveInventory.RemoveItemAtSlot(removedSlotIndex, item, amount);
            }
        });
    }

    public static void OnS_ExchangeItemResultNtf(FlatPacket root)
    {
        var pkt = root.TypeAsS_ExchangeItemResultNtf();
        if (!pkt.IsSuccess) return;

        int playerGainItemId = pkt.PlayerGainItemId;
        int playerGainAmount = pkt.PlayerGainItemAmount;
        int playerSlotIndex = pkt.PlayerSlotIndex;
        int boxGainItemId = pkt.BoxGainItemId;
        int boxGainAmount = pkt.BoxGainItemAmount;
        int boxSlotIndex = pkt.BoxSlotIndex;

        MainThreadDispatcher.Enqueue(() =>
        {
            // GiveInventory = 플레이어, GainedInventory = 박스 (OnSlotDropped에서 SaveChangeInventorys(_inventory, interactionInven) 순서 기준)
            var playerInven = GameManager.GetInstance().GiveInventory;
            var boxInven = GameManager.GetInstance().GainedInventory;

            ItemData playerGainedItem = ItemTable.Instance.Get(playerGainItemId);
            ItemData boxGainedItem = ItemTable.Instance.Get(boxGainItemId);

            if (playerGainedItem != null && boxGainedItem != null)
            {
                playerInven.RemoveItemAtSlot(playerSlotIndex, boxGainedItem, boxGainAmount);
                boxInven.RemoveItemAtSlot(boxSlotIndex, playerGainedItem, playerGainAmount);
                playerInven.AddItemAtSlot(playerSlotIndex, playerGainedItem, playerGainAmount);
                boxInven.AddItemAtSlot(boxSlotIndex, boxGainedItem, boxGainAmount);
                Debug.Log($"playerSlot : {playerSlotIndex}        boxSlot : {boxSlotIndex}");
            }
        });
    }
}
