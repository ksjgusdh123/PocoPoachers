using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 좌측: 보유 스킬 목록(아이콘 + 이름)
// 우측: 선택된 스킬 상세(아이콘, 이름, 설명, 쿨타임)와 장착 버튼
// 창 종류에 묶이지 않는 순수 패널이라 단독 스킬 창(SkillEquipUI)과 강화대 스킬 탭이 같은 컴포넌트를 공유한다.
public class SkillEquipPanel : MonoBehaviour
{
    [Header("목록")]
    [SerializeField] private SkillEquipRowUI _rowPrefab;
    [SerializeField] private Transform _listContent;

    [Header("상세")]
    [SerializeField] private GameObject _detailPanel;
    [SerializeField] private Image _detailIcon;
    [SerializeField] private TextMeshProUGUI _detailNameText;
    [SerializeField] private TextMeshProUGUI _detailDescriptionText;
    [SerializeField] private TextMeshProUGUI _detailCooldownText;

    [Header("장착")]
    [SerializeField] private Button _equipButton;
    [SerializeField] private TextMeshProUGUI _equipButtonText;
    [SerializeField] private GameObject _slotPrompt;
    [SerializeField] private TextMeshProUGUI _slotPromptText;

    private PlayerSkillManager _manager;
    private readonly List<SkillEquipRowUI> _rows = new();
    private static readonly Key[] SlotKeys = { Key.Digit1, Key.Digit2, Key.Digit3 };

    private PlayerSkillData _selected;
    private PlayerSkillData _pendingSkill;

    private void Awake()
    {
        _equipButton?.onClick.AddListener(OnClickEquip);
        _detailPanel?.SetActive(false);
    }

    public void Setup(PlayerSkillManager manager)
    {
        Unsubscribe();

        _manager = manager;
        if (_manager == null) return;

        _manager.OnSlotChanged += OnSlotChanged;
        _manager.OnUnlockChanged += OnUnlockChanged;
        RefreshList();
        RefreshDetail();
    }

    private void OnDestroy() => Unsubscribe();

    private void Unsubscribe()
    {
        if (_manager == null) return;

        _manager.OnSlotChanged -= OnSlotChanged;
        _manager.OnUnlockChanged -= OnUnlockChanged;
        _manager = null;
    }

    private void OnEnable()
    {
        CancelSlotSelection();

        if (_manager == null) return;

        RefreshEquippedState();
        RefreshDetail();
    }

    private void OnDisable() => CancelSlotSelection();

    // 슬롯이 바뀌면 목록의 장착 표시와 상세의 버튼 문구가 함께 따라가야 한다.
    private void OnSlotChanged(int slotIndex, IPlayerSkill skill)
    {
        RefreshEquippedState();
        RefreshDetail();
    }

    // 강화대에서 스탯을 찍어 스킬이 해금되면 창을 다시 열지 않아도 잠금 표시가 풀려야 한다.
    private void OnUnlockChanged()
    {
        RefreshEquippedState();
        RefreshDetail();
    }

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
            row.Setup(data, OnSelectSkill);
            row.SetEquipped(_manager.IsEquipped(data.id));
            row.SetLocked(!_manager.IsUsable(data));
            row.SetSelected(_selected != null && _selected.id == data.id);
        }

        for (int i = used; i < _rows.Count; i++)
        {
            if (_rows[i] != null && _rows[i].gameObject.activeSelf)
                _rows[i].gameObject.SetActive(false);
        }
    }

    private IEnumerable<PlayerSkillData> SortedSkills() => PlayerSkillTable.Instance.All.OrderBy(d => d.id);

    private void RefreshEquippedState()
    {
        List<PlayerSkillData> sorted = SortedSkills().ToList();

        for (int i = 0; i < _rows.Count && i < sorted.Count; i++)
        {
            if (_rows[i] == null || !_rows[i].gameObject.activeSelf) continue;

            _rows[i].SetEquipped(_manager.IsEquipped(sorted[i].id));
            _rows[i].SetLocked(!_manager.IsUsable(sorted[i]));
            _rows[i].SetSelected(_selected != null && _selected.id == sorted[i].id);
        }
    }

    private void OnSelectSkill(PlayerSkillData data)
    {
        CancelSlotSelection();

        _selected = data;
        RefreshEquippedState();
        RefreshDetail();
    }

    private void RefreshDetail()
    {
        if (_selected == null)
        {
            _detailPanel?.SetActive(false);
            return;
        }

        _detailPanel?.SetActive(true);

        LocalizationManager localization = LocalizationManager.GetInstance();

        if (_detailIcon != null)
        {
            _detailIcon.sprite = ResourceManager.Instance.LoadSprite(_selected.icon);
            _detailIcon.enabled = _detailIcon.sprite != null;
        }
        if (_detailNameText != null)
            _detailNameText.text = localization.GetString(_selected.name);

        bool locked = _manager != null && !_manager.IsUnlocked(_selected);
        bool owned = _manager != null && _manager.IsOwned(_selected);

        // 잠긴 스킬은 효과 설명 대신 해금 조건을, 해금됐지만 아직 획득 전이면 설명과 함께 필요한 재료를 보여준다.
        if (_detailDescriptionText != null)
        {
            if (locked)
                _detailDescriptionText.text = _selected.GetUnlockHint();
            else if (!owned)
                _detailDescriptionText.text = localization.GetString(_selected.description) + "\n\n" + BuildCostText(localization);
            else
                _detailDescriptionText.text = localization.GetString(_selected.description);
        }
        if (_detailCooldownText != null)
            _detailCooldownText.text = $"{_selected.cooldown:0.#}s";

        bool equipped = _manager != null && _manager.IsEquipped(_selected.id);

        // 버튼 하나가 상태에 따라 잠김 / 획득 / 장착 / 해제를 맡는다.
        if (_equipButton != null)
            _equipButton.interactable = _manager != null && !locked && (owned || _manager.CanAcquire(_selected));

        if (_equipButtonText != null)
        {
            if (locked) _equipButtonText.text = localization.GetString("skill.locked");
            else if (!owned) _equipButtonText.text = localization.GetString("skill.acquire");
            else _equipButtonText.text = localization.GetString(equipped ? "skill.unequip" : "skill.equip");
        }
    }

    // "획득 재료: 철 주괴 3/8" — 보유량과 필요량을 함께 보여준다.
    private string BuildCostText(LocalizationManager localization)
    {
        if (!_selected.TryGetCost(out ItemData item, out int count)) return "";

        int owned = _manager != null ? _manager.GetOwnedItemCount(_selected) : 0;
        return string.Format(
            localization.GetString("skill.acquire_cost"),
            localization.GetString(item.ItemName),
            owned,
            count);
    }

    // 아직 획득 전이면 재료를 소모해 획득하고, 장착돼 있으면 해제,
    // 그 외에는 어느 슬롯에 넣을지 키 입력을 기다린다.
    private void OnClickEquip()
    {
        if (_manager == null || _selected == null) return;
        if (!_manager.IsUnlocked(_selected)) return;

        CancelSlotSelection();

        // 획득에 성공하면 OnUnlockChanged가 상세를 "장착" 상태로 새로 그린다.
        if (!_manager.IsOwned(_selected))
        {
            _manager.TryAcquire(_selected);
            return;
        }

        if (_manager.UnequipSkill(_selected.id)) return;

        _pendingSkill = _selected;

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
