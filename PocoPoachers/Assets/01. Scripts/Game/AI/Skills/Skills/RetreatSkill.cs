using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 타겟에게서 멀어지는 스킬. 씬에 RetreatPointSet이 있으면 그 지점 중 하나로 랜덤 후퇴,
// 없으면 타겟 반대 방향으로 distance 만큼 후퇴한다. 후퇴 중에는 이동 방향을 바라본다.
// 수치는 skill.csv(SkillData)에서 주입: distance=후퇴 거리.
public class RetreatSkill : SkillBase
{
    // 지정 지점 도착 판정 여유 거리
    private const float ArriveThreshold = 0.1f;
    // 지정 지점을 NavMesh 위로 보정할 때 탐색 반경
    private const float PointSampleRange = 2f;

    public override SkillId Id => SkillId.Retreat;

    private RetreatPointSet _pointSet;
    private bool _pointSetSearched;

    public RetreatSkill(SkillData data) : base(data) { }

    public override void Begin(SkillContext ctx)
    {
        ctx.Stat?.MarkRetreated();
        StartFacingMovement(ctx);

        // 지정 후퇴 지점이 있으면 그 중 하나로 랜덤 후퇴, 없으면 타겟 반대 방향으로 후퇴
        if (!TryRetreatToRandomPoint(ctx))
            RetreatAwayFromTarget(ctx);
    }

    // 두 모드 모두 설정한 목적지에 도착하면 종료 (타겟과의 실시간 거리와 무관)
    public override bool Tick(SkillContext ctx)
    {
        return !HasReachedDestination(ctx);
    }

    public override void End(SkillContext ctx)
    {
        // 이동 방향 우선 모드 해제 → 다시 RotateToTarget(조준)이 회전을 가져갈 수 있게 함
        if (ctx.Rotator != null)
            ctx.Rotator.EndFaceMovement();

        if (ctx.Agent == null)
            return;
        ctx.Agent.updateRotation = false;
        if (ctx.Agent.hasPath)
            ctx.Agent.ResetPath();
    }

    // 후퇴 중에는 이동 방향을 바라보게 함 — 병렬 브랜치의 RotateToTarget이 타겟을 물고 있어도
    // AIRotator의 이동 방향 우선 모드가 조준을 덮어씀. AIRotator가 없으면 NavMeshAgent 회전으로 폴백
    private void StartFacingMovement(SkillContext ctx)
    {
        if (ctx.Rotator != null)
            ctx.Rotator.BeginFaceMovement();
        else
            ctx.Agent.updateRotation = true;
    }

    // 후퇴 지점 후보가 있으면 랜덤으로 한 곳을 골라 목적지로 설정하고 true 반환. 후보가 없으면 false
    private bool TryRetreatToRandomPoint(SkillContext ctx)
    {
        GameObject point = PickRandomRetreatPoint();
        if (point == null)
            return false;

        SetDestinationOnNavMesh(ctx.Agent, point.transform.position, PointSampleRange);
        return true;
    }

    // 타겟 반대 방향으로 distance 만큼 떨어진 지점으로 후퇴 (방향은 시작 시점 기준 1회 계산)
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

    // 지정 지점에 도착했는지
    private bool HasReachedDestination(SkillContext ctx)
    {
        NavMeshAgent agent = ctx.Agent;
        return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + ArriveThreshold;
    }

    // 목적지를 NavMesh 위로 보정해 설정. 보정 실패 시 원래 위치로 설정
    private void SetDestinationOnNavMesh(NavMeshAgent agent, Vector3 desiredPos, float sampleRange)
    {
        Vector3 dest = NavMesh.SamplePosition(desiredPos, out NavMeshHit hit, sampleRange, NavMesh.AllAreas)
            ? hit.position
            : desiredPos;
        agent.SetDestination(dest);
    }

    // 후퇴 지점 후보 중 null이 아닌 것들 중에서 하나를 랜덤 선택. 후보가 없으면 null
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
