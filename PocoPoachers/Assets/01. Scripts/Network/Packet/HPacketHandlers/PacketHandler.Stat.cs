using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnH_StatSync(FlatPacket root)
    {
        var packet = root.TypeAsH_StatSync();

        var objectManager = ObjectManager.Instance;
        if (objectManager == null) return;

        if (!objectManager.TryGet(ObjectKind.Player, packet.PlayerId, out var worldObj)) return;

        var stat = worldObj.GetComponent<StatBase>();
        if (stat == null)
            stat = worldObj.gameObject.AddComponent<RemotePlayerStat>();

        if (stat is RemotePlayerStat remote)
        {
            remote.ApplyNetworkStats(packet.Hp, packet.MaxHp, packet.Stamina, packet.Battery, packet.Defense);
            return;
        }

        stat.SetHpFromNetwork(packet.Hp, packet.MaxHp, 0);
    }
}
