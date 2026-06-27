public class ArmorBase : EquippableItemBase
{
    private const float DefaultMaxDurability = 100f;

    public ArmorStatData Stat => DataManager.GetArmorStat(_itemId);

    // uid가 아직 없는 임시 스폰(예: 구버전 호출부) 호환용
    public void SetItemId(int itemId) => Initialize(_uid, itemId, _maxDurability > 0f ? _maxDurability : DefaultMaxDurability);

    public void DecreaseDurability(float damage) => SetDurability(_currentDurability - damage / 10f);
}
