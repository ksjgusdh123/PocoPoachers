using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 타겟에게서 멀어지는 스킬. RetreatPointSet이 있으면 그 지점 중 랜덤, 없으면 타겟 반대 방향으로 distance 만큼.
public class RetreatSkill : SkillBase
{
    private const float ArriveThreshold = 0.1f;
    private const float PointSampleRange = 2f;

    public override SkillId Id => SkillId.Retreat;

    private RetreatPointSet _pointSet;
    private bool _pointSetSearched;

    // 진행 중인 후퇴 목적지 — 구르기 등으로 끊겼다 재진입할 때 이 지점으로 이어감.
    // 스킬 인스턴스는 재사용되므로 End→Begin 사이에도 값이 유지된다.
    private Vector3 _destination;
    private bool _hasDestination;

    public RetreatSkill(SkillData data) : base(data) { }

    public override void Begin(SkillContext ctx)
    {
        ctx.Stat?.MarkRetreated();
        ctx.SetBlackboard(BlackboardKeys.IsRetreating, true);
        StartFacingMovement(ctx);

        if (_hasDestination)
            ctx.Agent.SetDestination(_destination);   // 끊겼다 재진입 → 같은 목적지로 이어감
        else if (!TryRetreatToRandomPoint(ctx))
            RetreatAwayFromTarget(ctx);
    }

    // 설정한 목적지에 도착하면 완료 (타겟과의 거리와 무관)
    public override bool Tick(SkillContext ctx)
    {
        if (!HasReachedDestination(ctx))
            return true;

        // 진짜 도착 → 후퇴 완료. 이어갈 목적지 없애고 깃발 끔
        _hasDestination = false;
        ctx.SetBlackboard(BlackboardKeys.IsRetreating, false);
        return false;
    }

    public override void End(SkillContext ctx)
    {
        // 여기선 IsRetreating을 끄지 않는다 — 구르기 등으로 잠시 끊긴 것일 수 있으므로.
        // 진짜 완료는 Tick에서 끄고, 재진입 시 이어간다.
        if (ctx.Rotator != null)
            ctx.Rotator.EndFaceMovement();

        if (ctx.Agent == null)
            return;
        ctx.Agent.updateRotation = false;
        if (ctx.Agent.hasPath)
            ctx.Agent.ResetPath();
    }

    // 병렬 RotateToTarget이 타겟을 물고 있어도 이동 방향을 보도록 우선 모드 사용
    private void StartFacingMovement(SkillContext ctx)
    {
        if (ctx.Rotator != null)
            ctx.Rotator.BeginFaceMovement();
        else
            ctx.Agent.updateRotation = true;
    }

    private bool TryRetreatToRandomPoint(SkillContext ctx)
    {
        GameObject point = PickRandomRetreatPoint();
        if (point == null)
            return false;

        SetDestinationOnNavMesh(ctx.Agent, point.transform.position, PointSampleRange);
        return true;
    }

    private void RetreatAwayFromTarget(SkillContext ctx)
    {
        Vector3 selfPos = ctx.Self.transform.position;

        GameObject target = ctx.Target;
        Vector3 awayDir = target != null
            ? selfPos - target.transform.position
            : -ctx.Self.transform.forward;
        awayDir.y = 0f;
        if (awayDir.sqrMagnitude < 0.0001f)
            awayDir = -ctx.Self.transform.forward;
        awayDir.Normalize();

        SetDestinationOnNavMesh(ctx.Agent, selfPos + awayDir * Data.distance, Data.distance);
    }

    private bool HasReachedDestination(SkillContext ctx)
    {
        NavMeshAgent agent = ctx.Agent;
        return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + ArriveThreshold;
    }

    // 목적지를 NavMesh 위로 보정해 설정·저장 (실패 시 원래 위치)
    private void SetDestinationOnNavMesh(NavMeshAgent agent, Vector3 desiredPos, float sampleRange)
    {
        _destination = NavMesh.SamplePosition(desiredPos, out NavMeshHit hit, sampleRange, NavMesh.AllAreas)
            ? hit.position
            : desiredPos;
        _hasDestination = true;
        agent.SetDestination(_destination);
    }

    private GameObject PickRandomRetreatPoint()
    {
        if (!_pointSetSearched)
        {
            _pointSet = Object.FindFirstObjectByType<RetreatPointSet>();
            _pointSetSearched = true;
        }

        IReadOnlyList<GameObject> points = _pointSet != null ? _pointSet.Points : null;
        if (points == null || points.Count == 0)
            return null;

        List<GameObject> valid = null;
        foreach (GameObject p in points)
        {
            if (p == null) continue;
            (valid ??= new List<GameObject>()).Add(p);
        }

        if (valid == null || valid.Count == 0)
            return null;

        return valid[Random.Range(0, valid.Count)];
    }
}
