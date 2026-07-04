using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class RepairSlotDropHandler : ItemHolderDropHandler
{
    public event Action<ItemData> OnItemSet;
    public event Action OnItemCleared;

    private int _droppedUid;

    protected override bool OnItemDropped(ItemData data, int amount, int uid)
    {
        if (data.ItemType != ItemType.Weapon &&
            data.ItemType != ItemType.Helmet &&
            data.ItemType != ItemType.Armor)
            return false;

        SetDisplay(data, amount);
        _droppedUid = uid;
        OnItemSet?.Invoke(data);
        return true;
    }

    public int DroppedUid => _droppedUid;

    protected override int GetUnequipUid() => _droppedUid;

    public override void Unequip()
    {
        base.Unequip();
        OnItemCleared?.Invoke();
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;
        if (!_isSetted) return;

        Unequip();
    }
}
