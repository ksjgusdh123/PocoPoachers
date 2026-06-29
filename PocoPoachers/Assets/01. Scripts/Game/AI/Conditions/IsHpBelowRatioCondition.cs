using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "IsHpBelowRatio", story: "[Self] 의 체력이 [Ratio] 비율 미만인지 확인", category: "Conditions", id: "7c2e9a4f1b3d4e5f6a7b8c9d0e1f2a3b")]
public partial class IsHpBelowRatioCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<float> Ratio;

    public override bool IsTrue()
    {
        if (Self?.Value == null) return false;

        var stat = Self.Value.GetComponent<EnemyStat>();
        if (stat == null || stat.MaxHp <= 0f) return false;
        if (stat.HasRetreated) return false;

        return stat.CurrentHp / stat.MaxHp < Ratio.Value;
    }
}
