using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 총기 파츠 장착 패널. 무기 우클릭 → "파츠 장착"으로 열린다(PlayerController가 Open 호출).
// 씬에 비활성으로 둬도 됨 — 활성 객체가 Open을 부르면 그때 켜진다.
// 슬롯(GunPartDropHandler)은 SlotType별로 프리팹에 고정 배치.
public class GunPartUI : MonoBehaviour
{
    [SerializeField] private Button _closeButton;   // 눌리면 패널을 끈다

    private GunPartDropHandler[] _slots;
    private GunBase _gun;   // 현재 패널이 다루는 총 (uid=_gun.Uid, 데이터=_gun.Stat)

    private void Awake()
    {
        CacheSlots();
        if (_closeButton != null)
            _closeButton.onClick.AddListener(Close);
    }

    // 해당 총으로 패널을 연다. 지원 슬롯만 켜고, 각 슬롯에 총+장착 파츠를 Bind.
    public void Open(GunBase gun)
    {
        if (gun == null) return;

        _gun = gun;
        gameObject.SetActive(true);
        CacheSlots();

        HashSet<SlotType> supported = GetSupportedSlots(gun);
        foreach (GunPartDropHandler slot in _slots)
        {
            bool ok = supported.Contains(slot.SlotType);
            slot.gameObject.SetActive(ok);
            if (ok)
                slot.Bind(gun);   // 총 지정 + 이미 장착된 파츠 표시
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
