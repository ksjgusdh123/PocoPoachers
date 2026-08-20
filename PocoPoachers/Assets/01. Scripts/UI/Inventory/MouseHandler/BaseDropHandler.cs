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
        if (HandleDrop(manager))
        {
            InvokeDropSucceeded(manager);
        }
        else
        {
            manager.InvokeItemPlaceFailed();
            _rectTransform.DOKill();
            _rectTransform.DOShakeAnchorPos(0.4f, strength: new Vector2(10f, 0f), vibrato: 20, randomness: 0);
        }
    }

    protected abstract bool HandleDrop(SlotInteractionManager manager);

    // 놓기 성공 피드백 — 다른 소리를 내야 하는 슬롯이 override 한다
    protected virtual void InvokeDropSucceeded(SlotInteractionManager manager) => manager.InvokeItemPlaced();
}
