using UnityEngine;

// duration 동안 사격이 명중할 때마다 Data.power 확률로 데미지가 (1 + Data.radius)배로 오른다.
// 크리/사거리 배율과 같은 구조 — StatBase.LuckyShotChance/LuckyShotMultiplier를 세팅해두면
// StatSync를 타고 호스트까지 가고, 실제 확률 판정(Random 굴림)은 데미지를 넣는 호스트가
// Bullet.cs에서 공격자 스탯을 보고 직접 한다(ReflectSkill/InvincibleSkill과 달리 별도 토글 패킷은 없다).
public class LuckyShotSkill : PlayerSkillBase
{
    public override PlayerSkillId Id => PlayerSkillId.LuckyShot;

    private float _elapsed;

    public LuckyShotSkill(PlayerSkillData data) : base(data) { }

    public override bool CanUse(PlayerSkillContext ctx)
    {
        return base.CanUse(ctx) && ctx.Stat != null;
    }

    public override void Begin(PlayerSkillContext ctx)
    {
        _elapsed = 0f;
        Apply(ctx, Data.power, 1f + Data.radius);
    }

    public override bool Tick(PlayerSkillContext ctx)
    {
        _elapsed += Time.deltaTime;
        return _elapsed < Data.duration;
    }

    public override void End(PlayerSkillContext ctx)
    {
        Apply(ctx, StatBase.DefaultLuckyShotChance, StatBase.DefaultLuckyShotMultiplier);
    }

    private static void Apply(PlayerSkillContext ctx, float chance, float multiplier)
    {
        if (ctx.Stat == null) return;

        ctx.Stat.LuckyShotChance = chance;
        ctx.Stat.LuckyShotMultiplier = multiplier;
        ctx.Stat.SyncStatsNow();
    }
}
