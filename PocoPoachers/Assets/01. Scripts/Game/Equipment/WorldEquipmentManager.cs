using System.Collections.Generic;
using UnityEngine;

// 호스트에서만 사용하는 장비 런타임 상태 저장소
// uid -> 상태(내구도/장탄수/장착 파츠)를 들고 있어, 장착/해제를 반복해도 같은 uid면 상태가 유지된다
public static class WorldEquipmentManager
{
    private class State
    {
        public int ItemId;
        public float Current;
        public float Max;
        public int CurrentAmmo;
        public int MaxAmmo;
        public bool AmmoSet; // SetAmmo가 실제로 한 번이라도 호출됐는지 (GetOrCreate의 부수 생성과 구분하기 위함)
        public readonly Dictionary<SlotType, int> Parts = new();   // 슬롯 -> 파츠 id
    }

    private static readonly Dictionary<int, State> _states = new();
    private static readonly Dictionary<SlotType, int> _emptyParts = new();

    // uid에 상태가 없으면 만들어 반환. uid==0(비영속)이면 null
    private static State Ensure(int uid)
    {
        if (uid == 0) return null;
        if (!_states.TryGetValue(uid, out var state))
        {
            state = new State();
            _states[uid] = state;
        }
        return state;
    }

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

    // ---- 장탄수 ----

    public static void SetAmmo(int uid, int current, int max)
    {
        var state = Ensure(uid);
        if (state == null) return;
        state.MaxAmmo = max;
        state.CurrentAmmo = Mathf.Clamp(current, 0, Mathf.Max(0, max));
        state.AmmoSet = true;
    }

    // 실제로 SetAmmo가 호출된 적 없으면 false (호출측이 총 기본값으로 폴백)
    // GetOrCreate 등이 내구도 조회를 위해 State를 미리 만들어둔 경우와 구분하기 위해 AmmoSet을 확인한다
    public static bool TryGetAmmo(int uid, out int current, out int max)
    {
        if (uid != 0 && _states.TryGetValue(uid, out var state) && state.AmmoSet)
        {
            current = state.CurrentAmmo;
            max = state.MaxAmmo;
            return true;
        }
        current = 0;
        max = 0;
        return false;
    }

    // ---- 파츠 ----

    public static void SetPart(int uid, SlotType slot, int partId)
    {
        var state = Ensure(uid);
        if (state == null) return;
        state.Parts[slot] = partId;
    }

    public static void RemovePart(int uid, SlotType slot)
    {
        if (uid != 0 && _states.TryGetValue(uid, out var state))
            state.Parts.Remove(slot);
    }

    // 전체 파츠 교체
    public static void SetParts(int uid, IReadOnlyDictionary<SlotType, int> parts)
    {
        var state = Ensure(uid);
        if (state == null) return;

        state.Parts.Clear();
        if (parts != null)
            foreach (var kv in parts)
                state.Parts[kv.Key] = kv.Value;
    }

    // 복원용 조회 — 없으면 빈 목록
    public static IReadOnlyDictionary<SlotType, int> GetParts(int uid) =>
        uid != 0 && _states.TryGetValue(uid, out var state) ? state.Parts : _emptyParts;

    public static void Clear() => _states.Clear();
}
