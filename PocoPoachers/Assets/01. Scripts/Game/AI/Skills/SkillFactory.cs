using System;
using UnityEngine;

// SkillData의 skill 문자열을 SkillId로 파싱해 알맞은 ISkill을 생성한다.
public static class SkillFactory
{
    public static ISkill Create(SkillData data)
    {
        if (data == null)
            return null;

        if (!Enum.TryParse(data.skill, true, out SkillId id))
        {
            Debug.LogWarning($"[SkillFactory] 알 수 없는 skill 값: '{data.skill}' (id={data.id})");
            return null;
        }

        switch (id)
        {
            case SkillId.Dodge:
                return new DodgeRollSkill(data);
            case SkillId.Retreat:
                return new RetreatSkill(data);
            case SkillId.Heal:
                return new HealSkill(data);
            default:
                Debug.LogWarning($"[SkillFactory] 미구현 skill: {id} (id={data.id})");
                return null;
        }
    }
}
