// 크로스헤어가 가리키는 지면 지점(사거리 distance 제한)에 수류탄을 던진다.
// 던지는 순간 끝나는 즉발 스킬 — 폭발/피해 판정은 GrenadeProjectile이 맡고, 재사용은 쿨다운으로만 막는다.
public class GrenadeSkill : PlayerSkillBase
{
    public override PlayerSkillId Id => PlayerSkillId.Grenade;

    public GrenadeSkill(PlayerSkillData data) : base(data) { }

    public override void Begin(PlayerSkillContext ctx)
    {
        var target = ctx.AimGroundPoint(Data.distance);

        // 로컬 사본 — 호스트면 이 사본이 곧 권위 있는 폭발이고, 게스트면 연출용(피해는 호스트가 대신 넣는다)
        GrenadeProjectile.Launch(ctx.Transform.position, target, ctx.Self, Data, applyDamage: RoomManager.IsHost);
        RoomSync.GrenadeThrow(Data.id, ctx.Transform.position, target);
    }

    public override bool Tick(PlayerSkillContext ctx) => false;
}
