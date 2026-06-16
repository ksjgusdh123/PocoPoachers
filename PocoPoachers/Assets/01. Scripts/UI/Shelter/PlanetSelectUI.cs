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
            bool isUnlocked = ShelterManager.GetInstance().IsPlanetUnlocked(data);
            slot.Setup(data, isUnlocked);
        }
    }
}
