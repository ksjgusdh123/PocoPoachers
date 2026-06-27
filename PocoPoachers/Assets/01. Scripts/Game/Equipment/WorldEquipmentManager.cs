using System.Collections.Generic;
using UnityEngine;

// 호스트에서만 사용하는 장비 내구도 상태 저장소
// uid -> 내구도를 들고 있어서, 장착/해제를 반복해도 같은 uid면 동일한 내구도가 유지된다
public static class WorldEquipmentManager
{
    private class State
    {
        public int ItemId;
        public float Current;
        public float Max;
    }

    private static readonly Dictionary<int, State> _states = new();

    // 장착 시 호출 — 처음 보는 uid면 풀내구도로 등록, 이미 있으면 기존 상태 반환
    public static (float current, float max) GetOrCreate(int uid, int itemId, float maxDurability)
    {
        if (uid == 0) return (maxDurability, maxDurability);

        if (!_states.TryGetValue(uid, out var state))
        {
            state = new State { ItemId = itemId, Current = maxDurability, Max = maxDurability };
            _states[uid] = state;
        }

        return (state.Current, state.Max);
    }

    // 발사/피격 등으로 내구도 변화 — amount는 음수(감소)/양수(회복) 모두 가능
    public static (float current, float max) ApplyChange(int uid, int itemId, float amount, float defaultMaxDurability)
    {
        if (uid == 0) return (0f, defaultMaxDurability);

        if (!_states.TryGetValue(uid, out var state))
        {
            state = new State { ItemId = itemId, Current = defaultMaxDurability, Max = defaultMaxDurability };
            _states[uid] = state;
        }

        state.Current = Mathf.Clamp(state.Current + amount, 0f, state.Max);
        return (state.Current, state.Max);
    }

    public static void Clear() => _states.Clear();
}
