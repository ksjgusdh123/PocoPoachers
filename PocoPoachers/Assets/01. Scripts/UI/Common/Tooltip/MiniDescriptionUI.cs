// 파츠 패널(GunPartPanelUI) 전용 호버 툴팁. 표시 로직은 전부 base(ItemInfoPanel)에 있고,
// 이 타입은 "파츠 전용 호버 툴팁"을 특정하기 위한 구분용이다.
// 파츠 슬롯 핸들러(GunPartDropHandler)가 FindAnyObjectByType<MiniDescriptionUI>로 이 인스턴스만 찾는다.
// 일반 DescriptionUI는 파츠 패널이 떠 있으면 억제되므로, 그 자리를 이 툴팁이 대신한다.
public class MiniDescriptionUI : ItemInfoPanel
{
    private void Awake() => ApplyAlwaysOnTopSorting();
}
