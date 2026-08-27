using UnityEngine;

public static partial class PacketHandlers
{
    // 게스트의 도발 요청 — AI 타겟은 호스트만 판정하므로(TargetDetector가 호스트 전용)
    // 실제 타겟 변경을 호스트가 대신 수행한다. 중계할 것은 없다(연출 없는 즉발 스킬).
    public static void OnG_Taunt(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;
        if (!RoomManager.TryGetGuestIdFromPacket(0, autoRegister: false, out int guestId))
            return;

        if (ObjectManager.Instance == null ||
            !ObjectManager.Instance.TryGet(ObjectKind.Player, guestId, out var playerObj))
            return;

        var packet = root.TypeAsG_Taunt();
        var pos = packet.Pos;
        Vector3 center = pos.HasValue
            ? new Vector3(pos.Value.X, pos.Value.Y, pos.Value.Z)
            : playerObj.transform.position;

        TauntSkill.ApplyAuthoritative(center, packet.Radius, packet.Duration, playerObj.gameObject);
    }
}
