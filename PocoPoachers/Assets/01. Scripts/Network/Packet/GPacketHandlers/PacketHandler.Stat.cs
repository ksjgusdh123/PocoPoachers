public static partial class PacketHandlers
{
    public static void OnG_StatSync(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;

        var pkt = root.TypeAsG_StatSync();
        int senderId = RoomManager.LastGuestId;

        // 호스트 측 프록시에도 적용
        var om = ObjectManager.Instance;
        if (om != null && om.TryGet(ObjectKind.Player, senderId, out var worldObj))
        {
            var stat = worldObj.GetComponent<StatBase>();
            if (stat == null)
                stat = worldObj.gameObject.AddComponent<RemotePlayerStat>();
            stat.SetHpFromNetwork(pkt.Hp, pkt.MaxHp);
            if (stat is RemotePlayerStat remote)
            {
                remote.SetVitalsFromNetwork(pkt.Stamina, pkt.Hunger, pkt.Thirst);
                remote.SetArmorDefense(pkt.Defense);
            }
        }

        PacketBuilder.BroadcastToGuests(senderId, new H_StatSyncT
        {
            PlayerId = senderId,
            Hp       = pkt.Hp,
            MaxHp    = pkt.MaxHp,
            Stamina  = pkt.Stamina,
            Hunger   = pkt.Hunger,
            Thirst   = pkt.Thirst,
            Defense  = pkt.Defense,
        }, H_StatSync.Pack, PacketType.H_StatSync);
    }
}
