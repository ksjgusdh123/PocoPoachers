using System;
using UnityEngine;

public static class PlayerSkillFactory
{
    public static IPlayerSkill Create(PlayerSkillData data)
    {
        if (data == null)
            return null;

        if (!Enum.TryParse(data.skill, true, out PlayerSkillId id))
        {
            Debug.LogWarning($"[PlayerSkillFactory] 알 수 없는 skill 값: '{data.skill}' (id={data.id})");
            return null;
        }

        switch (id)
        {
            case PlayerSkillId.Dash:
                return new DashSkill(data);
            case PlayerSkillId.InstantReload:
                return new InstantReloadSkill(data);
            case PlayerSkillId.InfiniteAmmo:
                return new InfiniteAmmoSkill(data);
            case PlayerSkillId.ExtraShot:
                return new ExtraShotSkill(data);
            case PlayerSkillId.ForceHeadshot:
                return new ForceHeadshotSkill(data);
            case PlayerSkillId.CritDamage:
                return new CritDamageSkill(data);
            case PlayerSkillId.RangeBoost:
                return new RangeBoostSkill(data);
            default:
                Debug.LogWarning($"[PlayerSkillFactory] 미구현 skill: {id} (id={data.id})");
                return null;
        }
    }
}
