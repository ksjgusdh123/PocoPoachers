using UnityEngine;

public static partial class PacketHandlers
{
    private const float MaxCritMultiplier = 5f;
    private const float MaxRangeMultiplier = 5f;
    private const float MaxLuckyShotChance = 1f;
    private const float MaxLuckyShotMultiplier = 5f;
    private const float MaxAttackPowerMultiplier = 5f;
    private const float MaxDefenseBuffRate = 1f;

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

        // 방어율과 달리 크리 배율은 스킬에서 나오는 값이라 호스트가 재계산할 수 없다.
        // 그래서 요청값을 신뢰하되 상한만 막는다 (다른 G_ 핸들러의 검증 방식과 동일).
        float critMultiplier = Mathf.Clamp(packet.CritMultiplier, 1f, MaxCritMultiplier);
        float rangeMultiplier = Mathf.Clamp(packet.RangeMultiplier, 1f, MaxRangeMultiplier);
        float luckyChance = Mathf.Clamp(packet.LuckyChance, 0f, MaxLuckyShotChance);
        float luckyMultiplier = Mathf.Clamp(packet.LuckyMultiplier, 1f, MaxLuckyShotMultiplier);
        float attackPowerMultiplier = Mathf.Clamp(packet.AttackPowerMultiplier, 1f, MaxAttackPowerMultiplier);
        float defenseBuffRate = Mathf.Clamp(packet.DefenseBuffRate, 0f, MaxDefenseBuffRate);

        if (stat != null)
        {
            // 방어율은 장착 아이템 기준으로 호스트가 직접 계산한 값만 신뢰한다(ApplyRemoteArmorStats).
            // 게스트가 보낸 packet.Defense를 그대로 적용하면 임의의 값(예: 1.0)으로 무적이 될 수 있음.
            // (방어력 버프 오라는 별개 필드(defenseBuffRate)라 여기 영향 없이 그대로 신뢰한다)
            if (stat is RemotePlayerStat remote)
            {
                defense = remote.ArmorDefenseRate;
                remote.ApplyNetworkStats(hp, maxHp, stamina, battery, defense, critMultiplier, rangeMultiplier, luckyChance, luckyMultiplier, attackPowerMultiplier, defenseBuffRate);
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
            CritMultiplier = critMultiplier,
            RangeMultiplier = rangeMultiplier,
            LuckyChance = luckyChance,
            LuckyMultiplier = luckyMultiplier,
            AttackPowerMultiplier = attackPowerMultiplier,
            DefenseBuffRate = defenseBuffRate,
        }, H_StatSync.Pack, PacketType.H_StatSync);
    }
}
