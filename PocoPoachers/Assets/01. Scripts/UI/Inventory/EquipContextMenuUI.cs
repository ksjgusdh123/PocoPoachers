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
        gameObject.SetActive(false);
        _unequipButton.onClick.AddListener(OnClickUnequip);

        SlotInteractionManager.GetInstance().OnEquipRightClick += Show;
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
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        _targetHandler = null;
        gameObject.SetActive(false);
    }

    private void OnClickUnequip()
    {
        _targetHandler?.Unequip();
        Hide();
    }
}
