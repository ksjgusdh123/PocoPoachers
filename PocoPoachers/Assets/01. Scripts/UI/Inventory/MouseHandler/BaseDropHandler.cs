using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public abstract class BaseDropHandler : MonoBehaviour, IDropHandler
{
    private RectTransform _rectTransform;

    protected virtual void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        var manager = SlotInteractionManager.GetInstance();
        if (manager.DraggedSlot == null) return;
        if (!HandleDrop(manager))
        {
            _rectTransform.DOKill();
            _rectTransform.DOShakeAnchorPos(0.4f, strength: new Vector2(10f, 0f), vibrato: 20, randomness: 0);
        }
    }

    protected abstract bool HandleDrop(SlotInteractionManager manager);
}
