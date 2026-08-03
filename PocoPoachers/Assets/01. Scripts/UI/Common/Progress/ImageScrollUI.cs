using UnityEngine;
using UnityEngine.UI;

// 오브젝트(RectTransform)는 움직이지 않고, RawImage의 UV만 계속 흘려서 안의 그림만 스크롤되는 것처럼 보이게 한다.
// 텍스처 Import 설정에서 Wrap Mode가 Repeat여야 이음매 없이 무한 반복된다.
[RequireComponent(typeof(RawImage))]
public class ImageScrollUI : MonoBehaviour
{
    [SerializeField] private Vector2 _scrollSpeed = new Vector2(0.05f, 0f); // 초당 UV 이동량 (0~1 기준, 부호로 방향 결정)

    private RawImage _rawImage;

    private void Awake()
    {
        _rawImage = GetComponent<RawImage>();
    }

    private void Update()
    {
        Rect uv = _rawImage.uvRect;
        uv.position += _scrollSpeed * Time.unscaledDeltaTime;
        _rawImage.uvRect = uv;
    }
}
