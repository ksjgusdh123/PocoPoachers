using UnityEngine;

// 이동 입력 방향(없으면 전방)으로 duration 동안 speed로 미끄러진다. 무적 없음.
public class DashSkill : PlayerSkillBase
{
    public override PlayerSkillId Id => PlayerSkillId.Dash;
    public override bool LocksMovement => true;

    private Vector3 _direction;
    private float _elapsed;

    public DashSkill(PlayerSkillData data) : base(data) { }

    public override void Begin(PlayerSkillContext ctx)
    {
        _direction = ctx.MoveDirectionOrForward();
        _direction.y = 0f;
        _direction.Normalize();
        _elapsed = 0f;

        ctx.Weapon?.CancelReload();
    }

    public override bool Tick(PlayerSkillContext ctx)
    {
        _elapsed += Time.deltaTime;
        if (_elapsed >= Data.duration)
            return false;

        ctx.Controller.Move(_direction * (Data.speed * Time.deltaTime));
        return true;
    }
}
