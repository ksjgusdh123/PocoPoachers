using UnityEngine;
using UnityEngine.UI;

// 오브젝트(RectTransform)는 움직이지 않고, RawImage의 UV만 계속 흘려서 안의 그림만 스크롤되는 것처럼 보이게 한다.
// 텍스처 Import 설정에서 Wrap Mode가 Repeat여야 이음매 없이 무한 반복된다.
[RequireComponent(typeof(RawImage))]
public class ImageScrollUI : MonoBehaviour
{
    [SerializeField] private Vector2 _scrollSpeed = new Vector2(0.05f, 0f); // 초당 UV 이동량 (0~1 기준, 부호로 방향 결정)

    private RawImage _rawImage;
    private float _directionSign = 1f; // Awake 타이밍과 무관하게 동작하도록, 원본 _scrollSpeed는 건드리지 않고 부호만 별도로 곱한다

    private void Awake()
    {
        _rawImage = GetComponent<RawImage>();
    }

    // 로딩 방향(예: Shelter로 돌아올 때 vs 나갈 때)에 따라 배경이 흐르는 방향을 반대로 뒤집는다
    public void SetReversed(bool reversed) => _directionSign = reversed ? -1f : 1f;

    private void Update()
    {
        Rect uv = _rawImage.uvRect;
        uv.position += _scrollSpeed * _directionSign * Time.unscaledDeltaTime;
        _rawImage.uvRect = uv;
    }
}
