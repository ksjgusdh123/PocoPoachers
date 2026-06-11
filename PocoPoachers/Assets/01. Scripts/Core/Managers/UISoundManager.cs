using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UISoundManager : Singleton<UISoundManager>
{
    private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>();
    private Button _hoveredButton;
    private SlotInteractionManager _slotInteractionManager;

    protected override void Awake()
    {
        base.Awake();

        _slotInteractionManager = SlotInteractionManager.GetInstance();
        _slotInteractionManager.OnDragBegin += OnSlotDragBegin;
        _slotInteractionManager.OnItemPlaced += OnItemPlaced;
        _slotInteractionManager.OnItemPlaceFailed += OnItemPlaceFailed;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (_slotInteractionManager == null) return;
        _slotInteractionManager.OnDragBegin -= OnSlotDragBegin;
        _slotInteractionManager.OnItemPlaced -= OnItemPlaced;
        _slotInteractionManager.OnItemPlaceFailed -= OnItemPlaceFailed;
    }

    private void OnSlotDragBegin(ItemSlotUI _) => SoundManager.GetInstance().PlaySfx("ui_slot_click");

    private void OnItemPlaced() => SoundManager.GetInstance().PlaySfx("ui_item_place");

    private void OnItemPlaceFailed() => SoundManager.GetInstance().PlaySfx("ui_item_place_fail");

    private void Update()
    {
        if (EventSystem.current == null || Mouse.current == null) return;

        var pointerData = new PointerEventData(EventSystem.current) { position = Mouse.current.position.ReadValue() };
        _raycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, _raycastResults);

        Button button = null;
        if (_raycastResults.Count > 0)
        {
            var hit = _raycastResults[0].gameObject.GetComponentInParent<Button>();
            if (hit != null && hit.interactable) button = hit;
        }

        if (button != _hoveredButton)
        {
            if (button != null)
                SoundManager.GetInstance().PlaySfx("ui_hover");
            _hoveredButton = button;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame && button != null)
            SoundManager.GetInstance().PlayButtonClick();
    }
}
