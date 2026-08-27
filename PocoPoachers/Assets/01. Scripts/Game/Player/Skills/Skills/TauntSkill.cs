using System.Collections.Generic;
using UnityEngine;

// radius 안의 적이 duration 동안 시전자만 노리게 만든다.
// 지속 판정은 적 쪽(TargetDetector)이 만료 시각을 들고 처리하므로, 도발 중에는 피격이나 소리로도
// 어그로가 넘어가지 않는다 — ForceSetTarget이 한 곳에서 막아준다.
// AI 타겟 판정은 호스트 전용이라(EnemyNetworkSetup이 게스트의 BehaviorGraphAgent를 끈다)
// 게스트는 호스트에 요청만 보내고 실제 변경은 호스트가 수행한다.
public class TauntSkill : PlayerSkillBase
{
    public override PlayerSkillId Id => PlayerSkillId.Taunt;

    private float _elapsed;

    public TauntSkill(PlayerSkillData data) : base(data) { }

    public override void Begin(PlayerSkillContext ctx)
    {
        _elapsed = 0f;

        Vector3 center = ctx.Transform.position;

        if (RoomManager.IsHost)
            ApplyAuthoritative(center, Data.radius, Data.duration, ctx.Self);
        else
            RoomSync.RequestTaunt(center, Data.radius, Data.duration);
    }

    // 스킬 자체는 시전 순간 끝나지만, 남은 도발 시간이 HUD에 뜨도록 duration 동안 살려둔다.
    public override bool Tick(PlayerSkillContext ctx)
    {
        _elapsed += Time.deltaTime;
        return _elapsed < Data.duration;
    }

    // 호스트에서만 호출된다 — 시전자 본인이거나(로컬 호스트) 게스트 요청을 대신 처리하는 경우다.
    public static void ApplyAuthoritative(Vector3 center, float radius, float duration, GameObject taunter)
    {
        if (taunter == null || radius <= 0f) return;

        // 적 하나가 콜라이더를 여러 개 가질 수 있어 TargetDetector 단위로 중복을 걸러낸다.
        var applied = new HashSet<TargetDetector>();

        foreach (var hit in Physics.OverlapSphere(center, radius))
        {
            var detector = hit.GetComponentInParent<TargetDetector>();
            if (detector == null || !applied.Add(detector)) continue;

            // duration이 비어 있는 데이터라도 최소한 타겟은 돌려세운다.
            if (duration > 0f) detector.ApplyTaunt(taunter, duration);
            else detector.ForceSetTarget(taunter);
        }
    }
}
