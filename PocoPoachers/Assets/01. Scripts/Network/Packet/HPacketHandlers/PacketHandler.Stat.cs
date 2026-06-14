using UnityEngine;

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

        float prevHp = stat.CurrentHp;
        stat.SetHpFromNetwork(pkt.Hp, pkt.MaxHp, 0);

        //float damage = prevHp - pkt.Hp;
        //if (damage > 0.1f)
        //    DamageTextUI.Show(damage, worldObj.transform.position + Vector3.up);

        if (stat is RemotePlayerStat remote)
        {
            remote.SetVitalsFromNetwork(pkt.Stamina, pkt.Battery);
            remote.SetArmorDefenseRate(pkt.Defense);
        }
    }
}
