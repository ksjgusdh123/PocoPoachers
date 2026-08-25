using UnityEngine;

// duration 동안 사격해도 탄창이 줄지 않는다.
//
// 멀티: 호스트는 G_Shoot을 인증하면서 자기 씬에 있는 게스트 총 사본의 탄약을 깎는다.
// 버프 중에는 게스트 쪽 탄약이 줄지 않으니 그대로 두면 사본만 바닥나 사격이 거부된다.
// 그래서 버프가 켜져 있는 동안 주기적으로 실제 탄약을 호스트에 보고해 사본을 되돌려 놓는다.
// (호스트/솔로에서는 SyncAmmoToHost가 아무것도 하지 않는다)
public class InfiniteAmmoSkill : PlayerSkillBase
{
    private const float HostSyncInterval = 0.5f;

    public override PlayerSkillId Id => PlayerSkillId.InfiniteAmmo;

    private float _elapsed;
    private float _syncTimer;

    public InfiniteAmmoSkill(PlayerSkillData data) : base(data) { }

    public override bool CanUse(PlayerSkillContext ctx)
    {
        return base.CanUse(ctx) && ctx.Weapon != null;
    }

    public override void Begin(PlayerSkillContext ctx)
    {
        _elapsed = 0f;
        _syncTimer = 0f;
        ctx.Weapon.InfiniteAmmo = true;
    }

    public override bool Tick(PlayerSkillContext ctx)
    {
        _elapsed += Time.deltaTime;

        _syncTimer -= Time.deltaTime;
        if (_syncTimer <= 0f)
        {
            _syncTimer = HostSyncInterval;
            ctx.Weapon.SyncAmmoToHost();
        }

        return _elapsed < Data.duration;
    }

    public override void End(PlayerSkillContext ctx)
    {
        if (ctx.Weapon == null) return;

        ctx.Weapon.InfiniteAmmo = false;
        ctx.Weapon.SyncAmmoToHost();   // 버프가 끝난 시점의 실제 탄약으로 사본을 맞춘다
    }
}
