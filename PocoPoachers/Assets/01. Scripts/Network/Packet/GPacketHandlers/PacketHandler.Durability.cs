using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnG_Durability(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;
        if (!RoomManager.TryGetGuestIdFromPacket(0, autoRegister: false, out int guestId))
            return;

        var packet = root.TypeAsG_Durability();
        if (packet.ItemUid == 0) return;
        if (!GuestValidator.GuestHasItem(guestId, packet.ItemUid)) return;
        if (Mathf.Abs(packet.Amount) > 5f) return;

        float maxDurability = GuestValidator.GetGuestItemMaxDurability(guestId, packet.ItemUid, packet.ItemId);
        var (current, max) = WorldEquipmentManager.ApplyChange(packet.ItemUid, packet.ItemId, packet.Amount, maxDurability);

        PacketBuilder.BroadcastToGuests(new H_DurabilityT
        {
            ItemUid = packet.ItemUid,
            Current = current,
            Max     = max,
        }, H_Durability.Pack, PacketType.H_Durability);
    }
}
