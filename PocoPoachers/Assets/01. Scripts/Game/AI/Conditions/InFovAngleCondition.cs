using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "InFovAngle", story: "[Self] 의 시야각 [FovAngle] 안에 [Target] 이 있는지", category: "Conditions", id: "9459f75ab8710a496003f8d9a1eea2eb")]
public partial class InFovAngleCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    // 시야각 전체 (ex. 120이면 좌우 각 60도)
    [SerializeReference] public BlackboardVariable<float> FovAngle;

    public float ViewRange = 10f;

    public override bool IsTrue()
    {
        if (Self.Value == null || Target.Value == null)
            return false;

        Vector3 forward = Self.Value.transform.forward;
        Vector3 dirToTarget = (Target.Value.transform.position - Self.Value.transform.position).normalized;

        float angle = Vector3.Angle(forward, dirToTarget);
        bool inFov = angle <= FovAngle.Value * 0.5f;

        DrawFov(inFov);
        return inFov;
    }

    private void DrawFov(bool detected)
    {
        Transform t = Self.Value.transform;
        float half = FovAngle.Value * 0.5f;
        Color color = detected ? Color.yellow : Color.white;

        // 좌우 경계선
        Vector3 leftDir = Quaternion.Euler(0, -half, 0) * t.forward;
        Vector3 rightDir = Quaternion.Euler(0, half, 0) * t.forward;
        Debug.DrawRay(t.position, leftDir * ViewRange, color, 0.1f);
        Debug.DrawRay(t.position, rightDir * ViewRange, color, 0.1f);

        // 부채꼴 호: 일정 간격으로 선분을 이어서 호처럼 보이게
        int segments = 20;
        Vector3 prev = t.position + (Quaternion.Euler(0, -half, 0) * t.forward) * ViewRange;
        for (int i = 1; i <= segments; i++)
        {
            float a = Mathf.Lerp(-half, half, i / (float)segments);
            Vector3 next = t.position + (Quaternion.Euler(0, a, 0) * t.forward) * ViewRange;
            Debug.DrawLine(prev, next, color, 0.1f);
            prev = next;
        }
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
