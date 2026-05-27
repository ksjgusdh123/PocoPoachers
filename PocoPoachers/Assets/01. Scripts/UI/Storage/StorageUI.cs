using UnityEngine;
using UnityEngine.UI;

public class StorageUI : InventoryUI
{
    [SerializeField] private int _pageSize = 5;
    [SerializeField] private Button _prevButton;
    [SerializeField] private Button _nextButton;

    private int _currentPage = 0;

    public int PageCount => Mathf.CeilToInt((float)Inventory.CurrentCapacity / _pageSize);

    protected override void Awake()
    {
        base.Awake();

        _prevButton?.onClick.AddListener(PrevPage);
        _nextButton?.onClick.AddListener(NextPage);
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

        int current = Inventory.CurrentCapacity;
        int start = _currentPage * _pageSize;
        int end = Mathf.Min(start + _pageSize, current);

        for (int i = 0; i < _slotUIs.Length; i++)
        {
            bool isActive = i >= start && i < end;
            _slotUIs[i].gameObject.SetActive(isActive);

            if (isActive)
                _slotUIs[i].SetSlot(Inventory.Slots[i]);
        }
    }
}
