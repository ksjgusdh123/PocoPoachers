// 아이템 사용 게이지 슬라이더 UI
// PlayerController의 OnUseStarted / OnUseCancelled 이벤트를 구독해 동작한다
public class ItemUseProgressUI : ProgressUIBase
{
    protected override void Subscribe()
    {
        PlayerController.OnUseStarted += StartFilling;
        PlayerController.OnUseCancelled += StopFilling;
    }

    protected override void Unsubscribe()
    {
        PlayerController.OnUseStarted -= StartFilling;
        PlayerController.OnUseCancelled -= StopFilling;
    }
}
