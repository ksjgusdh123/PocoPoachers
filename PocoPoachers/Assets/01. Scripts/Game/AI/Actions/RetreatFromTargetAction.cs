using System;
using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "RetreatFromTarget", story: "[Self] 가 [Target] 으로부터 [RetreatDistance] 만큼 후퇴, 지정 시 [RetreatPoints] 중 한 곳으로", category: "Action", id: "9d4e1a2b3c4d5e6f7a8b9c0d1e2f3a4b")]
public partial class RetreatFromTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<float> RetreatDistance;
    // 후퇴 지점 후보 — 지정되면 이 중 한 곳으로 랜덤 후퇴, 비어 있으면 기존 방식(타겟 반대 방향)
    [SerializeReference] public BlackboardVariable<List<GameObject>> RetreatPoints;

    // 지정 지점 도착 판정 여유 거리
    private const float ArriveThreshold = 0.1f;
    // 지정 지점을 NavMesh 위로 보정할 때 탐색 반경
    private const float PointSampleRange = 2f;

    private NavMeshAgent _agent;
    private AIRotator _rotator;
    // 지정된 후퇴 지점으로 이동 중인지 — true면 지점 도착으로 종료, false면 기존 거리 기준 종료
    private bool _usingPoint;

    protected override Status OnStart()
    {
        if (_agent == null) _agent = Self.Value.GetComponent<NavMeshAgent>();
        if (_rotator == null) _rotator = Self.Value.GetComponent<AIRotator>();

        StartFacingMovement();
        Self.Value.GetComponent<EnemyStat>()?.MarkRetreated();

        // 지정된 후퇴 지점이 있으면 그 중 하나로 랜덤 후퇴, 없으면 타겟 반대 방향으로 후퇴
        _usingPoint = TryRetreatToRandomPoint();
        if (!_usingPoint)
            RetreatAwayFromTarget();

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        bool finished = _usingPoint ? HasReachedDestination() : HasEscapedTarget();
        if (finished)
        {
            _agent.ResetPath();
            return Status.Success;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        // 이동 방향 우선 모드 해제 → 다시 RotateToTarget(조준)이 회전을 가져갈 수 있게 함
        _rotator?.EndFaceMovement();

        if (_agent == null) return;
        _agent.updateRotation = false;
        if (_agent.hasPath)
            _agent.ResetPath();
    }

    // 후퇴 중에는 이동 방향을 바라보게 함 — 병렬 브랜치의 RotateToTarget이 타겟을 물고 있어도
    // AIRotator의 이동 방향 우선 모드가 조준을 덮어씀. AIRotator가 없으면 NavMeshAgent 회전으로 폴백
    private void StartFacingMovement()
    {
        if (_rotator != null)
            _rotator.BeginFaceMovement();
        else
            _agent.updateRotation = true;
    }

    // 후퇴 지점 후보가 있으면 랜덤으로 한 곳을 골라 목적지로 설정하고 true 반환. 후보가 없으면 false
    private bool TryRetreatToRandomPoint()
    {
        GameObject point = PickRandomRetreatPoint();
        if (point == null)
            return false;

        SetDestinationOnNavMesh(point.transform.position, PointSampleRange);
        return true;
    }

    // 타겟 반대 방향으로 RetreatDistance 만큼 떨어진 지점으로 후퇴 (방향은 시작 시점 기준 1회 계산)
    private void RetreatAwayFromTarget()
    {
        Vector3 selfPos = Self.Value.transform.position;

        Vector3 awayDir = selfPos - Target.Value.transform.position;
        awayDir.y = 0f;
        if (awayDir.sqrMagnitude < 0.0001f)
            awayDir = -Self.Value.transform.forward;
        awayDir.Normalize();

        SetDestinationOnNavMesh(selfPos + awayDir * RetreatDistance.Value, RetreatDistance.Value);
    }

    // 지정 지점에 도착했는지
    private bool HasReachedDestination()
    {
        return !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + ArriveThreshold;
    }

    // 타겟에서 충분히 멀어졌는지 (타겟이 사라진 경우도 종료)
    private bool HasEscapedTarget()
    {
        if (Target.Value == null)
            return true;

        float distance = Vector3.Distance(Self.Value.transform.position, Target.Value.transform.position);
        return distance >= RetreatDistance.Value;
    }

    // 목적지를 NavMesh 위로 보정해 설정. 보정 실패 시 원래 위치로 설정
    private void SetDestinationOnNavMesh(Vector3 desiredPos, float sampleRange)
    {
        Vector3 dest = NavMesh.SamplePosition(desiredPos, out NavMeshHit hit, sampleRange, NavMesh.AllAreas)
            ? hit.position
            : desiredPos;
        _agent.SetDestination(dest);
    }

    // 후퇴 지점 후보 중 null이 아닌 것들 중에서 하나를 랜덤 선택. 후보가 없으면 null
    private GameObject PickRandomRetreatPoint()
    {
        List<GameObject> points = RetreatPoints?.Value;
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

        return valid[UnityEngine.Random.Range(0, valid.Count)];
    }
}
