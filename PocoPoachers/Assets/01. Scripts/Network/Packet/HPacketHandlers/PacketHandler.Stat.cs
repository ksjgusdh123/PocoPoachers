using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnH_StatSync(FlatPacket root)
    {
        var packet = root.TypeAsH_StatSync();

        var objectManager = ObjectManager.Instance;
        if (objectManager == null) return;

        // 내 플레이어에 대한 호스트의 권한 판정(피격/사망) — 로컬 PlayerStat은 _objects에 없으므로 별도 처리
        var nm = NetworkManager.Instance;
        if (nm != null && packet.PlayerId == nm.MyPlayerId)
        {
            var myStat = UnityEngine.Object.FindFirstObjectByType<PlayerStat>();
            if (myStat == null) return;

            // 깎인 양을 damage로 넘겨 피격 연출/방어구 내구도 등 OnDamaged 흐름도 동일하게 발동
            float damage = Mathf.Max(0f, myStat.CurrentHp - packet.Hp);
            Debug.Log($"[H_StatSync] 내 HP 판정 수신 {packet.Hp}/{packet.MaxHp} (damage {damage})");
            myStat.SetHpFromNetwork(packet.Hp, packet.MaxHp, damage);
            return;
        }

        if (!objectManager.TryGet(ObjectKind.Player, packet.PlayerId, out var worldObj)) return;

        var stat = worldObj.GetComponent<StatBase>();
        if (stat == null)
            stat = worldObj.gameObject.AddComponent<RemotePlayerStat>();

        if (stat is RemotePlayerStat remote)
        {
            remote.ApplyNetworkStats(packet.Hp, packet.MaxHp, packet.Stamina, packet.Battery, packet.Defense, packet.CritMultiplier, packet.RangeMultiplier, packet.LuckyChance, packet.LuckyMultiplier, packet.AttackPowerMultiplier);
            return;
        }

        stat.SetHpFromNetwork(packet.Hp, packet.MaxHp, 0);
    }
}
