using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ChangeAIState", story: "[animator] Change [AIState] To [State] [CurrentAnimState] To [AnimState]", category: "Action", id: "9d267d8e351dfef93fd393b09e7287d0")]
public partial class ChangeAiStateAction : Action
{
    [SerializeReference] public BlackboardVariable<Animator> animator;
    [SerializeReference] public BlackboardVariable<AIState> AIState;
    [SerializeReference] public BlackboardVariable<AIState> State;
    [SerializeReference] public BlackboardVariable<AIAnimState> CurrentAnimState;
    [SerializeReference] public BlackboardVariable<AIAnimState> AnimState;

    private static readonly int AnimStateHash = Animator.StringToHash("currentState");

    protected override Status OnStart()
    {
        if (AIState != null && State != null && !AIStateTransitionRules.CanTransition(AIState.Value, State.Value))
            return Status.Failure;

        if (animator?.Value != null && AnimState != null)
        {
            animator.Value.SetInteger(AnimStateHash, (int)AnimState.Value);
            CurrentAnimState.Value = AnimState.Value;
        }

        if (AIState != null && State != null)
            AIState.Value = State.Value;

        return Status.Success;
    }
}

