using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class GunEnhancementDropHandler : ItemHolderDropHandler
{
    public event Action<ItemData> OnGunSet;
    public event Action OnGunCleared;

    protected override bool OnItemDropped(ItemData data, int amount)
    {
        if (!base.OnItemDropped(data, amount)) return false;

        OnGunSet?.Invoke(data);
        return true;
    }

    public override void Unequip()
    {
        base.Unequip();
        OnGunCleared?.Invoke();
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;
        if (!_isSetted) return;

        Unequip();
    }
}
