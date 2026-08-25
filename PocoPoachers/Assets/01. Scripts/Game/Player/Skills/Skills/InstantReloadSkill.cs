using UnityEngine;

// duration 동안 재장전이 대기 없이 곧바로 끝난다.
// 발동 순간에 장전하는 게 아니라, 버프가 켜져 있는 동안 플레이어가 재장전할 때마다 적용된다.
// 수동 재장전(R)과 탄창 소진 시 자동 재장전이 모두 WeaponController의 같은 진입점을 지나므로
// 거기 플래그 하나로 양쪽이 함께 처리된다.
public class InstantReloadSkill : PlayerSkillBase
{
    public override PlayerSkillId Id => PlayerSkillId.InstantReload;

    private float _elapsed;

    public InstantReloadSkill(PlayerSkillData data) : base(data) { }

    public override bool CanUse(PlayerSkillContext ctx)
    {
        return base.CanUse(ctx) && ctx.Weapon != null;
    }

    public override void Begin(PlayerSkillContext ctx)
    {
        _elapsed = 0f;
        ctx.Weapon.InstantReloadBuff = true;
    }

    public override bool Tick(PlayerSkillContext ctx)
    {
        _elapsed += Time.deltaTime;
        return _elapsed < Data.duration;
    }

    public override void End(PlayerSkillContext ctx)
    {
        if (ctx.Weapon != null) ctx.Weapon.InstantReloadBuff = false;
    }
}
