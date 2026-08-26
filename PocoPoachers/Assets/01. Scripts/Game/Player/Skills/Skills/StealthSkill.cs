using UnityEngine;

// duration 동안 몸을 반투명하게 만들고 적 AI 탐지 후보에서 제외한다(StatBase.IsStealthed → TargetDetector).
// 무기를 발사하면(GunBase.OnShoot) 그 즉시 은신이 풀린다 — 총을 바꿔도 새 총의 발사를 잡도록 매 Tick 재구독한다.
public class StealthSkill : PlayerSkillBase
{
    public override PlayerSkillId Id => PlayerSkillId.Stealth;

    private float _elapsed;
    private bool _broken;
    private GunBase _subscribedGun;

    public StealthSkill(PlayerSkillData data) : base(data) { }

    public override bool CanUse(PlayerSkillContext ctx) => base.CanUse(ctx) && ctx.Stat != null;

    public override void Begin(PlayerSkillContext ctx)
    {
        _elapsed = 0f;
        _broken = false;

        ctx.Stat.IsStealthed = true;
        StealthVisual.SetActiveForSelf(ctx.Self, true, Data.power);
        RoomSync.Stealth(true, Data.power);

        SubscribeToGun(ctx);
    }

    public override bool Tick(PlayerSkillContext ctx)
    {
        SubscribeToGun(ctx); // 은신 중 무기를 바꿔도 새 총의 발사에 반응하도록 갱신
        _elapsed += Time.deltaTime;
        return !_broken && _elapsed < Data.duration;
    }

    public override void End(PlayerSkillContext ctx)
    {
        UnsubscribeGun();

        if (ctx.Stat != null) ctx.Stat.IsStealthed = false;
        StealthVisual.SetActiveForSelf(ctx.Self, false, Data.power);
        RoomSync.Stealth(false, Data.power);
    }

    private void SubscribeToGun(PlayerSkillContext ctx)
    {
        GunBase current = ctx.Weapon != null ? ctx.Weapon.CurrentGun : null;
        if (current == _subscribedGun) return;

        UnsubscribeGun();
        _subscribedGun = current;
        if (_subscribedGun != null)
            _subscribedGun.OnShoot += HandleShoot;
    }

    private void UnsubscribeGun()
    {
        if (_subscribedGun != null)
            _subscribedGun.OnShoot -= HandleShoot;
        _subscribedGun = null;
    }

    private void HandleShoot(Vector2 kick) => _broken = true;
}
