using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Line Trace To Wall", story: "[Self] 에서 [Target] 사이 시야가 벽이 없는지", category: "Conditions", id: "3e79e0124d3572f5f667c21b761267e0")]
public partial class LineTraceToWallCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;

    // 벽으로 판단할 레이어 마스크
    public LayerMask WallLayer;
    // 눈 높이 오프셋 (발 위치에서 레이 시작점 보정)
    public float EyeHeight = 1.5f;

    public override bool IsTrue()
    {
        if (Self.Value == null || Target.Value == null)
            return false;

        Vector3 eyeOffset = Vector3.up * EyeHeight;
        Vector3 origin = Self.Value.transform.position + eyeOffset;
        Vector3 targetPos = Target.Value.transform.position + eyeOffset;
        Vector3 direction = targetPos - origin;
        float distance = direction.magnitude;

        bool isBlocked = Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, WallLayer);

        // 막혔으면 빨간색(AI→충돌 지점), 뚫렸으면 초록색(AI→Target)
        Debug.DrawLine(origin, isBlocked ? hit.point : targetPos, isBlocked ? Color.red : Color.green, 2f);

        return !isBlocked;
    }

    public override void OnStart()
    {
        // NameToLayer는 인덱스를 반환하므로 비트 시프트로 LayerMask 변환
        WallLayer = 1 << LayerMask.NameToLayer("Wall");
    }

    public override void OnEnd()
    {
    }
}
