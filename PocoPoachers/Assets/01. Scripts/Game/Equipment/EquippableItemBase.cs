using System;
using System.Collections.Generic;
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

    // 현재 씬에 존재하는 모든 장착 인스턴스를 uid로 조회하기 위한 레지스트리
    // (호스트가 보내는 H_Durability 결과를 해당 uid의 인스턴스에 즉시 반영할 때 사용)
    private static readonly Dictionary<int, EquippableItemBase> _registry = new();
    public static EquippableItemBase FindByUid(int uid) => _registry.TryGetValue(uid, out var item) ? item : null;

    // durability는 그대로 둔 채 uid만 갱신 (인벤토리 슬롯에서 가져온 기존 uid를 적용할 때 사용)
    public void SetUid(int uid)
    {
        UnregisterUid();
        _uid = uid;
        RegisterUid();
    }

    // 월드 오브젝트의 uid를 그대로 받아 아이템 인스턴스를 초기화한다
    public virtual void Initialize(int uid, int itemId, float maxDurability)
    {
        UnregisterUid();
        _uid = uid;
        _itemId = itemId;
        _maxDurability = maxDurability;
        _currentDurability = maxDurability;
        RegisterUid();
    }

    public virtual void SetDurability(float value)
    {
        _currentDurability = Mathf.Clamp(value, 0f, _maxDurability);
        OnDurabilityChanged?.Invoke(_currentDurability, _maxDurability);
    }

    protected virtual void OnDestroy() => UnregisterUid();

    private void RegisterUid()
    {
        if (_uid != 0) _registry[_uid] = this;
    }

    private void UnregisterUid()
    {
        if (_uid != 0 && _registry.TryGetValue(_uid, out var cur) && cur == this)
            _registry.Remove(_uid);
    }
}
