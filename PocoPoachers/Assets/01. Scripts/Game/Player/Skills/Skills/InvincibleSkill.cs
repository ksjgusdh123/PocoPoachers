using UnityEngine;

// duration 동안 피해를 받지 않는다 (StatBase.SetInvincible → RoomSync로 데미지 면역 동기화까지 자동 처리됨).
// 방어막 연출(ShieldFX)은 별개 채널(RoomSync.ShieldFx)로 전파해 다른 클라이언트 화면에도 보이게 한다 —
// 데미지 면역 판정용 패킷과 섞으면 안 되므로(그건 호스트 판정용 편도 보고라 중계되지 않음) 나눴다.
public class InvincibleSkill : PlayerSkillBase
{
    private const string ShieldFxPrefabPath = "Skill/ShieldFX";

    public override PlayerSkillId Id => PlayerSkillId.Invincible;

    private float _elapsed;
    private ShieldFxVisual _shieldFx;

    public InvincibleSkill(PlayerSkillData data) : base(data) { }

    public override void Begin(PlayerSkillContext ctx)
    {
        _elapsed = 0f;
        ctx.Stat.SetInvincible(true);

        _shieldFx = ShieldFxVisual.SpawnSelf(ctx.Transform, ctx.Stat, ShieldFxPrefabPath);
        RoomSync.ShieldFx(true);
    }

    public override bool Tick(PlayerSkillContext ctx)
    {
        _elapsed += Time.deltaTime;
        return _elapsed < Data.duration;
    }

    public override void End(PlayerSkillContext ctx)
    {
        if (ctx.Stat != null) ctx.Stat.SetInvincible(false);

        // 즉시 파괴하지 않고 페이드아웃이 끝난 뒤 스스로 파괴하도록 맡긴다
        _shieldFx?.FadeOutAndDestroy();
        _shieldFx = null;
        RoomSync.ShieldFx(false);
    }
}
