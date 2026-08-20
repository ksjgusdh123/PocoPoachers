using UnityEngine;

// 로컬 플레이어의 스킬 슬롯 HUD. PlayerSkillManager가 스폰 후 Setup으로 연결한다.
public class SkillHudUI : MonoBehaviour
{
    [SerializeField] private SkillSlotUI[] _slots;

    private PlayerSkillManager _manager;

    public void Setup(PlayerSkillManager manager)
    {
        Unsubscribe();

        _manager = manager;
        if (_manager == null) return;

        _manager.OnSlotChanged += RefreshSlot;

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null) continue;

            _slots[i].SetKeyLabel($"Shift+{i + 1}");
            RefreshSlot(i, _manager.GetSkill(i));
        }
    }

    private void OnDestroy() => Unsubscribe();

    private void Unsubscribe()
    {
        if (_manager == null) return;

        _manager.OnSlotChanged -= RefreshSlot;
        _manager = null;
    }

    private void RefreshSlot(int slotIndex, IPlayerSkill skill)
    {
        if (slotIndex < 0 || slotIndex >= _slots.Length) return;

        _slots[slotIndex]?.SetSkill(skill);
    }

    private void Update()
    {
        if (_manager == null) return;

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null) continue;

            IPlayerSkill skill = _manager.GetSkill(i);
            if (skill == null) continue;

            _slots[i].SetCooldown(_manager.GetCooldownRemaining(i), skill.Cooldown);
        }
    }
}
