using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class EquipContextMenuUI : MonoBehaviour
{
    [SerializeField] private Button _unequipButton;
    [SerializeField] private Vector2 _offset = new Vector2(100f, 0f);

    private ItemHolderDropHandler _targetHandler;
    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _unequipButton.onClick.AddListener(OnClickUnequip);

        UIManager.GetInstance().Register(UIType.EquipContextMenu, gameObject);
        gameObject.SetActive(false);
        SlotInteractionManager.GetInstance().OnEquipRightClick += Show;
    }

    private void OnDisable()
    {
        _targetHandler = null;
    }

    private void OnDestroy()
    {
        var ui = UIManager.GetInstance();
        if (ui != null) ui.Unregister(UIType.EquipContextMenu);
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

    private void Show(ItemHolderDropHandler handler)
    {
        _targetHandler = handler;
        transform.position = handler.transform.position + (Vector3)_offset;
        UIManager.GetInstance().Show(UIType.EquipContextMenu);
    }

    private void Hide()
    {
        UIManager.GetInstance().Hide(UIType.EquipContextMenu);
    }

    private void OnClickUnequip()
    {
        _targetHandler?.Unequip();
        Hide();
    }
}
