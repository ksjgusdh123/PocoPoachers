using UnityEngine;

// duration 동안 스태미나가 소모되지 않는다(회복은 그대로 동작).
// 다른 클라 판정에 쓰이는 값이 아니라 네트워크 동기화가 필요 없다(InfiniteAmmo와 달리 호스트 사본이 없음).
public class InfiniteStaminaSkill : PlayerSkillBase
{
    public override PlayerSkillId Id => PlayerSkillId.InfiniteStamina;

    private float _elapsed;

    public InfiniteStaminaSkill(PlayerSkillData data) : base(data) { }

    public override void Begin(PlayerSkillContext ctx)
    {
        _elapsed = 0f;
        ctx.Stat.HasInfiniteStamina = true;
    }

    public override bool Tick(PlayerSkillContext ctx)
    {
        _elapsed += Time.deltaTime;
        return _elapsed < Data.duration;
    }

    public override void End(PlayerSkillContext ctx)
    {
        if (ctx.Stat != null) ctx.Stat.HasInfiniteStamina = false;
    }
}
