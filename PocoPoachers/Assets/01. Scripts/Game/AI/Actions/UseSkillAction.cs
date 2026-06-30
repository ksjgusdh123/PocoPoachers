using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

// BT가 스킬 사용을 명령하는 브리지 노드 — 실제 동작은 SkillManager/ISkill이 담당
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "UseSkill", story: "[Self] 가 [Skill] 스킬 사용", category: "Action", id: "b1a2c3d4e5f60718293a4b5c6d7e8f90")]
public partial class UseSkillAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<SkillId> Skill;

    private SkillManager _manager;

    protected override Status OnStart()
    {
        if (_manager == null)
            _manager = Self.Value.GetComponent<SkillManager>();
        if (_manager == null)
            return Status.Failure;

        return _manager.TryBegin(Skill.Value) ? Status.Running : Status.Failure;
    }

    protected override Status OnUpdate()
    {
        return _manager.Tick(Skill.Value) ? Status.Running : Status.Success;
    }

    protected override void OnEnd()
    {
        _manager?.End(Skill.Value);
    }
}
