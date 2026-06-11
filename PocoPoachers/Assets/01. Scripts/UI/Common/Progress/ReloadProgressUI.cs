public class ReloadProgressUI : ProgressUIBase
{
    protected override void Subscribe()
    {
        GunBase.OnReloadStarted += StartFilling;
        GunBase.OnReloadEnded += StopFilling;
    }

    protected override void Unsubscribe()
    {
        GunBase.OnReloadStarted -= StartFilling;
        GunBase.OnReloadEnded -= StopFilling;
    }
}
