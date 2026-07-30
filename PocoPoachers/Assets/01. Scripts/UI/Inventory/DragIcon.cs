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
        // Canvas 바로 아래로 끌어올리고, 자체 정렬 순서를 패널 범위보다 높게 잡아
        // UIManager가 패널 sortingOrder를 올려도 항상 위에 그려지도록 한다.
        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            transform.SetParent(canvas.transform, false);

        ApplyOverlaySorting();

        gameObject.SetActive(false);
    }

    // 씬 전환으로 파괴된 뒤 Instance가 파괴된 오브젝트를 가리키면
    // SlotClickHandler 등에서 Hide() 호출 시 MissingReferenceException이 난다.
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void ApplyOverlaySorting()
    {
        if (!TryGetComponent(out Canvas ownCanvas))
            ownCanvas = gameObject.AddComponent<Canvas>();

        ownCanvas.overrideSorting = true;
        ownCanvas.sortingOrder = UIManager.OverlaySortingOrder;

        if (_icon != null) _icon.raycastTarget = false;
    }

    public void Show(Sprite sprite, Vector2 position, int count)
    {
        _icon.sprite = sprite;
        _count.text = count.ToString();
        transform.position = position;
        gameObject.SetActive(true);
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
