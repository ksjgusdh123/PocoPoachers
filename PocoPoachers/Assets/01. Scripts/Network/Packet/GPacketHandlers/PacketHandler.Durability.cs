using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnG_Durability(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;
        if (!RoomManager.TryResolveGuestSender(0, allowAutoRegister: false, out int senderId))
            return;

        var pkt = root.TypeAsG_Durability();
        if (pkt.ItemUid == 0) return;
        if (!NetworkPlayerAuthority.GuestOwnsItemUid(senderId, pkt.ItemUid)) return;
        if (Mathf.Abs(pkt.Amount) > 5f) return;

        var (current, max) = WorldEquipmentManager.ApplyChange(pkt.ItemUid, pkt.ItemId, pkt.Amount, 1f);

        PacketBuilder.BroadcastToGuests(new H_DurabilityT
        {
            ItemUid = pkt.ItemUid,
            Current = current,
            Max     = max,
        }, H_Durability.Pack, PacketType.H_Durability);
    }
}
