using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Say", story: "[AISpeech]가 [word] 라고 [second]동안 말합니다", category: "Action", id: "616dd9eac284c889f3910a969a5c405a")]
public partial class SayAction : Action
{
    [SerializeReference] public BlackboardVariable<AISpeech> AISpeech;
    [SerializeReference] public BlackboardVariable<string> word;
    [SerializeReference] public BlackboardVariable<float> second;

    protected override Status OnStart()
    {
        AISpeech.Value.Say(word.Value, second.Value);
        return Status.Success;
    }
}
