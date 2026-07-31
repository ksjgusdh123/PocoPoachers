using UnityEngine;

// 인벤토리/장비 슬롯 호버 시 뜨는 툴팁. 표시 로직은 전부 base(ItemInfoPanel)에 있고,
// 이 타입은 "호버 툴팁"을 특정하기 위한 구분용이다.
// 호버 핸들러들이 FindAnyObjectByType<DescriptionUI>로 이 인스턴스만 찾도록 하기 위해
// 고정 패널(GunPartPanelUI 등)과 다른 타입으로 분리해 둔다.
public class DescriptionUI : ItemInfoPanel
{
    private GunPartPanelUI _gunPartPanel;

    protected override void Show(ItemData data, int uid)
    {
        // 고정 파츠 패널이 떠 있으면 호버 툴팁은 띄우지 않는다 (둘은 같은 정보 영역을 공유하는 형제)
        _gunPartPanel ??= FindAnyObjectByType<GunPartPanelUI>(FindObjectsInactive.Include);
        if (_gunPartPanel != null && _gunPartPanel.gameObject.activeInHierarchy) return;

        base.Show(data, uid);
    }
}
