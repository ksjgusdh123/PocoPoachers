using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class EquipContextMenuUI : UIBase
{
    [SerializeField] private Button _unequipButton;
    [SerializeField] private Vector2 _offset = new Vector2(100f, 0f);

    private ItemHolderDropHandler _targetHandler;
    private RectTransform _rectTransform;

    protected override UIType UiType => UIType.EquipContextMenu;

    protected override void Awake()
    {
        base.Awake();

        _rectTransform = GetComponent<RectTransform>();
        _unequipButton.onClick.AddListener(OnClickUnequip);

        SlotInteractionManager.GetInstance().OnEquipRightClick += ShowAt;
    }

    private void OnDisable()
    {
        _targetHandler = null;
    }

    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            if (!RectTransformUtility.RectangleContainsScreenPoint(_rectTransform, mousePos))
                Hide();
        }
    }

    private void ShowAt(ItemHolderDropHandler handler)
    {
        _targetHandler = handler;
        transform.position = handler.transform.position + (Vector3)_offset;
        Show();
    }

    private void OnClickUnequip()
    {
        _targetHandler?.Unequip();
        Hide();
    }
}
