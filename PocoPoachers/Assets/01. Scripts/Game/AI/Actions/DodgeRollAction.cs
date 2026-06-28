using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "DodgeRoll", story: "[Self] 가 공격자 반대 방향으로 [Duration] 동안 구르며 무적", category: "Action", id: "8b3c5d7e9f1a2b4c6d8e0f1a2b3c4d5e")]
public partial class DodgeRollAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<float> Duration;
    [SerializeReference] public BlackboardVariable<float> RollSpeed;

    private NavMeshAgent _agent;
    private EnemyStat _stat;
    private AIDodgeState _dodgeState;
    private Vector3 _direction;
    private float _elapsed;

    protected override Status OnStart()
    {
        if (_agent == null) _agent = Self.Value.GetComponent<NavMeshAgent>();
        if (_stat == null) _stat = Self.Value.GetComponent<EnemyStat>();
        if (_dodgeState == null) _dodgeState = Self.Value.GetComponent<AIDodgeState>();

        GameObject attacker = _dodgeState?.LastAttacker;
        _direction = attacker != null
            ? Self.Value.transform.position - attacker.transform.position
            : -Self.Value.transform.forward;
        _direction.y = 0f;
        if (_direction.sqrMagnitude < 0.01f)
            _direction = -Self.Value.transform.forward;
        _direction.Normalize();

        _stat?.SetInvincible(true);

        // 구르는 동안은 BT의 다른 이동 액션이 경로를 덮어쓰지 못하도록 NavMeshAgent의 경로 추종을 잠시 멈춤
        _agent.isStopped = true;
        _elapsed = 0f;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        _elapsed += Time.deltaTime;
        if (_elapsed >= Duration.Value)
            return Status.Success;

        _agent.Move(_direction * RollSpeed.Value * Time.deltaTime);
        return Status.Running;
    }

    protected override void OnEnd()
    {
        _dodgeState?.ConsumeDodge();
        _stat?.SetInvincible(false);
        if (_agent != null)
            _agent.isStopped = false;
    }
}
