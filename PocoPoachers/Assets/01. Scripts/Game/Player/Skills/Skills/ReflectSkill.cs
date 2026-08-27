using UnityEngine;

// duration 동안 무적 + 피격 총알 반사. InvincibleSkill과 거의 같은 구조지만 SetReflecting(true)도 함께 켜서
// Bullet.cs가 막힌 총알을 관통시키는 대신 역벡터로 반사시키게 한다(Bullet.ReflectOff 참고).
// 데미지 면역 판정용 패킷(G_Invincible/G_Reflecting)과 방어막 연출용 패킷(RoomSync.ReflectFx)은
// InvincibleSkill과 동일하게 별개 채널이다.
public class ReflectSkill : PlayerSkillBase
{
    private const string ShieldFxPrefabPath = "Skill/ShieldFXReflect";

    public override PlayerSkillId Id => PlayerSkillId.Reflect;

    private float _elapsed;
    private ShieldFxVisual _shieldFx;

    public ReflectSkill(PlayerSkillData data) : base(data) { }

    public override void Begin(PlayerSkillContext ctx)
    {
        _elapsed = 0f;
        ctx.Stat.SetInvincible(true);
        ctx.Stat.SetReflecting(true);

        _shieldFx = ShieldFxVisual.SpawnSelf(ctx.Transform, ctx.Stat, ShieldFxPrefabPath);
        RoomSync.ReflectFx(true);
    }

    public override bool Tick(PlayerSkillContext ctx)
    {
        _elapsed += Time.deltaTime;
        return _elapsed < Data.duration;
    }

    public override void End(PlayerSkillContext ctx)
    {
        if (ctx.Stat != null)
        {
            ctx.Stat.SetInvincible(false);
            ctx.Stat.SetReflecting(false);
        }

        // 즉시 파괴하지 않고 페이드아웃이 끝난 뒤 스스로 파괴하도록 맡긴다
        _shieldFx?.FadeOutAndDestroy();
        _shieldFx = null;
        RoomSync.ReflectFx(false);
    }
}
