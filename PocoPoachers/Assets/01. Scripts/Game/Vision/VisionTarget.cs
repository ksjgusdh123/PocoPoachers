using UnityEngine;

public class VisionTarget : MonoBehaviour
{
    [SerializeField] private bool _visibleOnStart;

    private Renderer[] _renderers;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        SetVisible(_visibleOnStart);
    }

    public void SetVisible(bool visible)
    {
        if (_renderers == null)
            _renderers = GetComponentsInChildren<Renderer>(true);

        foreach (var targetRenderer in _renderers)
            targetRenderer.enabled = visible;
    }
}
