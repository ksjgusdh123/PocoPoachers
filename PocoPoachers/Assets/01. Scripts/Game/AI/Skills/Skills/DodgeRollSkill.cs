using UnityEngine;

// 공격자 반대 방향으로 일정 속도로 구르며 무적이 되는 스킬.
// 지속 시간은 구르기 애니메이션 클립 길이를 따오고, 못 구하면 duration(폴백) 사용.
// 수치는 skill.csv(SkillData)의 범용 컬럼에서 주입받는다: speed=구르기 속도, duration=폴백 지속시간, clip_name=구르기 클립.
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
        GameObject attacker = ctx.Attacker;

        // 공격자 쪽 방향(없으면 정면)으로 구르기 방향 결정
        _direction = attacker != null
            ? attacker.transform.position - self.position
            : self.forward;
        _direction.y = 0f;
        if (_direction.sqrMagnitude < 0.01f)
            _direction = self.forward;
        _direction.Normalize();

        ctx.Stat?.SetInvincible(true);

        // 지속 시간을 구르기 클립 길이로 설정 — 못 구하면 폴백(duration)
        _duration = GetRollClipLength(ctx.Animator);
        if (_duration <= 0f)
            _duration = Data.duration;

        // 반응형 구르기 신호 소비 (쿨다운은 SkillManager가 관리)
        if (_dodgeState == null)
            _dodgeState = ctx.Self.GetComponent<AIDodgeState>();
        _dodgeState?.ConsumeDodge();

        // 구르는 동안은 BT의 다른 이동 액션이 경로를 덮어쓰지 못하도록 경로 추종을 멈춤
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

    // 애니메이터 컨트롤러에서 구르기 클립을 이름으로 찾아 길이(초)를 반환. 못 찾으면 0
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
