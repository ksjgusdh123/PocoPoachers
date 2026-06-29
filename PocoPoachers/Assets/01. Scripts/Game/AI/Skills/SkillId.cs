using Unity.Behavior;

// AI 스킬 식별자 — BT의 UseSkill / CanUseSkill 노드에서 선택
[BlackboardEnum]
public enum SkillId
{
    Dodge,
    Retreat
}
