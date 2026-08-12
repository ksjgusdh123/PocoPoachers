using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : Singleton<SaveManager>
{
    private string SlotPath(int slotIndex) =>
        Path.Combine(Application.persistentDataPath, $"save_{slotIndex}.json");

    private static string NowTimestamp() => DateTime.Now.ToString("yyyy-MM-dd HH:mm");

    private readonly Dictionary<int, GameSaveData> _cache = new();
    private int _activeSlot;

    protected override void Awake()
    {
        base.Awake();
    }

    public void SetActiveSlot(int slotIndex) => _activeSlot = slotIndex;
    public int ActiveSlot => _activeSlot;

    // 저장된 슬롯 인덱스 목록 (오름차순)
    public List<int> GetAllSlotIndices()
    {
        var indices = new List<int>();
        foreach (string path in Directory.GetFiles(Application.persistentDataPath, "save_*.json"))
        {
            string name = Path.GetFileNameWithoutExtension(path); // "save_0"
            if (int.TryParse(name.Substring(5), out int idx))
                indices.Add(idx);
        }
        indices.Sort();
        return indices;
    }

    // 새 게임 시작 시 호출 — 다음 빈 슬롯 인덱스를 activeSlot으로 확보
    public void AllocateNewSlot()
    {
        var existing = GetAllSlotIndices();
        _activeSlot = existing.Count > 0 ? existing[existing.Count - 1] + 1 : 0;
    }

    public void SaveInventory(string key, Inventory inventory)
    {
        var data = GetOrLoad(_activeSlot);
        var entries = new List<SlotSaveEntry>();
        for (int i = 0; i < inventory.CurrentCapacity; i++)
        {
            var slot = inventory.Slots[i];
            if (!slot.IsEmpty)
                entries.Add(new SlotSaveEntry { slotIndex = i, itemId = slot.ItemData.id, amount = slot.Amount, uid = slot.Uid });
        }
        data.SetInventory(key, entries);
        data.lastSavedAt = NowTimestamp();
        SaveSlotToDisk(_activeSlot);
    }

    public void LoadInventory(string key, Inventory inventory)
    {
        var entries = GetOrLoad(_activeSlot).GetInventory(key);
        if (entries == null) return;

        foreach (var entry in entries)
        {
            var itemData = ItemTable.Instance.Get(entry.itemId);
            if (itemData != null)
                inventory.AddItemAtSlot(entry.slotIndex, itemData, entry.amount, entry.uid);
        }
    }

    // 장착 슬롯(무기/방어구/가방) 구성 저장 — 어떤 아이템(itemId/uid)이 몇 번 슬롯에 장착됐는지.
    // uid로 WorldEquipmentManager의 내구도/장탄수/파츠와 연결된다. 인벤토리와 동일하게 게스트도 로컬 저장.
    public void SaveEquipSlots(List<EquipSlotEntry> slots)
    {
        var data = GetOrLoad(_activeSlot);
        data.equipSlots = slots ?? new List<EquipSlotEntry>();
        data.lastSavedAt = NowTimestamp();
        SaveSlotToDisk(_activeSlot);
    }

    public List<EquipSlotEntry> LoadEquipSlots() => GetOrLoad(_activeSlot).equipSlots;

    // 퀵슬롯에 올려둔 아이템은 인벤토리에서 빠져나와 있어 SaveInventory에 안 잡히므로 따로 저장한다
    public void SaveQuickSlots(List<SlotSaveEntry> slots)
    {
        var data = GetOrLoad(_activeSlot);
        data.quickSlots = slots ?? new List<SlotSaveEntry>();
        data.lastSavedAt = NowTimestamp();
        SaveSlotToDisk(_activeSlot);
    }

    public List<SlotSaveEntry> LoadQuickSlots() => GetOrLoad(_activeSlot).quickSlots;

    // 총기/방어구 등 uid별 인스턴스 상태(내구도/장탄수/파츠/강화)를 디스크에 저장 (호스트 전용)
    public void SaveEquipmentState()
    {
        if (!RoomManager.IsHost) return;
        var data = GetOrLoad(_activeSlot);
        data.equipment = WorldEquipmentManager.Export();
        data.lastSavedAt = NowTimestamp();
        SaveSlotToDisk(_activeSlot);
    }

    // 저장된 장비 상태를 WorldEquipmentManager로 복원하고, uid 카운터를 최댓값 다음으로 시드한다.
    // 게임 로드 시 게임플레이 시작 전에 1회 호출 (인벤토리 로드보다 먼저일 필요는 없으나, 장착 복원 전에 완료돼야 함)
    public void LoadEquipmentState()
    {
        var data = GetOrLoad(_activeSlot);
        WorldEquipmentManager.Import(data.equipment);
        ItemSpawner.SeedItemUid(WorldEquipmentManager.MaxUid());
    }

    // 퀘스트 진행 상태 저장 - 쉘터 레벨과 마찬가지로 파티 공유 값이라 호스트만 저장한다
    public void SaveQuestState()
    {
        if (!RoomManager.IsHost) return;
        var data = GetOrLoad(_activeSlot);
        data.questProgress = QuestManager.Export();
        data.lastSavedAt = NowTimestamp();
        SaveSlotToDisk(_activeSlot);
    }

    // 저장된 퀘스트 진행 상태를 QuestManager로 복원. 게임 로드 시 1회 호출.
    public void LoadQuestState()
    {
        var data = GetOrLoad(_activeSlot);
        QuestManager.Import(data.questProgress);
    }

    // 활력치(체력/스태미나/배터리) 저장 — 맵 전환 시 유지되도록 로컬에 영속화. 인벤/장비와 동일하게 게스트도 로컬 저장.
    public void SaveVitals(float hp, float stamina, float battery)
    {
        var data = GetOrLoad(_activeSlot);
        data.hasVitals = true;
        data.hp = hp;
        data.stamina = stamina;
        data.battery = battery;
        data.lastSavedAt = NowTimestamp();
        SaveSlotToDisk(_activeSlot);
    }

    // 사망 등으로 저장값을 버리고 다음 스폰을 최대치로 시작시키고 싶을 때 호출
    public void ClearVitals()
    {
        var data = GetOrLoad(_activeSlot);
        if (!data.hasVitals) return;
        data.hasVitals = false;
        SaveSlotToDisk(_activeSlot);
    }

    // 저장된 활력치가 있으면 true. 없으면(새 게임 등) false — 호출측이 최대치로 폴백한다.
    public bool TryLoadVitals(out float hp, out float stamina, out float battery)
    {
        var data = GetOrLoad(_activeSlot);
        hp = data.hp;
        stamina = data.stamina;
        battery = data.battery;
        return data.hasVitals;
    }

    // 캐릭터 생성에서 정한 닉네임 — 슬롯당 하나. 저장과 동시에 lastSavedAt이 찍혀 슬롯 목록에 나타난다.
    public void SaveNickname(string nickname)
    {
        var data = GetOrLoad(_activeSlot);
        data.nickname = nickname;
        data.lastSavedAt = NowTimestamp();
        SaveSlotToDisk(_activeSlot);
    }

    public string LoadNickname() => GetOrLoad(_activeSlot).nickname;

    public string GetNickname(int slotIndex) => GetOrLoad(slotIndex).nickname;

    public bool HasSave(int slotIndex) =>
        !string.IsNullOrEmpty(GetOrLoad(slotIndex).lastSavedAt);

    public void DeleteSlot(int slotIndex)
    {
        _cache.Remove(slotIndex);
        string path = SlotPath(slotIndex);
        if (File.Exists(path))
            File.Delete(path);
    }

    public string GetLastSavedAt(int slotIndex) => GetOrLoad(slotIndex).lastSavedAt;

    public int GetSavedItemCount(int slotIndex, string key) =>
        GetOrLoad(slotIndex).GetInventory(key)?.Count ?? 0;

    private GameSaveData GetOrLoad(int slotIndex)
    {
        if (_cache.TryGetValue(slotIndex, out var cached)) return cached;

        string path = SlotPath(slotIndex);
        GameSaveData data;
        if (!File.Exists(path))
        {
            data = new GameSaveData();
        }
        else
        {
            try { data = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(path)) ?? new GameSaveData(); }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] 슬롯 {slotIndex} 로드 실패: {e.Message}");
                data = new GameSaveData();
            }
        }
        _cache[slotIndex] = data;
        return data;
    }

    private void SaveSlotToDisk(int slotIndex)
    {
        try { File.WriteAllText(SlotPath(slotIndex), JsonUtility.ToJson(_cache[slotIndex], true)); }
        catch (Exception e) { Debug.LogError($"[SaveManager] 슬롯 {slotIndex} 저장 실패: {e.Message}"); }
    }

    [Serializable]
    public class SlotSaveEntry
    {
        public int slotIndex;
        public int itemId;
        public int amount;
        public int uid; // 스택 불가 아이템(무기/방어구)의 개체 식별자. 0이면 미배정(소모품 등)
    }

    // 방 세계(호스트 소유)에 보관하는 게스트별 상태 — playerId로 구분.
    // 코옵 게스트는 개인 세이브 대신 이걸로 복원된다. (uid 충돌 없음: 게스트는 빈손 시작해 호스트 월드에서만 물건을 얻음)
    [Serializable]
    public class GuestRoomState
    {
        public int playerId;
        public List<SlotSaveEntry> inventory = new List<SlotSaveEntry>();
        public List<EquipSlotEntry> equipSlots = new List<EquipSlotEntry>();
        public List<SlotSaveEntry> quickSlots = new List<SlotSaveEntry>();
        public WorldEquipmentManager.SaveData equipment = new WorldEquipmentManager.SaveData();

        // 게스트가 직접 올린 스냅샷인지. false면 GuestInventoryTracker(상자 교환 검증용 부분 미러)에서
        // 긁어낸 부정확한 값이라, 진짜 스냅샷이 한 번이라도 오면 덮어써야 한다.
        public bool fromGuestSnapshot;
    }

    [Serializable]
    private class InventorySaveEntry
    {
        public string key;
        public List<SlotSaveEntry> slots = new List<SlotSaveEntry>();
    }

    [Serializable]
    public class EquipSlotEntry
    {
        public int slotIndex; // 장착 UI 슬롯 인덱스 (무기 0~1, 방어구 2~3, 가방 4)
        public int itemId;
        public int uid; // WorldEquipmentManager의 인스턴스 상태(내구도/장탄수/파츠)와 연결
    }

    public void SaveShelterLevel(int level)
    {
        var data = GetOrLoad(_activeSlot);
        data.shelterLevel = level;
        data.lastSavedAt = NowTimestamp();
        SaveSlotToDisk(_activeSlot);
    }

    public int LoadShelterLevel() => GetOrLoad(_activeSlot).shelterLevel;

    // 호스트 전용 — 방 세계에 게스트 상태를 playerId 기준으로 갱신 저장한다.
    public void SaveGuestRoomState(GuestRoomState state)
    {
        if (!RoomManager.IsHost || state == null) return;
        var data = GetOrLoad(_activeSlot);
        int idx = data.guestStates.FindIndex(g => g.playerId == state.playerId);
        if (idx >= 0) data.guestStates[idx] = state;
        else data.guestStates.Add(state);
        data.lastSavedAt = NowTimestamp();
        SaveSlotToDisk(_activeSlot);
    }

    // 접속한 게스트의 저장된 방 상태 (없으면 null → 빈손 시작)
    public GuestRoomState LoadGuestRoomState(int playerId)
        => GetOrLoad(_activeSlot).guestStates.Find(g => g.playerId == playerId);

    [Serializable]
    private class GameSaveData
    {
        public string lastSavedAt;
        public string nickname;
        public int shelterLevel = 1;
        public List<InventorySaveEntry> inventories = new List<InventorySaveEntry>();
        public List<EquipSlotEntry> equipSlots = new List<EquipSlotEntry>();
        public List<SlotSaveEntry> quickSlots = new List<SlotSaveEntry>();
        public WorldEquipmentManager.SaveData equipment = new WorldEquipmentManager.SaveData();

        // 퀘스트 진행 상태 - 쉘터 레벨처럼 파티 전체가 공유하는 값이라 게스트별(GuestRoomState)이 아니라 여기 한 곳에만 둔다
        public QuestManager.SaveData questProgress = new QuestManager.SaveData();

        // 방 세계에 참여한 게스트들의 상태 (호스트 세이브 = 방 세계이므로 여기 함께 보관)
        public List<GuestRoomState> guestStates = new List<GuestRoomState>();

        // 활력치 — hasVitals가 false면 미저장(새 게임)이라 최대치로 시작
        public bool hasVitals;
        public float hp;
        public float stamina;
        public float battery;

        public void SetInventory(string key, List<SlotSaveEntry> entries)
        {
            var entry = inventories.Find(e => e.key == key);
            if (entry != null) entry.slots = entries;
            else inventories.Add(new InventorySaveEntry { key = key, slots = entries });
        }

        public List<SlotSaveEntry> GetInventory(string key) =>
            inventories.Find(e => e.key == key)?.slots;
    }
}
