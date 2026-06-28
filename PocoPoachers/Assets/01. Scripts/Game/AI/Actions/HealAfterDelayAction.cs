using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "HealAfterDelay", story: "[Self] 가 [Delay] 초 뒤에 [HealAmount] 만큼 체력 회복", category: "Action", id: "2f6b8c1d3e4f5a6b7c8d9e0f1a2b3c4d")]
public partial class HealAfterDelayAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<float> HealAmount;
    [SerializeReference] public BlackboardVariable<float> Delay;

    private StatBase _stat;
    private float _elapsed;

    protected override Status OnStart()
    {
        if (_stat == null)
            _stat = Self.Value.GetComponent<StatBase>();

        if (_stat == null || _stat.IsDead)
            return Status.Failure;

        _elapsed = 0f;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (_stat == null || _stat.IsDead)
            return Status.Failure;

        _elapsed += Time.deltaTime;
        if (_elapsed < Delay.Value)
            return Status.Running;

        _stat.Heal(HealAmount.Value);
        return Status.Success;
    }

    protected override void OnEnd() { }
}
