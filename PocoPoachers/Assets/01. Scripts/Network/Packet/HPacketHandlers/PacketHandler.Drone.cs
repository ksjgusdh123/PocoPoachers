using UnityEngine;

public static partial class PacketHandlers
{
    // 다른 플레이어의 드론이 켜지거나 꺼졌다. 내 드론은 스킬이 직접 띄우므로 여기서 무시한다.
    public static void OnH_DroneState(FlatPacket root)
    {
        var packet = root.TypeAsH_DroneState();
        if (packet.PlayerId == NetworkManager.Instance?.MyPlayerId) return;

        CombatDrone.SetActiveFor(packet.PlayerId, packet.Active, packet.Damage);
    }

    // 호스트가 판정한 드론 발사를 그대로 그린다. 내 드론이 쏜 것도 여기로 온다 —
    // 게스트는 추측 발사를 하지 않으므로 이게 유일한 발사 경로다.
    public static void OnH_DroneShoot(FlatPacket root)
    {
        var packet = root.TypeAsH_DroneShoot();

        var drone = CombatDrone.FindFor(packet.PlayerId);
        if (drone == null) return;

        if (!EnemyNetSync.TryGetGameObject(packet.EnemyId, out var enemy) || enemy == null) return;

        var collider = enemy.GetComponentInChildren<Collider>();
        if (collider == null) return;

        drone.FireVisual(collider);
    }
}
