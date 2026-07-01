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
        float defense = 0f;

        if (stat != null)
        {
            // 방어율은 장착 아이템 기준으로 호스트가 직접 계산한 값만 신뢰한다(ApplyRemoteArmorStats).
            // 게스트가 보낸 packet.Defense를 그대로 적용하면 임의의 값(예: 1.0)으로 무적이 될 수 있음.
            if (stat is RemotePlayerStat remote)
            {
                defense = remote.ArmorDefenseRate;
                remote.ApplyNetworkStats(hp, maxHp, stamina, battery, defense);
            }
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
            Defense  = defense,
        }, H_StatSync.Pack, PacketType.H_StatSync);
    }
}
