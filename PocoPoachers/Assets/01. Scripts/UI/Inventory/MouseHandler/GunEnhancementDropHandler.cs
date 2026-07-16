using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class GunEnhancementDropHandler : ItemHolderDropHandler
{
    public event Action<ItemData> OnGunSet;
    public event Action OnGunCleared;

    private int _droppedUid;

    public int DroppedUid => _droppedUid;

    protected override bool OnItemDropped(ItemData data, int amount, int uid)
    {
        if (data.ItemType != ItemType.Armor &&
            data.ItemType != ItemType.Helmet &&
            data.ItemType != ItemType.GunPart &&
            data.ItemType != ItemType.Weapon)
            return false;

        SetDisplay(data, amount);
        _droppedUid = uid;
        OnGunSet?.Invoke(data);
        return true;
    }

    protected override int GetUnequipUid() => _droppedUid;

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
