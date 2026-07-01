using UnityEngine;

public class GunPartTester : MonoBehaviour
{
    [SerializeField] private int _partItemId;

    private GunBase _gun;

    private void Awake()
    {
        _gun = GetComponent<GunBase>();
    }

    [ContextMenu("파츠 장착")]
    private void Equip()
    {
        var part = GunPartTable.Instance.Get(_partItemId);
        if (part == null) { Debug.LogWarning($"파츠 없음: id={_partItemId}"); return; }
        _gun.EquipPart(part);
        Debug.Log($"[{_gun.Stat.GunType}] {part.slot_type} 장착 완료 — recoil={_gun.Stat.RecoilForce:F2}");
    }

    [ContextMenu("파츠 해제")]
    private void Unequip()
    {
        var part = GunPartTable.Instance.Get(_partItemId);
        if (part == null) return;
        _gun.UnequipPart(part.slot_type);
        Debug.Log($"[{_gun.Stat.GunType}] {part.slot_type} 해제 완료 — recoil={_gun.Stat.RecoilForce:F2}");
    }
}
