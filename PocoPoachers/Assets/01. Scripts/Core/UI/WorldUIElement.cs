using UnityEngine;

public abstract class WorldUIElement : MonoBehaviour
{
    public WorldUIType UIType { get; private set; }

    private Transform _target;
    private Vector3 _offset;

    protected virtual void LateUpdate()
    {
        if (_target != null)
            transform.position = _target.position + _offset;
    }

    public void Init(Transform target, Vector3 offset, WorldUIType type)
    {
        _target = target;
        _offset = offset;
        UIType = type;
    }

    public void Release()
    {
        WorldUIManager.Instance.Return(this);
    }
}
