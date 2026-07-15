using System.Collections.Generic;
using UnityEngine;

// 호스트/게스트 양쪽에서 쓰는 로컬 캐시 — 남의 장비를 내 화면에 그리기 위한 것이다.
// (WorldEquipmentManager의 uid별 내구도/탄약처럼 호스트가 권위를 갖는 상태가 아니다)
// playerId -> 슬롯별 장착 아이템(itemId/uid). 게스트는 OnH_Equip, 호스트는 OnG_Equip으로 채운다.
//
// Equip 패킷은 플레이어 오브젝트가 생기기 전에 도착할 수 있다(씬 전환 시 ObjectManager.Clear 후
// 오브젝트는 첫 Move 패킷에 지연 생성됨). 패킷은 여기에 상태만 남기고, 오브젝트 생성 시점에
// ApplyTo로 입혀 도착 순서와 무관하게 장비가 보이도록 한다.
// 로컬 플레이어는 ObjectManager에 등록되지 않고 SaveManager 경로로 복원되므로 여기 담기지 않는다.
public static class RemoteEquipState
{
    static readonly Dictionary<int, Dictionary<int, (int ItemId, int Uid)>> _playerSlots = new();

    // Enter Play Mode Options에서 Reload Domain을 끄면 static이 이전 플레이 세션까지 살아남는다.
    // 재사용된 playerId가 지난 세션의 장비를 입고 스폰되는 것을 막는다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => _playerSlots.Clear();

    public static void ClearPlayer(int playerId) => _playerSlots.Remove(playerId);

    public static void Clear() => _playerSlots.Clear();

    public static void SetSlot(int playerId, int slotIndex, int itemId, int uid)
    {
        if (!_playerSlots.TryGetValue(playerId, out var slots))
        {
            slots = new Dictionary<int, (int, int)>();
            _playerSlots[playerId] = slots;
        }

        if (itemId == 0)
            slots.Remove(slotIndex);
        else
            slots[slotIndex] = (itemId, uid);
    }

    // 갓 생성된 원격 플레이어에게 알려진 장착 상태를 입힌다
    public static void ApplyTo(WorldObject worldObj, int playerId)
    {
        if (worldObj == null) return;
        if (!_playerSlots.TryGetValue(playerId, out var slots)) return;

        foreach (var kv in slots)
        {
            // 패킷 경로와 동일한 순서 — 방어구 스탯은 마운트에 장착되기 전에 비교/적용돼야 한다
            PacketHandlers.ApplyRemoteArmorStats(worldObj, playerId, kv.Value.ItemId, kv.Key, sendToOthers: false);
            PacketHandlers.ApplyRemoteEquipVisual(worldObj, kv.Value.ItemId, kv.Value.Uid, kv.Key);
        }
    }
}
