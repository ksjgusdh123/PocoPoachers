using UnityEngine;

public class PlanetSelectUI : UIBase
{
    [SerializeField] private Transform _content;
    [SerializeField] private PlanetSlotUI _slotPrefab;

    protected override UIType UiType => UIType.PlanetSelect;

    protected override void Awake()
    {
        PopulateSlots();
        base.Awake();
    }

    private void PopulateSlots()
    {
        foreach (var data in PlanetTable.Instance.All)
        {
            var slot = Instantiate(_slotPrefab, _content);
            // TODO: 쉘터 레벨 시스템 구현 후 실제 해금 조건으로 교체
            bool isUnlocked = true;
            slot.Setup(data, isUnlocked);
        }
    }
}
