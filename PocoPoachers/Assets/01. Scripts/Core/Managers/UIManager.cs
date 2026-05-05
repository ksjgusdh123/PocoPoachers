using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    CrosshairUI _crosshairUI;

    protected override void Awake()
    {
        _crosshairUI = FindAnyObjectByType<CrosshairUI>();
    }

    public void ChangeMouseCursor(bool isCrosshair)
    {
        _crosshairUI.SetGameMode(isCrosshair);
    }
}
