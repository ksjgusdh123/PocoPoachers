using System;
using UnityEngine;

// 장착 가능한 아이템 인스턴스(총/방어구/헬멧 등)의 공통 기반
// uid는 해당 아이템이 처음 월드 오브젝트로 스폰될 때 발급된 값을 그대로 이어받아,
// 장착/해제를 반복해도 동일 개체로 식별할 수 있게 한다
public abstract class EquippableItemBase : MonoBehaviour
{
    [SerializeField] protected int _itemId;

    protected int _uid;
    protected float _maxDurability;
    protected float _currentDurability;

    public int Uid => _uid;
    public int ItemId => _itemId;
    public float MaxDurability => _maxDurability;
    public float CurrentDurability => _currentDurability;

    public event Action<float, float> OnDurabilityChanged; // (현재, 최대)

    // 월드 오브젝트의 uid를 그대로 받아 아이템 인스턴스를 초기화한다
    public virtual void Initialize(int uid, int itemId, float maxDurability)
    {
        _uid = uid;
        _itemId = itemId;
        _maxDurability = maxDurability;
        _currentDurability = maxDurability;
    }

    public virtual void SetDurability(float value)
    {
        _currentDurability = Mathf.Clamp(value, 0f, _maxDurability);
        OnDurabilityChanged?.Invoke(_currentDurability, _maxDurability);
    }
}
