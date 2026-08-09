using UnityEngine;

// 미니맵 패널. DialogueUI와 동일한 방식으로, 자체 배경 딤머(_backgroundDim)를
// 열릴 때 켜고 닫힐 때 끈다 (UIManager의 공용 딤머는 쓰지 않는다).
public class MinimapUI : UIBase
{
    [Tooltip("미니맵이 열릴 때 같이 켜지는 배경(화면 전체를 덮는 어두운 이미지 등). 씬/프리팹에서 직접 배치해서 연결.")]
    [SerializeField] private GameObject _backgroundDim;

    [SerializeField] private MinimapMarkerSpawner _markerSpawner;
    [SerializeField] private MinimapZoomPan _zoomPan;

    protected override UIType UiType => UIType.Minimap;

    // 상시 표시 HUD라 블러를 깔면 게임 내내 화면을 덮는다.
    protected override bool UseBackdropBlurByDefault => false;

    protected override void OnShow()
    {
        if (_backgroundDim != null) _backgroundDim.SetActive(true);
        _markerSpawner?.Refresh();
        _zoomPan?.ResetView();
    }

    protected override void OnHide()
    {
        if (_backgroundDim != null) _backgroundDim.SetActive(false);
    }
}
