using UnityEngine;

public static partial class PacketHandlers
{
    // 게스트가 추가탄 드론을 켜거나 껐다.
    // 호스트도 자기 씬에 그 드론을 띄워야 한다 — 게스트 총알의 명중을 판정하는 게 호스트라,
    // 이 드론이 없으면 게스트의 유도탄에 데미지가 실리지 않는다.
    // damage는 게스트의 스킬 테이블/강화에서 나온 값이라 호스트가 재계산할 수 없어 그대로 신뢰하되,
    // 다른 G_ 핸들러와 같은 기준으로 상한만 검증한다.
    public static void OnG_DroneState(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;
        if (!RoomManager.TryGetGuestIdFromPacket(0, autoRegister: false, out int guestId)) return;

        var packet = root.TypeAsG_DroneState();
        float damage = Mathf.Clamp(packet.Damage, 0f, MaxDroneDamage);

        CombatDrone.SetActiveFor(guestId, packet.Active, damage);
        RoomSync.DroneStateRelay(guestId, packet.Active, damage);
    }

    private const float MaxDroneDamage = 200f;
}
