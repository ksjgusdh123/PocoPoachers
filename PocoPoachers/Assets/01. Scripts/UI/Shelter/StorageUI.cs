using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StorageUI : InventoryUI
{
    [System.Serializable]
    private struct FilterEntry
    {
        public Button Button;
        public ItemType Type;
    }

    [SerializeField] private int _pageSize = 5;
    [SerializeField] private Button _prevButton;
    [SerializeField] private Button _nextButton;
    [SerializeField] private TextMeshProUGUI _pageText;
    [SerializeField] private FilterEntry[] _filterButtons;

    private int _currentPage = 0;
    private ItemType _filterType = ItemType.None;
    private readonly List<int> _visibleIndices = new();

    public int PageCount
    {
        get
        {
            if (Inventory == null || _pageSize <= 0) return 1;

            int visibleCount = _filterType == ItemType.None
                ? Inventory.CurrentCapacity
                : GetVisibleIndices().Count;
            return Mathf.Max(1, Mathf.CeilToInt((float)visibleCount / _pageSize));
        }
    }

    // 창고는 리빌 연출이 없으므로 항상 공개 상태로 취급 (더블클릭/설명 차단 해제)
    public override bool IsSlotUnrevealed(int slotIndex) => false;

    // 필터 설정 — 같은 타입 재클릭 시 None으로 토글
    public void SetFilter(ItemType type)
    {
        _filterType = _filterType == type ? ItemType.None : type;
        _currentPage = 0;
        RefreshFilterSelection();
        Refresh();
    }

    protected override void Awake()
    {
        base.Awake();

        _prevButton?.onClick.AddListener(PrevPage);
        _nextButton?.onClick.AddListener(NextPage);

        foreach (var entry in _filterButtons)
        {
            var type = entry.Type;
            entry.Button.onClick.AddListener(() => SetFilter(type));
        }

        RefreshFilterSelection();
    }

    private void RefreshFilterSelection()
    {
        foreach (var entry in _filterButtons)
        {
            if (entry.Button == null) continue;

            Transform accent = entry.Button.transform.Find("SelectionAccent");
            if (accent != null)
                accent.gameObject.SetActive(_filterType == entry.Type);

            TextMeshProUGUI label = entry.Button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
                label.color = _filterType == entry.Type
                    ? Color.white
                    : new Color32(0xA8, 0xB9, 0xCC, 0xFF);
        }
    }

    public void NextPage()
    {
        _currentPage = Mathf.Min(_currentPage + 1, PageCount - 1);
        Refresh();
    }

    public void PrevPage()
    {
        _currentPage = Mathf.Max(_currentPage - 1, 0);
        Refresh();
    }

    public void ShowPage(int pageIndex)
    {
        _currentPage = Mathf.Clamp(pageIndex, 0, PageCount - 1);
        Refresh();
    }

    public override void Refresh()
    {
        if (_slotUIs == null) return;

        // 필터 적용 시 조건에 맞는 슬롯 인덱스만 추출
        var visibleIndices = GetVisibleIndices();

        int start = _currentPage * _pageSize;
        int end = Mathf.Min(start + _pageSize, visibleIndices.Count);

        for (int i = 0; i < _slotUIs.Length; i++)
        {
            int visibleIndex = start + i;
            bool isActive = visibleIndex < end;
            _slotUIs[i].gameObject.SetActive(isActive);

            if (isActive)
                _slotUIs[i].SetSlot(Inventory.Slots[visibleIndices[visibleIndex]]);
        }

        if (_prevButton != null)
            _prevButton.interactable = _currentPage > 0;
        if (_nextButton != null)
            _nextButton.interactable = _currentPage < PageCount - 1;

        RefreshCountText();
    }

    // 필터 조건에 맞는 슬롯 인덱스 목록 반환
    private List<int> GetVisibleIndices()
    {
        _visibleIndices.Clear();
        for (int i = 0; i < Inventory.CurrentCapacity; i++)
        {
            var slot = Inventory.Slots[i];

            // 필터 없음: 빈 슬롯 포함 전체 표시
            if (_filterType == ItemType.None)
            {
                _visibleIndices.Add(i);
                continue;
            }

            // 필터 있음: 해당 타입 아이템만 표시
            if (!slot.IsEmpty && slot.ItemData.ItemType == _filterType)
                _visibleIndices.Add(i);
        }
        return _visibleIndices;
    }

    protected override void RefreshCountText()
    {
        // 아이템 수 / 용량 텍스트는 부모에서 처리
        base.RefreshCountText();

        string page = $"{_currentPage + 1} / {PageCount}";
        if (_pageText != null)
        {
            _pageText.text = page;
            return;
        }

        // 기존 prefab은 전용 page text가 없으므로 개선된 header meta 영역을 fallback으로 사용한다.
        Transform subtitleTransform = transform.Find("BoxNameUI/PolishHeader/Subtitle");
        TextMeshProUGUI subtitle = subtitleTransform != null
            ? subtitleTransform.GetComponent<TextMeshProUGUI>()
            : null;
        if (subtitle != null)
            subtitle.text = $"{Inventory.ItemCount} / {Inventory.CurrentCapacity} ITEMS  /  PAGE {page}";
    }
}
