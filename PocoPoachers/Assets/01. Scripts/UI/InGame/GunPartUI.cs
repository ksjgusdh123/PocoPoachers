using System.Collections.Generic;
using UnityEngine;

// 총기 파츠 장착 패널. 무기 우클릭 → "파츠 장착"으로 열린다(PlayerController가 Open 호출).
// 씬에 비활성으로 둬도 됨 — 활성 객체가 Open을 부르면 그때 켜진다.
// 슬롯(GunPartDropHandler)은 SlotType별로 프리팹에 고정 배치.
public class GunPartUI : MonoBehaviour
{
    private GunPartDropHandler[] _slots;

    private void Awake() => CacheSlots();

    // 해당 총으로 패널을 연다. 그 총이 지원하는 슬롯만 켜고 SetGun.
    public void Open(GunBase gun)
    {
        if (gun == null) return;

        gameObject.SetActive(true);
        CacheSlots();

        HashSet<SlotType> supported = GetSupportedSlots(gun);
        foreach (GunPartDropHandler slot in _slots)
        {
            bool ok = supported.Contains(slot.SlotType);
            slot.gameObject.SetActive(ok);
            if (ok)
                slot.SetGun(gun);
        }
    }

    // 닫기 버튼에 연결
    public void Close() => gameObject.SetActive(false);

    private void CacheSlots()
    {
        if (_slots == null)
            _slots = GetComponentsInChildren<GunPartDropHandler>(true);
    }

    // 이 총이 지원하는 슬롯 = 호환되는 파츠들의 slot_type 집합
    private static HashSet<SlotType> GetSupportedSlots(GunBase gun)
    {
        var set = new HashSet<SlotType>();
        GunType type = gun.Stat.GunType;
        foreach (GunPartData part in GunPartTable.Instance.All)
            if (GunPartUtil.IsCompatible(part, type))
                set.Add(part.slot_type);
        return set;
    }
}
