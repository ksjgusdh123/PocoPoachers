using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class GunEnhancementDropHandler : ItemHolderDropHandler
{
    public event Action<ItemData> OnGunSet;
    public event Action OnGunCleared;

    private int _droppedUid;

    protected override bool OnItemDropped(ItemData data, int amount, int uid)
    {
        if (!base.OnItemDropped(data, amount, uid)) return false;

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
