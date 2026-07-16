using System.Collections.Generic;
using UnityEngine;

public enum RescueState : sbyte
{
    Started   = 0,
    Cancelled = 1,
    Completed = 2,
}

// 구출 진행을 구출자·대상 두 명에게만 전달하는 호스트 전용 중계
// 구출자가 호스트면 RoomSync.Rescue가, 게스트면 OnG_Rescue가 Relay를 호출한다
public static class RescueRelay
{
    private const float MinDuration = 0.1f;
    private const float MaxDuration = 10f;

    // 부활 시 회복되는 최대 HP 대비 비율
    private const float ReviveHpRatio = 0.5f;

    // 진행 중인 구출 (대상 id → 구출자 id) — 두 명이 같은 대상을 동시에 구출하는 것을 막는다
    private static readonly Dictionary<int, int> _activeRescues = new();

    public static void Clear() => _activeRescues.Clear();

    public static void Relay(int rescuerId, int targetId, RescueState state, float duration)
    {
        if (!RoomManager.IsHost) return;
        if (rescuerId == targetId) return;
        if (!TryUpdateActiveRescue(rescuerId, targetId, state)) return;

        duration = Mathf.Clamp(duration, MinDuration, MaxDuration);

        if (state == RescueState.Completed)
            ReviveTarget(targetId);

        Deliver(rescuerId, rescuerId, targetId, state, duration);
        Deliver(targetId, rescuerId, targetId, state, duration);
    }

    // HP 권한은 호스트에 있으므로 부활도 여기서만 판정한다
    // 회복 결과는 각 StatBase의 OnLocalHpChanged가 기존 H_StatSync로 전파하므로 별도 패킷이 필요 없다
    private static void ReviveTarget(int targetId)
    {
        var stat = FindPlayerStat(targetId);
        if (stat == null)
        {
            Debug.LogWarning($"[RescueRelay] 구출 대상 {targetId}의 스탯을 찾을 수 없어 부활시키지 못했습니다");
            return;
        }

        stat.Revive(stat.MaxHp * ReviveHpRatio);
        Debug.Log($"[RescueRelay] 플레이어 {targetId} 부활 {stat.CurrentHp}/{stat.MaxHp}");
    }

    // 호스트 자신은 _objects에 없으므로(ObjectManager.ApplyMove의 IsLocalPlayer) 따로 찾는다
    private static StatBase FindPlayerStat(int playerId)
    {
        if (playerId == (NetworkManager.Instance?.MyPlayerId ?? 0))
            return Object.FindFirstObjectByType<PlayerStat>();

        if (ObjectManager.Instance != null && ObjectManager.Instance.TryGet(ObjectKind.Player, playerId, out var worldObj))
            return worldObj.GetComponent<StatBase>();

        return null;
    }

    // 중복 구출 차단 — 이미 다른 구출자가 붙어 있는 대상의 시작 요청은 버린다
    private static bool TryUpdateActiveRescue(int rescuerId, int targetId, RescueState state)
    {
        if (state == RescueState.Started)
        {
            if (_activeRescues.TryGetValue(targetId, out int owner) && owner != rescuerId)
                return false;

            _activeRescues[targetId] = rescuerId;
            return true;
        }

        // 취소/완료는 진행 중인 구출의 주인만 보낼 수 있다
        if (!_activeRescues.TryGetValue(targetId, out int current) || current != rescuerId)
            return false;

        _activeRescues.Remove(targetId);
        return true;
    }

    // 수신자가 호스트 자신이면 패킷 대신 로컬 이벤트로 바로 반영한다
    private static void Deliver(int receiverId, int rescuerId, int targetId, RescueState state, float duration)
    {
        if (receiverId == (NetworkManager.Instance?.MyPlayerId ?? 0))
        {
            RescueInteractable.RaiseProgress(state, duration);
            return;
        }

        PacketBuilder.SendReliableToGuest(receiverId, new H_RescueT
        {
            RescuerId = rescuerId,
            TargetId  = targetId,
            State     = (sbyte)state,
            Duration  = duration,
        }, H_Rescue.Pack, PacketType.H_Rescue);
    }
}
