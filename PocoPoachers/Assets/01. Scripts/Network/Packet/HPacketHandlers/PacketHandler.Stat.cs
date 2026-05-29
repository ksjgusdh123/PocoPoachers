public static partial class PacketHandlers
{
    public static void OnH_StatSync(FlatPacket root)
    {
        var pkt = root.TypeAsH_StatSync();

        var om = ObjectManager.Instance;
        if (om == null) return;

        if (!om.TryGet(ObjectKind.Player, pkt.PlayerId, out var worldObj)) return;

        var stat = worldObj.GetComponent<StatBase>();
        if (stat == null)
            stat = worldObj.gameObject.AddComponent<RemotePlayerStat>();

        stat.SetHpFromNetwork(pkt.Hp, pkt.MaxHp);
    }
}
