using UnityEngine;

// duration 동안 크리티컬(헤드샷) 데미지 배율을 power로 올린다. 기본 배율은 2배.
//
// 배율은 방어율과 같은 캐릭터 스탯(StatBase.CritMultiplier)이라 StatSync를 타고 호스트까지 간다.
// 데미지를 넣는 건 호스트이므로, 호스트의 RemotePlayerStat에 반영되지 않으면 게스트에겐 효과가 없다.
// 주기 발신만 믿으면 한 주기만큼 늦게 걸리고 늦게 풀리므로 켤 때와 끌 때 즉시 보낸다.
public class CritDamageSkill : PlayerSkillBase
{
    public override PlayerSkillId Id => PlayerSkillId.CritDamage;

    private float _elapsed;

    public CritDamageSkill(PlayerSkillData data) : base(data) { }

    public override bool CanUse(PlayerSkillContext ctx)
    {
        return base.CanUse(ctx) && ctx.Stat != null;
    }

    public override void Begin(PlayerSkillContext ctx)
    {
        _elapsed = 0f;
        Apply(ctx, Mathf.Max(StatBase.DefaultCritMultiplier, Data.power));
    }

    public override bool Tick(PlayerSkillContext ctx)
    {
        _elapsed += Time.deltaTime;
        return _elapsed < Data.duration;
    }

    public override void End(PlayerSkillContext ctx)
    {
        Apply(ctx, StatBase.DefaultCritMultiplier);
    }

    private static void Apply(PlayerSkillContext ctx, float multiplier)
    {
        if (ctx.Stat == null) return;

        ctx.Stat.CritMultiplier = multiplier;
        ctx.Stat.SyncStatsNow();
    }
}
