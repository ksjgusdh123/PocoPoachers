using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class RepairSlotDropHandler : ItemHolderDropHandler
{
    public event Action<ItemData> OnItemSet;
    public event Action OnItemCleared;

    protected override bool OnItemDropped(ItemData data, int amount)
    {
        if (data.ItemType != ItemType.Weapon &&
            data.ItemType != ItemType.Helmet &&
            data.ItemType != ItemType.Armor)
            return false;

        SetDisplay(data, amount);
        OnItemSet?.Invoke(data);
        return true;
    }

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
