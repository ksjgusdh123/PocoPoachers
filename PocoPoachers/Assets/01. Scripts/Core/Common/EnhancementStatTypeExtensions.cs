// 강화 스탯 이름을 강화 UI와 스킬 해금 안내가 함께 쓰기 위한 공용 표시명.
public static class EnhancementStatTypeExtensions
{
    public static string GetDisplayName(this EnhancementStatType statType)
    {
        string key = statType switch
        {
            EnhancementStatType.AttackPower => "enhancement.stat.attack_power",
            EnhancementStatType.MoveSpeed => "enhancement.stat.move_speed",
            EnhancementStatType.MaxHp => "enhancement.stat.max_hp",
            EnhancementStatType.DefenseRate => "enhancement.stat.defense_rate",
            EnhancementStatType.VisionRange => "enhancement.stat.vision_range",
            EnhancementStatType.AttackSpeed => "enhancement.stat.attack_speed",
            _ => null
        };

        return key == null ? statType.ToString() : LocalizationManager.GetInstance().GetString(key);
    }
}
