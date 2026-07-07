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
        public readonly Dictionary<SlotType, int> Parts = new();     // 슬롯 -> 파츠 id
        public readonly Dictionary<SlotType, int> PartUids = new();  // 슬롯 -> 파츠 uid
        public int EnhancementLevel;
    }

    private static readonly Dictionary<int, State> _states = new();
    private static readonly Dictionary<SlotType, int> _emptyParts = new();
    // 인벤토리 아이템(uid=0)의 강화 레벨: itemId → level
    private static readonly Dictionary<int, int> _itemTypeEnhancementLevels = new();

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

    // 새로 발급된 uid의 초기 내구도를 미리 정해둔다 (예: 랜덤 내구도) — ItemSpawner.AssignItemUid에서 호출
    public static void SetInitialDurability(int uid, int itemId, float current, float max)
    {
        if (uid == 0) return;
        _states[uid] = new State { ItemId = itemId, Current = current, Max = max };
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

    public static void SetPart(int uid, SlotType slot, int partId, int partUid = 0)
    {
        var state = Ensure(uid);
        if (state == null) return;
        state.Parts[slot] = partId;
        if (partUid != 0)
            state.PartUids[slot] = partUid;
        else
            state.PartUids.Remove(slot);
    }

    public static void RemovePart(int uid, SlotType slot)
    {
        if (uid != 0 && _states.TryGetValue(uid, out var state))
        {
            state.Parts.Remove(slot);
            state.PartUids.Remove(slot);
        }
    }

    // 전체 파츠 교체
    public static void SetParts(int uid, IReadOnlyDictionary<SlotType, int> parts)
    {
        var state = Ensure(uid);
        if (state == null) return;

        state.Parts.Clear();
        state.PartUids.Clear();
        if (parts != null)
            foreach (var kv in parts)
                state.Parts[kv.Key] = kv.Value;
    }

    // 복원용 조회 — 없으면 빈 목록
    public static IReadOnlyDictionary<SlotType, int> GetParts(int uid) =>
        uid != 0 && _states.TryGetValue(uid, out var state) ? state.Parts : _emptyParts;

    public static int GetPartUid(int gunUid, SlotType slot)
    {
        if (gunUid != 0 && _states.TryGetValue(gunUid, out var state) && state.PartUids.TryGetValue(slot, out int partUid))
            return partUid;
        return 0;
    }

    // ---- 강화 레벨 ----

    // uid가 0(인벤토리 일반 아이템)일 때는 itemId 기반 딕셔너리에 저장
    public static void SetEnhancementLevel(int uid, int level, int itemId = 0)
    {
        if (uid != 0)
        {
            var state = Ensure(uid);
            if (state != null) state.EnhancementLevel = Mathf.Max(0, level);
        }
        else if (itemId > 0)
        {
            _itemTypeEnhancementLevels[itemId] = Mathf.Max(0, level);
        }
    }

    public static int GetEnhancementLevel(int uid, int itemId = 0)
    {
        if (uid != 0 && _states.TryGetValue(uid, out var state))
            return state.EnhancementLevel;
        if (itemId > 0 && _itemTypeEnhancementLevels.TryGetValue(itemId, out int typeLevel))
            return typeLevel;
        return 0;
    }

    // 1.0 기준 편차(효과 크기)를 강화 레벨만큼 증폭
    private static float EnhanceMultiplier(float baseValue, float bonusMultiplier)
        => 1f + (baseValue - 1f) * bonusMultiplier;

    public static ArmorStatData GetEnhancedArmorStat(int uid, ArmorStatData baseStat)
    {
        int level = GetEnhancementLevel(uid);
        if (level <= 0) return baseStat;

        float m = 1f + 0.1f * level;
        return new ArmorStatData
        {
            defense_rate = baseStat.DefenseRate * m,
            max_hp_bonus = baseStat.MaxHpBonus * m,
            move_speed_multiplier = EnhanceMultiplier(baseStat.MoveSpeedMultiplier, m),
        };
    }

    public static GunPartData GetEnhancedGunPart(GunPartData basePart, int partUid)
    {
        int level = GetEnhancementLevel(partUid, basePart.Id);
        if (level <= 0) return basePart;

        float m = 1f + 0.1f * level;
        return new GunPartData
        {
            id = basePart.Id,
            slot_type = basePart.SlotType,
            compatible_gun_types = basePart.CompatibleGunTypes,
            spread_multiplier = EnhanceMultiplier(basePart.SpreadMultiplier, m),
            aim_fov_multiplier = EnhanceMultiplier(basePart.AimFovMultiplier, m),
            reload_time_multiplier = EnhanceMultiplier(basePart.ReloadTimeMultiplier, m),
            recoil_multiplier = EnhanceMultiplier(basePart.RecoilMultiplier, m),
            max_magazine_bonus = Mathf.RoundToInt(basePart.MaxMagazineBonus * m),
            sound_range_multiplier = EnhanceMultiplier(basePart.SoundRangeMultiplier, m),
        };
    }

    public static bool TryGetDurability(int uid, out float current, out float max)
    {
        if (uid != 0 && _states.TryGetValue(uid, out var state))
        {
            current = state.Current;
            max = state.Max;
            return true;
        }
        current = 0f;
        max = 0f;
        return false;
    }

    public static void Clear()
    {
        _states.Clear();
        _itemTypeEnhancementLevels.Clear();
    }

    // ---- 영속화 (SaveManager가 디스크에 저장/복원) ----

    // 현재 uid별 상태 전체를 직렬화 가능한 형태로 내보낸다 (호스트 전용)
    public static SaveData Export()
    {
        var data = new SaveData();
        foreach (var kv in _states)
        {
            var s = kv.Value;
            var entry = new StateEntry
            {
                uid = kv.Key,
                itemId = s.ItemId,
                current = s.Current,
                max = s.Max,
                currentAmmo = s.CurrentAmmo,
                maxAmmo = s.MaxAmmo,
                ammoSet = s.AmmoSet,
                enhancementLevel = s.EnhancementLevel,
            };
            foreach (var p in s.Parts)
            {
                int partUid = s.PartUids.TryGetValue(p.Key, out var pu) ? pu : 0;
                entry.parts.Add(new PartEntry { slotType = (int)p.Key, partId = p.Value, partUid = partUid });
            }
            data.states.Add(entry);
        }
        foreach (var kv in _itemTypeEnhancementLevels)
            data.itemTypeEnhancements.Add(new TypeEnhancementEntry { itemId = kv.Key, level = kv.Value });
        return data;
    }

    // 저장 데이터로 상태를 통째로 교체 (게임 로드 시 1회, 게임플레이 시작 전에 호출)
    public static void Import(SaveData data)
    {
        _states.Clear();
        _itemTypeEnhancementLevels.Clear();
        if (data == null) return;

        if (data.states != null)
            foreach (var e in data.states)
            {
                var state = new State
                {
                    ItemId = e.itemId,
                    Current = e.current,
                    Max = e.max,
                    CurrentAmmo = e.currentAmmo,
                    MaxAmmo = e.maxAmmo,
                    AmmoSet = e.ammoSet,
                    EnhancementLevel = e.enhancementLevel,
                };
                if (e.parts != null)
                    foreach (var p in e.parts)
                    {
                        state.Parts[(SlotType)p.slotType] = p.partId;
                        if (p.partUid != 0)
                            state.PartUids[(SlotType)p.slotType] = p.partUid;
                    }
                _states[e.uid] = state;
            }

        if (data.itemTypeEnhancements != null)
            foreach (var t in data.itemTypeEnhancements)
                _itemTypeEnhancementLevels[t.itemId] = t.level;
    }

    // 발급된 uid 중 최댓값 (로드 후 uid 카운터를 이보다 크게 시드해 충돌 방지)
    public static int MaxUid()
    {
        int max = 0;
        foreach (var uid in _states.Keys)
            if (uid > max) max = uid;
        return max;
    }

    [System.Serializable]
    public class SaveData
    {
        public List<StateEntry> states = new();
        public List<TypeEnhancementEntry> itemTypeEnhancements = new();
    }

    [System.Serializable]
    public class StateEntry
    {
        public int uid;
        public int itemId;
        public float current;
        public float max;
        public int currentAmmo;
        public int maxAmmo;
        public bool ammoSet;
        public int enhancementLevel;
        public List<PartEntry> parts = new();
    }

    [System.Serializable]
    public class PartEntry
    {
        public int slotType;  // (int)SlotType
        public int partId;
        public int partUid;
    }

    [System.Serializable]
    public class TypeEnhancementEntry
    {
        public int itemId;
        public int level;
    }
}
