using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "WantsToDodge", story: "[Self] 가 구르기를 원하는지 확인", category: "Conditions", id: "4a6b8c1d3e2f5a7b9c0d1e2f3a4b5c6d")]
public partial class WantsToDodgeCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    public override bool IsTrue()
    {
        if (Self?.Value == null)
        {
            //Debug.Log("[WantsToDodge] Self가 null");
            return false;
        }

        var dodgeState = Self.Value.GetComponent<AIDodgeState>();
        var stat = Self.Value.GetComponent<EnemyStat>();
        float ratio = (stat != null && stat.MaxHp > 0f) ? stat.CurrentHp / stat.MaxHp : -1f;

        bool result = dodgeState != null && dodgeState.WantsToDodge;
        //Debug.Log($"[WantsToDodge] result={result}, dodgeState null={dodgeState == null}, WantsToDodge={dodgeState?.WantsToDodge}, hpRatio={ratio:F2}");
        return result;
    }
}
