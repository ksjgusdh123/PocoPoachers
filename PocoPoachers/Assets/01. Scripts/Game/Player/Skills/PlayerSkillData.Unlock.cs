using System;
using UnityEngine;

// player_skill.csv의 해금 조건(unlock_stat / unlock_level)과 획득 재료(need_item_id / need_item_count) 해석.
// 해금은 스탯 강화 레벨로 열리고, 열린 뒤 재료를 소모해야 실제로 획득해 장착할 수 있다.
// 각각 값이 비어 있거나 0 이하면 그 조건이 없는 것으로 본다.
public partial class PlayerSkillData
{
    public bool TryGetUnlockCondition(out EnhancementStatType statType, out int requiredLevel)
    {
        statType = default;
        requiredLevel = 0;

        if (string.IsNullOrWhiteSpace(unlock_stat) || unlock_level <= 0) return false;

        if (!Enum.TryParse(unlock_stat.Trim(), true, out statType))
        {
            Debug.LogWarning($"[PlayerSkillData] player_skill.csv의 unlock_stat을 해석할 수 없습니다: {unlock_stat} (id {id})");
            return false;
        }

        requiredLevel = unlock_level;
        return true;
    }

    // 재료가 필요 없는 스킬(대시 등)은 false — 해금되면 곧바로 보유 상태가 된다.
    public bool TryGetCost(out ItemData item, out int count)
    {
        item = null;
        count = 0;

        if (need_item_id <= 0 || need_item_count <= 0) return false;

        item = DataManager.GetItem(need_item_id);
        if (item == null)
        {
            Debug.LogWarning($"[PlayerSkillData] item.csv에 없는 need_item_id: {need_item_id} (id {id})");
            return false;
        }

        count = need_item_count;
        return true;
    }

    // 패시브 스킬인지 — 장착만으로 해당 스탯에 보너스를 얹는다.
    // 수치 단위는 그 스탯의 성장 방식을 따른다(공격력/공격속도는 배율이라 0.15 = +15%, 체력 등은 가산이라 30 = +30).
    public bool TryGetPassive(out EnhancementStatType statType, out float value)
    {
        statType = default;
        value = 0f;

        if (string.IsNullOrWhiteSpace(passive_stat) || Mathf.Approximately(passive_value, 0f)) return false;

        if (!Enum.TryParse(passive_stat.Trim(), true, out statType))
        {
            Debug.LogWarning($"[PlayerSkillData] player_skill.csv의 passive_stat을 해석할 수 없습니다: {passive_stat} (id {id})");
            return false;
        }

        value = passive_value;
        return true;
    }

    // 해금 조건이 없으면 빈 문자열.
    public string GetUnlockHint()
    {
        if (!TryGetUnlockCondition(out EnhancementStatType statType, out int requiredLevel)) return "";

        return string.Format(
            LocalizationManager.GetInstance().GetString("skill.unlock_hint"),
            statType.GetDisplayName(),
            requiredLevel);
    }
}
