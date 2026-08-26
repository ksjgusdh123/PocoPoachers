using UnityEngine;

// 크로스헤어가 가리키는 지면 지점(사거리 distance 제한)에 수류탄을 던진다.
// 던지는 순간 끝나는 즉발 스킬 — 폭발/피해 판정은 GrenadeProjectile이 맡고, 재사용은 쿨다운으로만 막는다.
public class GrenadeSkill : PlayerSkillBase
{
    // 플레이어 피벗(CharacterController 캡슐 중심)에 그대로 스폰하면 수류탄 콜라이더가 캡슐과
    // 겹친 채로 물리가 시작돼 서로 깊은 침투를 튕겨내며(depenetration) 플레이어가 붕 뜬다.
    // 손 위치쯤(전방·상방으로 살짝 띄운 지점)에서 던지도록 시작점을 캡슐 밖으로 옮긴다.
    private const float ThrowForwardOffset = 0.6f;
    private const float ThrowHeightOffset = 1.3f;

    public override PlayerSkillId Id => PlayerSkillId.Grenade;

    public GrenadeSkill(PlayerSkillData data) : base(data) { }

    public override void Begin(PlayerSkillContext ctx)
    {
        Vector3 target = ctx.AimGroundPoint(Data.distance);
        Vector3 origin = ctx.Transform.position
            + ctx.Transform.forward * ThrowForwardOffset
            + Vector3.up * ThrowHeightOffset;

        if (RoomManager.IsHost)
        {
            // 호스트 자신이 던진 수류탄 — 이 사본이 곧 실제 물리로 시뮬레이션되는 권위 사본이다
            int grenadeId = GrenadeProjectile.LaunchAuthoritative(origin, target, ctx.Self, Data);
            RoomSync.GrenadeSpawned(grenadeId, Data.id, origin, target);
        }
        else
        {
            // 게스트는 즉시 로컬 예측 사본을 띄우고(피해 없음), 호스트에 진짜 투척을 요청한다
            GrenadeProjectile.LaunchCosmetic(origin, target, ctx.Self, Data);
            RoomSync.RequestGrenadeThrow(Data.id, origin, target);
        }
    }

    public override bool Tick(PlayerSkillContext ctx) => false;
}
