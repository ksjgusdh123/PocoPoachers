using UnityEngine;

// 공격자 반대 방향으로 구르며 무적. speed=속도, duration=폴백 지속시간, clip_name=구르기 클립.
public class DodgeRollSkill : SkillBase
{
    public override SkillId Id => SkillId.Dodge;

    private AIDodgeState _dodgeState;
    private Vector3 _direction;
    private float _elapsed;
    private float _duration;

    public DodgeRollSkill(SkillData data) : base(data) { }

    public override void Begin(SkillContext ctx)
    {
        Transform self = ctx.Self.transform;

        _direction = ComputeDirection(ctx, self);
        _direction.y = 0f;
        if (_direction.sqrMagnitude < 0.01f)
            _direction = self.forward;
        _direction.Normalize();

        ctx.Stat?.SetInvincible(true);

        // 지속 시간은 구르기 클립 길이, 못 구하면 폴백
        _duration = GetRollClipLength(ctx.Animator);
        if (_duration <= 0f)
            _duration = Data.duration;

        if (_dodgeState == null)
            _dodgeState = ctx.Self.GetComponent<AIDodgeState>();
        _dodgeState?.ConsumeDodge();

        // 다른 이동 액션이 경로를 덮어쓰지 못하도록 경로 추종을 멈춤
        ctx.Agent.isStopped = true;
        _elapsed = 0f;
    }

    public override bool Tick(SkillContext ctx)
    {
        _elapsed += Time.deltaTime;
        if (_elapsed >= _duration)
            return false;

        ctx.Agent.Move(_direction * Data.speed * Time.deltaTime);
        return true;
    }

    public override void End(SkillContext ctx)
    {
        ctx.Stat?.SetInvincible(false);
        if (ctx.Agent != null)
            ctx.Agent.isStopped = false;
    }

    // 후퇴 중이면 진행 방향(타겟 반대)으로, 평소엔 공격자 쪽으로 구른다
    private Vector3 ComputeDirection(SkillContext ctx, Transform self)
    {
        if (ctx.TryGetBlackboard(BlackboardKeys.IsRetreating, out bool retreating)
            && retreating && ctx.Target != null)
            return self.position - ctx.Target.transform.position;

        GameObject attacker = ctx.Attacker;
        return attacker != null
            ? attacker.transform.position - self.position
            : self.forward;
    }

    private float GetRollClipLength(Animator anim)
    {
        if (anim == null || anim.runtimeAnimatorController == null)
            return 0f;

        foreach (AnimationClip clip in anim.runtimeAnimatorController.animationClips)
            if (clip.name == Data.clip_name)
                return clip.length;

        return 0f;
    }
}
