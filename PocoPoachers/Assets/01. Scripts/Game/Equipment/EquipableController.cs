using UnityEngine;

// 장착 가능한 장비 컨트롤러의 공통 기반
// WeaponController, ArmorController 등이 상속
public abstract class EquipableController : MonoBehaviour
{
    public abstract void Equip(ItemData data, int slotIndex);
    public abstract void Unequip(int slotIndex);

    // 해제 가능 여부 (해제 시 인벤토리 반환 전에 호출됨)
    public virtual bool CanUnequip(int slotIndex) => true;
}
