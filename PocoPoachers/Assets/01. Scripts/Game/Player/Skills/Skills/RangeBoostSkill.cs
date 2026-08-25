using UnityEngine;

// duration 동안 탄환 사거리 배율을 power로 올린다.
//
// 크리 배율과 같은 경로다 — 사거리는 데미지를 넣는 클라(호스트)가 자기가 아는 총 스탯으로
// 다시 계산하므로, 배율이 StatSync를 타고 호스트의 RemotePlayerStat까지 가야 실제로 늘어난다.
public class RangeBoostSkill : PlayerSkillBase
{
    public override PlayerSkillId Id => PlayerSkillId.RangeBoost;

    private float _elapsed;

    public RangeBoostSkill(PlayerSkillData data) : base(data) { }

    public override bool CanUse(PlayerSkillContext ctx)
    {
        return base.CanUse(ctx) && ctx.Stat != null;
    }

    public override void Begin(PlayerSkillContext ctx)
    {
        _elapsed = 0f;
        Apply(ctx, Mathf.Max(StatBase.DefaultRangeMultiplier, Data.power));
    }

    public override bool Tick(PlayerSkillContext ctx)
    {
        _elapsed += Time.deltaTime;
        return _elapsed < Data.duration;
    }

    public override void End(PlayerSkillContext ctx)
    {
        Apply(ctx, StatBase.DefaultRangeMultiplier);
    }

    private static void Apply(PlayerSkillContext ctx, float multiplier)
    {
        if (ctx.Stat == null) return;

        ctx.Stat.RangeMultiplier = multiplier;
        ctx.Stat.SyncStatsNow();
    }
}
