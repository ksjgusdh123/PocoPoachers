using System;
using UnityEngine;

// 장착 가능한 장비 컨트롤러의 공통 기반
// WeaponController, ArmorController 등이 상속
public abstract class EquipableController : MonoBehaviour
{
    // 슬롯이 해제될 때 발생 (slotIndex) — 해당 슬롯의 UI가 표시를 정리하는 데 사용
    public event Action<int> OnSlotUnequipped;

    public abstract void Equip(ItemData data, int slotIndex);
    public abstract void Unequip(int slotIndex);

    // 장착된 모든 슬롯을 해제한다 (사망 등 일괄 정리 시)
    public abstract void UnequipAll();

    // 해당 슬롯에 장착된 아이템 ID (없으면 0) — UI가 실제 상태를 동기화할 때 사용
    public abstract int GetEquippedId(int slotIndex);

    // 해제 가능 여부 (해제 시 인벤토리 반환 전에 호출됨)
    public virtual bool CanUnequip(int slotIndex) => true;

    // 서브클래스가 슬롯 해제 후 호출
    protected void RaiseUnequipped(int slotIndex) => OnSlotUnequipped?.Invoke(slotIndex);
}
