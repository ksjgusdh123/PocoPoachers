// item.csv의 Weapon/Armor 항목과 gun_stat.csv/armor_stat.csv는 id가 1:1로 매핑됩니다.
// (예: item id 201 == gun_stat id 201)
public static class DataManager
{
    public static ItemData GetItem(int itemId) => ItemTable.Instance.Get(itemId);

    public static GunStatData GetGunStat(int id) => GunStatTable.Instance.Get(id);
    public static ArmorStatData GetArmorStat(int id) => ArmorStatTable.Instance.Get(id);

    public static GunStatData GetGunStatByItemId(int itemId) => GetGunStat(itemId);
    public static ArmorStatData GetArmorStatByItemId(int itemId) => GetArmorStat(itemId);
}
