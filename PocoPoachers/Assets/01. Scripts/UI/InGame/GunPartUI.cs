using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 총기 파츠 장착 패널. 무기 우클릭 → "파츠 장착"으로 열린다(PlayerController가 Open 호출).
// 씬에 비활성으로 둬도 됨 — 활성 객체가 Open을 부르면 그때 켜진다.
// 슬롯(GunPartDropHandler)은 SlotType별로 프리팹에 고정 배치.
public class GunPartUI : MonoBehaviour
{
    [SerializeField] private Button _closeButton;
    [SerializeField] private Image _gunImage;

    private GunPartDropHandler[] _slots;
    private GunBase _gun;   // 현재 패널이 다루는 총 (uid=_gun.Uid, 데이터=_gun.Stat)
    private GunBase _previewGun;   // 인벤토리(미장착) 총을 볼 때 임시 생성, Close에서 파괴

    private void Awake()
    {
        CacheSlots();
        if (_closeButton != null)
            _closeButton.onClick.AddListener(Close);
    }

    private void OnEnable()
    {
        WeaponController.OnWeaponChanged += OnWeaponChanged;
        GunBase.OnGunStateSynced += OnGunStateSynced;
    }

    private void OnDisable()
    {
        WeaponController.OnWeaponChanged -= OnWeaponChanged;
        GunBase.OnGunStateSynced -= OnGunStateSynced;
    }

    // 게스트: 호스트가 프리뷰 총의 파츠 상태(H_GunState)를 돌려주면 패널을 다시 바인딩해 반영한다
    private void OnGunStateSynced(int uid)
    {
        if (_previewGun != null && _previewGun.Uid == uid)
            Open(_previewGun);
    }

    // 무기 해제(data == null) 시 열려 있던 파츠 패널을 닫는다
    private void OnWeaponChanged(int slotIndex, ItemData data)
    {
        if (data == null) Close();
    }

    // 해당 총으로 패널을 연다. 지원 슬롯만 켜고, 각 슬롯에 총+장착 파츠를 Bind.
    public void Open(GunBase gun)
    {
        if (gun == null) return;

        // 실장착 총으로 다시 열릴 때 이전 프리뷰가 남아있으면 정리
        if (gun != _previewGun) DestroyPreview();

        _gun = gun;
        gameObject.SetActive(true);
        CacheSlots();
        ShowGunImage(gun);

        HashSet<SlotType> supported = GetSupportedSlots(gun);
        foreach (GunPartDropHandler slot in _slots)
        {
            bool ok = supported.Contains(slot.SlotType);
            slot.gameObject.SetActive(ok);
            if (ok)
                slot.Bind(gun);   // 총 지정 + 이미 장착된 파츠 표시
        }
    }

    // 인벤토리(미장착) 무기용 — 임시 총을 만들어 저장된 파츠를 복원한 뒤 패널을 연다.
    // 파츠 변경은 uid 기준으로 저장되므로, 이후 이 총을 장착하면 그대로 복원된다.
    public void OpenForItem(int itemId, int uid)
    {
        ItemData data = ItemTable.Instance.Get(itemId);
        if (data == null) return;

        GunBase gun = ResourceManager.Instance.Spawn<GunBase>(data.prefab, transform);
        if (gun == null) return;

        gun.SetUid(uid);
        gun.gameObject.SetActive(false);   // 시뮬레이션/렌더 없이 데이터 용도로만 사용

        // 호스트/솔로는 로컬 WEM에서 바로 복원. 게스트는 WEM을 신뢰할 수 없어 호스트에 요청 →
        // H_GunState 응답이 OnGunStateSynced로 들어와 프리뷰 총을 갱신한다.
        if (RoomManager.IsHost)
            RestorePartsFromWEM(gun, uid);

        DestroyPreview();   // 이전 프리뷰가 남아있으면 정리
        _previewGun = gun;
        Open(gun);

        if (!RoomManager.IsHost)
            RoomSync.RequestGunState(uid);
    }

    // WorldEquipmentManager에 저장된 파츠를 총에 복원한다 (호스트 권위) — EquipPart가 스탯을 재계산한다
    private static void RestorePartsFromWEM(GunBase gun, int uid)
    {
        foreach (var kv in WorldEquipmentManager.GetParts(uid))
        {
            GunPartData part = GunPartTable.Instance.Get(kv.Value);
            if (part == null) continue;

            int partUid = WorldEquipmentManager.GetPartUid(uid, kv.Key);
            gun.EquipPart(WorldEquipmentManager.GetEnhancedGunPart(part, partUid));
        }
    }

    public void Close()
    {
        gameObject.SetActive(false);
        DestroyPreview();
    }

    private void DestroyPreview()
    {
        if (_previewGun == null) return;
        Destroy(_previewGun.gameObject);
        _previewGun = null;
    }

    private void ShowGunImage(GunBase gun)
    {
        if (_gunImage == null) return;

        ItemData data = ItemTable.Instance.Get(gun.ItemId);
        _gunImage.sprite = data != null ? ResourceManager.Instance.LoadSprite(data.icon) : null;
        _gunImage.enabled = _gunImage.sprite != null;
    }

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
