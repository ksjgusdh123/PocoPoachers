using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "RetreatFromTarget", story: "[Self] 가 [Target] 으로부터 [RetreatDistance] 만큼 후퇴", category: "Action", id: "9d4e1a2b3c4d5e6f7a8b9c0d1e2f3a4b")]
public partial class RetreatFromTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<float> RetreatDistance;

    private NavMeshAgent _agent;
    private AIRotator _rotator;
    // 후퇴 시작 시점의 (자기 위치 - 타겟 위치) 방향을 한 번만 계산해 고정 — 이후엔 타겟이 움직여도 이 방향으로 후퇴
    private Vector3 _awayDir;

    protected override Status OnStart()
    {
        if (_agent == null)
            _agent = Self.Value.GetComponent<NavMeshAgent>();
        if (_rotator == null)
            _rotator = Self.Value.GetComponent<AIRotator>();

        // 후퇴 중에는 이동 방향을 바라보게 함 — 병렬 브랜치의 RotateToTarget이 타겟을 물고 있어도
        // AIRotator의 이동 방향 우선 모드가 조준을 덮어씀. AIRotator가 없으면 NavMeshAgent 회전으로 폴백
        if (_rotator != null)
            _rotator.BeginFaceMovement();
        else
            _agent.updateRotation = true;

        Self.Value.GetComponent<EnemyStat>()?.MarkRetreated();

        // 시작 시점의 위치 차이로 후퇴 방향을 한 번만 고정
        _awayDir = (Self.Value.transform.position - Target.Value.transform.position);
        _awayDir.y = 0f;
        if (_awayDir.sqrMagnitude < 0.0001f)
            _awayDir = -Self.Value.transform.forward;
        _awayDir.Normalize();

        SetRetreatDestination();
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Target.Value == null)
        {
            _agent.ResetPath();
            return Status.Success;
        }

        float distance = Vector3.Distance(Self.Value.transform.position, Target.Value.transform.position);
        if (distance >= RetreatDistance.Value)
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

    private void SetRetreatDestination()
    {
        Vector3 selfPos = Self.Value.transform.position;
        // 시작 시점에 고정한 방향으로 후퇴 (타겟의 현재 위치를 매번 다시 참조하지 않음)
        Vector3 desiredPos = selfPos + _awayDir * RetreatDistance.Value;

        if (NavMesh.SamplePosition(desiredPos, out NavMeshHit hit, RetreatDistance.Value, NavMesh.AllAreas))
            _agent.SetDestination(hit.position);
    }
}
