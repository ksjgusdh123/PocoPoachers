using UnityEngine;

public class ReloadProgressUI : ProgressUI
{
    private void Awake()
    {
        GunBase.OnReloadStarted += StartFilling;
        GunBase.OnReloadEnded += StopFilling;
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        GunBase.OnReloadStarted -= StartFilling;
        GunBase.OnReloadEnded -= StopFilling;
    }
}
