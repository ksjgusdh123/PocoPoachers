using UnityEngine;

// 단축키로 여닫는 단독 스킬 창. 목록·장착 로직은 같은 오브젝트의 SkillEquipPanel이 담당한다.
[RequireComponent(typeof(SkillEquipPanel))]
public class SkillEquipUI : UIBase
{
    protected override UIType UiType => UIType.Skill;

    private SkillEquipPanel _panel;

    protected override void Awake()
    {
        _panel = GetComponent<SkillEquipPanel>();
        base.Awake();
    }

    public void Setup(PlayerSkillManager manager) => _panel?.Setup(manager);

    protected override void OnHide() => _panel?.CancelSlotSelection();
}
