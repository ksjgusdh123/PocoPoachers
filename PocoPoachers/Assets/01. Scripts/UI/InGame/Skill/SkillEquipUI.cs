using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 보유 스킬을 행으로 나열하고 장착 버튼으로 빈 슬롯에 자동 장착한다.
public class SkillEquipUI : UIBase
{
    protected override UIType UiType => UIType.Skill;

    [SerializeField] private SkillEquipRowUI _rowPrefab;
    [SerializeField] private Transform _listContent;

    private PlayerSkillManager _manager;
    private readonly List<SkillEquipRowUI> _rows = new();

    public void Setup(PlayerSkillManager manager)
    {
        Unsubscribe();

        _manager = manager;
        if (_manager == null) return;

        _manager.OnSlotChanged += OnSlotChanged;
        RefreshList();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Unsubscribe();
    }

    private void Unsubscribe()
    {
        if (_manager == null) return;

        _manager.OnSlotChanged -= OnSlotChanged;
        _manager = null;
    }

    private void OnEnable()
    {
        if (_manager != null)
            RefreshEquippedState();
    }

    private void OnSlotChanged(int slotIndex, IPlayerSkill skill) => RefreshEquippedState();

    // 엔트리는 파괴하지 않고 재사용한다 — CraftingTableUI와 동일한 이유(GC 스파이크 방지).
    private void RefreshList()
    {
        if (_rowPrefab == null || _listContent == null) return;

        int used = 0;

        foreach (PlayerSkillData data in PlayerSkillTable.Instance.All.OrderBy(d => d.id))
        {
            if (used == _rows.Count)
                _rows.Add(Instantiate(_rowPrefab, _listContent));

            SkillEquipRowUI row = _rows[used];
            used++;
            if (row == null) continue;

            if (!row.gameObject.activeSelf) row.gameObject.SetActive(true);
            row.transform.SetSiblingIndex(used - 1);
            row.Setup(data, OnClickEquip);
            row.SetEquipped(_manager.IsEquipped(data.id));
        }

        for (int i = used; i < _rows.Count; i++)
        {
            if (_rows[i] != null && _rows[i].gameObject.activeSelf)
                _rows[i].gameObject.SetActive(false);
        }
    }

    private void RefreshEquippedState()
    {
        List<PlayerSkillData> sorted = PlayerSkillTable.Instance.All.OrderBy(d => d.id).ToList();

        for (int i = 0; i < _rows.Count && i < sorted.Count; i++)
        {
            if (_rows[i] != null && _rows[i].gameObject.activeSelf)
                _rows[i].SetEquipped(_manager.IsEquipped(sorted[i].id));
        }
    }

    private void OnClickEquip(PlayerSkillData data)
    {
        if (_manager == null || data == null) return;

        if (_manager.EquipToEmptySlot(data.id) >= 0) return;

        LocalizationManager localization = LocalizationManager.GetInstance();
        UIManager.Instance?.ShowNotice(
            localization.GetString("skill.slot_full_title"),
            localization.GetString("skill.slot_full_message"));
    }
}
