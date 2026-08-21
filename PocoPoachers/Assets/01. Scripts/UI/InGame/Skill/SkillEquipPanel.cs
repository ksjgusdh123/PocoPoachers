using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// 보유 스킬을 행으로 나열하고 장착 버튼으로 슬롯에 장착한다.
// 창 종류에 묶이지 않는 순수 패널이라 단독 스킬 창(SkillEquipUI)과 강화대 스킬 탭이 같은 컴포넌트를 공유한다.
public class SkillEquipPanel : MonoBehaviour
{
    [SerializeField] private SkillEquipRowUI _rowPrefab;
    [SerializeField] private Transform _listContent;
    [SerializeField] private GameObject _slotPrompt;
    [SerializeField] private TextMeshProUGUI _slotPromptText;

    private PlayerSkillManager _manager;
    private readonly List<SkillEquipRowUI> _rows = new();
    private static readonly Key[] SlotKeys = { Key.Digit1, Key.Digit2, Key.Digit3 };

    private PlayerSkillData _pendingSkill;

    public void Setup(PlayerSkillManager manager)
    {
        Unsubscribe();

        _manager = manager;
        if (_manager == null) return;

        _manager.OnSlotChanged += OnSlotChanged;
        RefreshList();
    }

    private void OnDestroy() => Unsubscribe();

    private void Unsubscribe()
    {
        if (_manager == null) return;

        _manager.OnSlotChanged -= OnSlotChanged;
        _manager = null;
    }

    private void OnEnable()
    {
        CancelSlotSelection();

        if (_manager != null)
            RefreshEquippedState();
    }

    private void OnDisable() => CancelSlotSelection();

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

    // 장착돼 있으면 해제, 아니면 어느 슬롯에 넣을지 키 입력을 기다린다.
    private void OnClickEquip(PlayerSkillData data)
    {
        if (_manager == null || data == null) return;

        CancelSlotSelection();

        if (_manager.UnequipSkill(data.id)) return;

        _pendingSkill = data;

        if (_slotPromptText != null)
            _slotPromptText.text = LocalizationManager.GetInstance().GetString("skill.select_slot");
        if (_slotPrompt != null)
            _slotPrompt.SetActive(true);
    }

    public void CancelSlotSelection()
    {
        _pendingSkill = null;
        if (_slotPrompt != null)
            _slotPrompt.SetActive(false);
    }

    private void Update()
    {
        if (_pendingSkill == null || _manager == null) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        for (int i = 0; i < SlotKeys.Length && i < PlayerSkillManager.SlotCount; i++)
        {
            if (!keyboard[SlotKeys[i]].wasPressedThisFrame) continue;

            _manager.Equip(i, _pendingSkill.id);
            CancelSlotSelection();
            return;
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
            CancelSlotSelection();
    }
}
