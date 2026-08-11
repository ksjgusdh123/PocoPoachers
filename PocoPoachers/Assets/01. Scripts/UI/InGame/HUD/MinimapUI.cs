using UnityEngine;

// 미니맵 패널. 뒤를 덮는 딤머는 UIManager의 공용 딤머를 쓴다(인스펙터의 Use Shared Dimmer).
public class MinimapUI : UIBase
{
    [SerializeField] private MinimapMarkerSpawner _markerSpawner;
    [SerializeField] private MinimapZoomPan _zoomPan;

    protected override UIType UiType => UIType.Minimap;

    // 상시 표시 HUD라 블러를 깔면 게임 내내 화면을 덮는다.
    protected override bool UseBackdropBlurByDefault => false;

    protected override void OnShow()
    {
        _markerSpawner?.Refresh();
        _zoomPan?.ResetView();
    }
}
