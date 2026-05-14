using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class EquipContextMenuUI : MonoBehaviour
{
    [SerializeField] private Button _unequipButton;

    private EquipDropHandler _targetHandler;
    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        gameObject.SetActive(false);
        _unequipButton.onClick.AddListener(OnClickUnequip);

        SlotInteractionManager.GetInstance().OnEquipRightClick += Show;
    }

    private void OnDestroy()
    {
        SlotInteractionManager.GetInstance().OnEquipRightClick -= Show;
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

    private void Show(EquipDropHandler handler, Vector2 screenPos)
    {
        _targetHandler = handler;
        transform.position = screenPos;
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
