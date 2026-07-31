using UnityEngine;

// 인벤토리/장비 슬롯 호버 시 뜨는 툴팁. 표시 로직은 전부 base(ItemInfoPanel)에 있고,
// 이 타입은 "호버 툴팁"을 특정하기 위한 구분용이다.
// 호버 핸들러들이 FindAnyObjectByType<DescriptionUI>로 이 인스턴스만 찾도록 하기 위해
// 고정 패널(GunPartPanelUI 등)과 다른 타입으로 분리해 둔다.
public class DescriptionUI : ItemInfoPanel
{
    private GunPartPanelUI _gunPartPanel;
    private MiniDescriptionUI _miniDescriptionUI;

    protected override void Show(ItemData data, int uid)
    {
        // 파츠 패널이 떠 있는 동안엔 일반 툴팁을 띄우지 않는다.
        // 단, 인벤토리의 파츠 아이템은 파츠 전용 미니 툴팁으로 대신 표시한다.
        if (IsGunPartPanelOpen())
        {
            if (data != null && data.ItemType == ItemType.GunPart)
                MiniPanel()?.ShowDescription(data, uid);
            return;
        }

        base.Show(data, uid);
    }

    // 인벤토리 호버 이탈 시 이 툴팁을 끄는데, 파츠 패널 중엔 미니로 위임했으므로 미니도 함께 정리한다.
    // (미표시 상태면 SetActive(false)라 no-op)
    public override void HideDescription()
    {
        if (_miniDescriptionUI != null)
            _miniDescriptionUI.HideDescription();

        base.HideDescription();
    }

    private bool IsGunPartPanelOpen()
    {
        _gunPartPanel ??= FindAnyObjectByType<GunPartPanelUI>(FindObjectsInactive.Include);
        return _gunPartPanel != null && _gunPartPanel.gameObject.activeInHierarchy;
    }

    private MiniDescriptionUI MiniPanel() =>
        _miniDescriptionUI ??= FindAnyObjectByType<MiniDescriptionUI>(FindObjectsInactive.Include);
}
