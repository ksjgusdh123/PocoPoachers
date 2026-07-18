using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Canvas 최상단에 배치 - 드래그 중 마우스를 따라다니는 아이콘
public class DragIcon : MonoBehaviour
{
    public static DragIcon Instance { get; private set; }

    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _count;

    private void Awake()
    {
        Instance = this;

        // 실제로는 PlayerBagUI 안쪽에 중첩돼 있어 다른 패널(발전기 등)에 가려질 수 있다.
        // Canvas 바로 아래로 끌어올려야 형제 순서만으로 항상 맨 위에 그려진다.
        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            transform.SetParent(canvas.transform, false);

        gameObject.SetActive(false);
    }

    public void Show(Sprite sprite, Vector2 position, int count)
    {
        _icon.sprite = sprite;
        _count.text = count.ToString();
        transform.position = position;
        gameObject.SetActive(true);
        transform.SetAsLastSibling(); // 이후에 열리는 다른 UI 패널에도 가려지지 않도록 항상 맨 위로
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        _icon.sprite = null;
        _count.text = "";
    }

    public void UpdatePosition(Vector2 position)
    {
        transform.position = position;
    }
}
