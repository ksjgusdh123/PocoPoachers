using UnityEngine;

public abstract class UIBase : MonoBehaviour
{
    protected abstract UIType UiType { get; }

    protected virtual void Awake()
    {
        RegisterSelf();
        gameObject.SetActive(false);
    }

    protected virtual void OnDestroy()
    {
        if (UIManager.GetInstance() == null) return;
        UnregisterSelf();
    }

    protected virtual void RegisterSelf()   => UIManager.GetInstance().Register(UiType, gameObject);
    protected virtual void UnregisterSelf() => UIManager.GetInstance().Unregister(UiType);

    public void Show()   => UIManager.GetInstance().Show(UiType);
    public void Hide()   => UIManager.GetInstance().Hide(UiType);
    public void Toggle() => UIManager.GetInstance().Toggle(UiType);
}
