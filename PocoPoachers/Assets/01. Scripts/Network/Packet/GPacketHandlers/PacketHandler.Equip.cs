using System.Collections.Generic;

public static partial class PacketHandlers
{
    public static void OnG_Equip(FlatPacket root)
    {
        var packet = root.TypeAsG_Equip();
        if (!RoomManager.TryGetGuestIdFromPacket(packet.PlayerId, autoRegister: false, out int guestId))
            return;

        int itemId    = packet.ItemId;
        int itemUid   = packet.ItemUid;
        int slotIndex = packet.SlotIndex;

        // 오브젝트가 아직 없어도 상태는 남긴다 — 스폰 시 RemoteEquipState.ApplyTo가 입혀준다
        RemoteEquipState.SetSlot(guestId, slotIndex, itemId, itemUid);

        EquippableItemBase spawned = null;
        if (ObjectManager.Instance.TryGet(ObjectKind.Player, guestId, out var worldObj))
        {
            ApplyRemoteArmorStats(worldObj, guestId, itemId, slotIndex, sendToOthers: true);
            spawned = ApplyRemoteEquipVisual(worldObj, itemId, itemUid, slotIndex);
        }

        if (RoomManager.IsHost)
        {
            PacketBuilder.BroadcastReliableToGuests(guestId,
                new H_EquipT
                {
                    PlayerId  = guestId,
                    ItemId    = itemId,
                    ItemUid   = itemUid,
                    SlotIndex = slotIndex,
                },
                H_Equip.Pack, PacketType.H_Equip);

            if (spawned != null && itemUid != 0)
            {
                var (current, max) = WorldEquipmentManager.GetOrCreate(itemUid, itemId, spawned.MaxDurability);
                spawned.SetDurability(current);
                PacketBuilder.BroadcastToGuests(new H_DurabilityT { ItemUid = itemUid, Current = current, Max = max },
                    H_Durability.Pack, PacketType.H_Durability);
            }

            if (itemUid != 0 && spawned is GunBase)
                SendGunStateToGuest(guestId, itemUid);
        }
    }

    // 게스트는 WorldEquipmentManager를 신뢰할 수 없어(자기 세이브 데이터가 들어있음) 탄약/파츠를 스스로 복원하지 못한다.
    // 호스트가 보관 중인 값을 장착한 게스트에게만 돌려준다 — 남의 화면에는 영향 없는 정보다.
    static void SendGunStateToGuest(int guestId, int gunUid)
    {
        var partIds    = new List<int>();
        var partLevels = new List<int>();

        foreach (var kv in WorldEquipmentManager.GetParts(gunUid))
        {
            if (GunPartTable.Instance.Get(kv.Value) == null) continue;

            int partUid = WorldEquipmentManager.GetPartUid(gunUid, kv.Key);
            partIds.Add(kv.Value);
            // 강화 레벨은 호스트가 계산해서 보낸다 (게스트 로컬 강화 상태는 호스트 월드와 무관)
            partLevels.Add(WorldEquipmentManager.GetEnhancementLevel(partUid, kv.Value));
        }

        bool hasAmmo = WorldEquipmentManager.TryGetAmmo(gunUid, out int curAmmo, out int maxMag);
        if (!hasAmmo && partIds.Count == 0) return; // 돌려줄 상태가 없으면 보내지 않는다

        PacketBuilder.SendReliableToGuest(guestId, new H_GunStateT
        {
            GunUid      = gunUid,
            HasAmmo     = hasAmmo,
            CurrentAmmo = curAmmo,
            MaxMagazine = maxMag,
            PartIds     = partIds,
            PartLevels  = partLevels,
        }, H_GunState.Pack, PacketType.H_GunState);
    }
}
