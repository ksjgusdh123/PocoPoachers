using System;
using UnityEngine;

public class ArmorBase : MonoBehaviour
{
    [SerializeField] private int _itemId;

    private const float MaxDurability = 100f;
    private float _currentDurability = MaxDurability;

    public int ItemId => _itemId;
    public ArmorStatData Stat => DataManager.GetArmorStat(_itemId);
    public float CurrentDurability => _currentDurability;

    public event Action<float, float> OnDurabilityChanged; // (현재, 최대)

    public void SetItemId(int itemId) => _itemId = itemId;

    public void DecreaseDurability(float damage)
    {
        _currentDurability = Mathf.Max(0f, _currentDurability - damage / 10f);
        OnDurabilityChanged?.Invoke(_currentDurability, MaxDurability);
    }
}
