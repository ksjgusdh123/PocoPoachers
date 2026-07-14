using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : Singleton<SaveManager>
{
    private string SlotPath(int slotIndex) =>
        Path.Combine(Application.persistentDataPath, $"save_{slotIndex}.json");

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
        data.lastSavedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
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
        data.lastSavedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        SaveSlotToDisk(_activeSlot);
    }

    public List<EquipSlotEntry> LoadEquipSlots() => GetOrLoad(_activeSlot).equipSlots;

    // 총기/방어구 등 uid별 인스턴스 상태(내구도/장탄수/파츠/강화)를 디스크에 저장 (호스트 전용)
    public void SaveEquipmentState()
    {
        if (!RoomManager.IsHost) return;
        var data = GetOrLoad(_activeSlot);
        data.equipment = WorldEquipmentManager.Export();
        data.lastSavedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
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
    private class SlotSaveEntry
    {
        public int slotIndex;
        public int itemId;
        public int amount;
        public int uid; // 스택 불가 아이템(무기/방어구)의 개체 식별자. 0이면 미배정(소모품 등)
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
        data.lastSavedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        SaveSlotToDisk(_activeSlot);
    }

    public int LoadShelterLevel() => GetOrLoad(_activeSlot).shelterLevel;

    [Serializable]
    private class GameSaveData
    {
        public string lastSavedAt;
        public int shelterLevel = 1;
        public List<InventorySaveEntry> inventories = new List<InventorySaveEntry>();
        public List<EquipSlotEntry> equipSlots = new List<EquipSlotEntry>();
        public WorldEquipmentManager.SaveData equipment = new WorldEquipmentManager.SaveData();

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
