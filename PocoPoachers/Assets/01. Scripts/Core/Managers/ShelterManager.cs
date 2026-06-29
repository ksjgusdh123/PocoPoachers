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

    // 플레이어 인벤 + 창고 재료를 합산해 소비 후 레벨 업 (플레이어 우선 차감)
    public bool TryUpgrade(Inventory player, Inventory storage)
    {
        var nextData = GetNextLevelData();
        if (nextData == null) return false;
        if (!HasRequiredItems(player, storage, nextData)) return false;

        ConsumeItems(player, storage, nextData);
        ApplyLevel(nextData.ShelterLevel);
        RoomSync.ShelterLevel(nextData.ShelterLevel);
        return true;
    }

    public bool ForceUpgradeLevel()
    {
        var nextData = GetNextLevelData();
        if (nextData == null) return false;

        ApplyLevel(nextData.ShelterLevel);
        RoomSync.ShelterLevel(nextData.ShelterLevel);
        return true;
    }

    public void SetLevel(int level)
    {
        ApplyLevel(Mathf.Max(1, level));
    }

    void ApplyLevel(int level)
    {
        CurrentLevel = level;
        SaveManager.GetInstance().SaveShelterLevel(CurrentLevel);
    }

    public ShelterData GetNextLevelData()
    {
        foreach (var data in ShelterTable.Instance.All)
            if (data.ShelterLevel == CurrentLevel + 1)
                return data;
        return null;
    }

    public bool HasRequiredItems(Inventory player, Inventory storage, ShelterData data)
    {
        return CheckItem(player, storage, data.NeedItem1Id, data.NeedItem1Count)
            && CheckItem(player, storage, data.NeedItem2Id, data.NeedItem2Count);
    }

    public int GetCombinedItemCount(Inventory player, Inventory storage, int itemId)
    {
        if (itemId == 0) return 0;

        var itemData = ItemTable.Instance.Get(itemId);
        if (itemData == null) return 0;

        int count = 0;
        if (player != null) count += player.GetItemCount(itemData);
        if (storage != null) count += storage.GetItemCount(itemData);
        return count;
    }

    private bool CheckItem(Inventory player, Inventory storage, int itemId, int count)
    {
        if (itemId == 0) return true;
        return GetCombinedItemCount(player, storage, itemId) >= count;
    }

    private void ConsumeItems(Inventory player, Inventory storage, ShelterData data)
    {
        TryConsume(player, storage, data.NeedItem1Id, data.NeedItem1Count);
        TryConsume(player, storage, data.NeedItem2Id, data.NeedItem2Count);
    }

    private void TryConsume(Inventory player, Inventory storage, int itemId, int count)
    {
        if (itemId == 0) return;

        var itemData = ItemTable.Instance.Get(itemId);
        if (itemData == null) return;

        int remaining = count;
        if (player != null)
            remaining -= player.RemoveItem(itemData, remaining);

        if (remaining > 0 && storage != null)
            storage.RemoveItem(itemData, remaining);
    }
}
