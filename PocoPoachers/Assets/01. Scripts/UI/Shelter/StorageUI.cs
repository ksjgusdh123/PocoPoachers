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

    public int PageCount => Mathf.CeilToInt((float)Inventory.CurrentCapacity / _pageSize);

    // 창고는 리빌 연출이 없으므로 항상 공개 상태로 취급 (더블클릭/설명 차단 해제)
    public override bool IsSlotUnrevealed(int slotIndex) => false;

    // 필터 설정 — 같은 타입 재클릭 시 None으로 토글
    public void SetFilter(ItemType type)
    {
        _filterType = _filterType == type ? ItemType.None : type;
        _currentPage = 0;
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

        // 페이지 텍스트 갱신
        if (_pageText != null)
            _pageText.text = $"{_currentPage + 1} / {PageCount}";
    }
}
