using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "DodgeRoll", story: "[Self] 가 공격자 반대 방향으로 [RollSpeed] 속도로 구르며 무적, 지속시간은 [Animator] 구르기 클립 길이 (없으면 [Duration])", category: "Action", id: "8b3c5d7e9f1a2b4c6d8e0f1a2b3c4d5e")]
public partial class DodgeRollAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<float> Duration;
    [SerializeReference] public BlackboardVariable<float> RollSpeed;
    [SerializeReference] public BlackboardVariable<Animator> Animator;
    // 구르기 애니메이션 클립 이름 — 이 클립의 길이를 구르기 지속 시간으로 사용
    [SerializeField] private string _rollClipName = "Sprinting Forward Roll";

    private NavMeshAgent _agent;
    private EnemyStat _stat;
    private AIDodgeState _dodgeState;
    private Vector3 _direction;
    private float _elapsed;
    // 실제 구르기 지속 시간 — 애니메이션 클립 길이를 따오고, 못 구하면 Duration으로 폴백
    private float _duration;

    protected override Status OnStart()
    {
        if (_agent == null) _agent = Self.Value.GetComponent<NavMeshAgent>();
        if (_stat == null) _stat = Self.Value.GetComponent<EnemyStat>();
        if (_dodgeState == null) _dodgeState = Self.Value.GetComponent<AIDodgeState>();

        GameObject attacker = _dodgeState?.LastAttacker;
        _direction = attacker != null
            ? attacker.transform.position - Self.Value.transform.position
            : Self.Value.transform.forward;
        _direction.y = 0f;
        if (_direction.sqrMagnitude < 0.01f)
            _direction = Self.Value.transform.forward;
        _direction.Normalize();

        _stat?.SetInvincible(true);

        // 구르기 지속 시간을 애니메이션 클립 길이로 설정 — 못 구하면 Duration 값으로 폴백
        _duration = GetRollClipLength();
        if (_duration <= 0f)
            _duration = Duration.Value;

        // 구르는 동안은 BT의 다른 이동 액션이 경로를 덮어쓰지 못하도록 NavMeshAgent의 경로 추종을 잠시 멈춤
        _dodgeState?.ConsumeDodge();
        _agent.isStopped = true;
        _elapsed = 0f;
        return Status.Running;
    }

    // 애니메이터 컨트롤러에서 구르기 클립을 이름으로 찾아 길이(초)를 반환. 못 찾으면 0
    private float GetRollClipLength()
    {
        Animator anim = Animator?.Value;
        if (anim == null || anim.runtimeAnimatorController == null)
            return 0f;

        foreach (AnimationClip clip in anim.runtimeAnimatorController.animationClips)
            if (clip.name == _rollClipName)
                return clip.length;

        return 0f;
    }

    protected override Status OnUpdate()
    {
        _elapsed += Time.deltaTime;
        if (_elapsed >= _duration)
            return Status.Success;

        _agent.Move(_direction * RollSpeed.Value * Time.deltaTime);
        return Status.Running;
    }

    protected override void OnEnd()
    {
        _stat?.SetInvincible(false);
        if (_agent != null)
            _agent.isStopped = false;
    }
}
