using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnG_StatSync(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;
        if (!RoomManager.TryGetGuestIdFromPacket(0, autoRegister: false, out int guestId))
            return;

        var packet = root.TypeAsG_StatSync();
        float maxHp = Mathf.Max(packet.MaxHp, 1f);

        var objectManager = ObjectManager.Instance;
        StatBase stat = null;
        if (objectManager != null && objectManager.TryGet(ObjectKind.Player, guestId, out var worldObj))
        {
            stat = worldObj.GetComponent<StatBase>();
            if (stat == null)
                stat = worldObj.gameObject.AddComponent<RemotePlayerStat>();
        }

        float hp = GuestValidator.ClampGuestHp(stat, packet.Hp, maxHp);
        float stamina = Mathf.Clamp(packet.Stamina, 0f, 200f);
        float battery = Mathf.Clamp(packet.Battery, 0f, 200f);

        if (stat != null)
        {
            if (stat is RemotePlayerStat remote)
                remote.ApplyNetworkStats(hp, maxHp, stamina, battery, packet.Defense);
            else
                stat.SetHpFromNetwork(hp, maxHp, 0);
        }

        PacketBuilder.BroadcastToGuests(guestId, new H_StatSyncT
        {
            PlayerId = guestId,
            Hp       = hp,
            MaxHp    = maxHp,
            Stamina  = stamina,
            Battery  = battery,
            Defense  = packet.Defense,
        }, H_StatSync.Pack, PacketType.H_StatSync);
    }
}
