using System.Collections.Generic;
using UnityEngine;

public class ShelterManager : Singleton<ShelterManager>
{
    public int CurrentLevel { get; private set; } = 1;

    protected override void Awake()
    {
        base.Awake();
        CurrentLevel = SaveManager.GetInstance().LoadShelterLevel();
    }

    public bool IsPlanetUnlocked(PlanetData planet)
    {
        return CurrentLevel >= planet.NeedShelterLevel;
    }

    // 창고 인벤토리에서 재료를 소비하고 레벨 업
    public bool TryUpgrade(Inventory storage)
    {
        var nextData = GetNextLevelData();
        if (nextData == null) return false;
        if (!HasRequiredItems(storage, nextData)) return false;

        ConsumeItems(storage, nextData);
        CurrentLevel = nextData.ShelterLevel;
        SaveManager.GetInstance().SaveShelterLevel(CurrentLevel);
        return true;
    }

    public ShelterData GetNextLevelData()
    {
        foreach (var data in ShelterTable.Instance.All)
            if (data.ShelterLevel == CurrentLevel + 1)
                return data;
        return null;
    }

    public bool HasRequiredItems(Inventory storage, ShelterData data)
    {
        return CheckItem(storage, data.NeedItem1Id, data.NeedItem1Count)
            && CheckItem(storage, data.NeedItem2Id, data.NeedItem2Count);
    }

    private bool CheckItem(Inventory storage, int itemId, int count)
    {
        if (itemId == 0) return true;
        if (storage == null) return false;
        var itemData = ItemTable.Instance.Get(itemId);
        return itemData != null && storage.GetItemCount(itemData) >= count;
    }

    private void ConsumeItems(Inventory storage, ShelterData data)
    {
        TryConsume(storage, data.NeedItem1Id, data.NeedItem1Count);
        TryConsume(storage, data.NeedItem2Id, data.NeedItem2Count);
    }

    private void TryConsume(Inventory storage, int itemId, int count)
    {
        if (itemId == 0) return;
        var itemData = ItemTable.Instance.Get(itemId);
        if (itemData != null) storage.RemoveItem(itemData, count);
    }
}
