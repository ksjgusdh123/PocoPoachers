// 아이템 사용 / 재장전을 하나의 게이지 바로 처리하는 통합 진행 UI.
// 두 이벤트 소스를 모두 구독해 같은 슬라이더를 채운다.
public class ProgressUI : ProgressUIBase
{
    protected override void Subscribe()
    {
        PlayerController.OnUseStarted   += StartFilling;
        PlayerController.OnUseCancelled += StopFilling;
        GunBase.OnReloadStarted += StartFilling;
        GunBase.OnReloadEnded   += StopFilling;
        BaseOre.OnMineStarted += StartFilling;
        BaseOre.OnMineEnded   += StopFilling;
    }

    protected override void Unsubscribe()
    {
        PlayerController.OnUseStarted   -= StartFilling;
        PlayerController.OnUseCancelled -= StopFilling;
        GunBase.OnReloadStarted -= StartFilling;
        GunBase.OnReloadEnded   -= StopFilling;
        BaseOre.OnMineStarted -= StartFilling;
        BaseOre.OnMineEnded   -= StopFilling;
    }
}
