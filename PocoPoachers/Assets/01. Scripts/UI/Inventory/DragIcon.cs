using UnityEngine;
using UnityEngine.UI;

// Canvas 최상단에 배치 - 드래그 중 마우스를 따라다니는 아이콘
public class DragIcon : MonoBehaviour
{
    public static DragIcon Instance { get; private set; }

    [SerializeField] private Image _icon;

    private void Awake()
    {
        Instance = this;
        gameObject.SetActive(false);
    }

    public void Show(Sprite sprite, Vector2 position)
    {
        _icon.sprite = sprite;
        transform.position = position;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        _icon.sprite = null;
    }

    public void UpdatePosition(Vector2 position)
    {
        transform.position = position;
    }
}
