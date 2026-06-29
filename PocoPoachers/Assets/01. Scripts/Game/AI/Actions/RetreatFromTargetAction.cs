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
    private float _repathTimer;
    private const float RepathInterval = 0.5f;

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

        _repathTimer = 0f;
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

        _repathTimer -= Time.deltaTime;
        if (_repathTimer <= 0f)
            SetRetreatDestination();

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
        _repathTimer = RepathInterval;

        Vector3 selfPos = Self.Value.transform.position;
        Vector3 awayDir = (selfPos - Target.Value.transform.position).normalized;
        Vector3 desiredPos = selfPos + awayDir * RetreatDistance.Value;

        if (NavMesh.SamplePosition(desiredPos, out NavMeshHit hit, RetreatDistance.Value, NavMesh.AllAreas))
            _agent.SetDestination(hit.position);
    }
}
