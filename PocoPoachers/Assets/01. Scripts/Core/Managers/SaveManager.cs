using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : Singleton<SaveManager>
{
    private const string SaveFileName = "save.json";
    private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    private GameSaveData _data;

    protected override void Awake()
    {
        base.Awake();
        LoadFromDisk();
    }

    public void SaveInventory(string key, Inventory inventory)
    {
        var slots = new List<SlotSaveEntry>();
        for (int i = 0; i < inventory.CurrentCapacity; i++)
        {
            var slot = inventory.Slots[i];
            if (!slot.IsEmpty)
                slots.Add(new SlotSaveEntry { slotIndex = i, itemId = slot.ItemData.id, amount = slot.Amount });
        }
        _data.SetInventory(key, slots);
        SaveToDisk();
    }

    public void LoadInventory(string key, Inventory inventory)
    {
        var slots = _data.GetInventory(key);
        if (slots == null) return;

        foreach (var entry in slots)
        {
            var itemData = ItemTable.Instance.Get(entry.itemId);
            if (itemData != null)
                inventory.AddItemAtSlot(entry.slotIndex, itemData, entry.amount);
        }
    }

    private void LoadFromDisk()
    {
        if (!File.Exists(SavePath))
        {
            _data = new GameSaveData();
            return;
        }

        try
        {
            _data = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(SavePath)) ?? new GameSaveData();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] 세이브 파일 로드 실패: {e.Message}");
            _data = new GameSaveData();
        }
    }

    private void SaveToDisk()
    {
        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(_data, true));
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] 세이브 실패: {e.Message}");
        }
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
        public List<InventorySaveEntry> inventories = new List<InventorySaveEntry>();

        public void SetInventory(string key, List<SlotSaveEntry> slots)
        {
            var entry = inventories.Find(e => e.key == key);
            if (entry != null)
                entry.slots = slots;
            else
                inventories.Add(new InventorySaveEntry { key = key, slots = slots });
        }

        public List<SlotSaveEntry> GetInventory(string key)
        {
            return inventories.Find(e => e.key == key)?.slots;
        }
    }
}
