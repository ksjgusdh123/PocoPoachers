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
                entries.Add(new SlotSaveEntry { slotIndex = i, itemId = slot.ItemData.id, amount = slot.Amount });
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
                inventory.AddItemAtSlot(entry.slotIndex, itemData, entry.amount);
        }
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
    }

    [Serializable]
    private class InventorySaveEntry
    {
        public string key;
        public List<SlotSaveEntry> slots = new List<SlotSaveEntry>();
    }

    [Serializable]
    private class GameSaveData
    {
        public string lastSavedAt;
        public List<InventorySaveEntry> inventories = new List<InventorySaveEntry>();

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
