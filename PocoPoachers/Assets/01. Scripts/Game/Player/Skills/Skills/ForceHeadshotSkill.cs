using UnityEngine;

// duration 동안 모든 사격이 헤드샷으로 판정된다.
//
// 헤드샷 여부는 원래부터 쏜 클라가 정해서 G_Shoot의 is_headshot으로 보내고
// 호스트가 그대로 신뢰하는 구조라, 이 버프는 네트워크에 아무것도 추가하지 않는다.
// 로컬 판정만 바꾸면 데미지·히트마커·다른 게스트 연출까지 그대로 따라온다.
public class ForceHeadshotSkill : PlayerSkillBase
{
    public override PlayerSkillId Id => PlayerSkillId.ForceHeadshot;

    private float _elapsed;

    public ForceHeadshotSkill(PlayerSkillData data) : base(data) { }

    public override bool CanUse(PlayerSkillContext ctx)
    {
        return base.CanUse(ctx) && ctx.Weapon != null;
    }

    public override void Begin(PlayerSkillContext ctx)
    {
        _elapsed = 0f;
        ctx.Weapon.ForceHeadshot = true;
    }

    public override bool Tick(PlayerSkillContext ctx)
    {
        _elapsed += Time.deltaTime;
        return _elapsed < Data.duration;
    }

    public override void End(PlayerSkillContext ctx)
    {
        if (ctx.Weapon != null) ctx.Weapon.ForceHeadshot = false;
    }
}
