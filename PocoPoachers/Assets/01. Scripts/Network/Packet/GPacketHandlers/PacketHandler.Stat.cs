using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnG_StatSync(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;
        if (!RoomManager.TryResolveGuestSender(0, allowAutoRegister: false, out int senderId))
            return;

        var pkt = root.TypeAsG_StatSync();
        float maxHp = Mathf.Max(pkt.MaxHp, 1f);

        var om = ObjectManager.Instance;
        StatBase stat = null;
        if (om != null && om.TryGet(ObjectKind.Player, senderId, out var worldObj))
        {
            stat = worldObj.GetComponent<StatBase>();
            if (stat == null)
                stat = worldObj.gameObject.AddComponent<RemotePlayerStat>();
        }

        float hp = NetworkPlayerAuthority.SanitizeGuestHp(stat, pkt.Hp, maxHp);
        float stamina = Mathf.Clamp(pkt.Stamina, 0f, 200f);
        float battery = Mathf.Clamp(pkt.Battery, 0f, 200f);

        if (stat != null)
        {
            stat.SetHpFromNetwork(hp, maxHp, 0);
            if (stat is RemotePlayerStat remote)
            {
                remote.SetVitalsFromNetwork(stamina, battery);
                remote.SetArmorDefenseRate(pkt.Defense);
            }
        }

        PacketBuilder.BroadcastToGuests(senderId, new H_StatSyncT
        {
            PlayerId = senderId,
            Hp       = hp,
            MaxHp    = maxHp,
            Stamina  = stamina,
            Battery  = battery,
            Defense  = pkt.Defense,
        }, H_StatSync.Pack, PacketType.H_StatSync);
    }
}
