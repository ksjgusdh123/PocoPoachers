using UnityEngine;

// 미니맵 가장자리 페이드(Custom/UI-EdgeFade) 파라미터.
// 기본값은 M_UIMapEdgeFade.mat의 현재 값 — 새 맵을 추가해도 기존과 같은 모양으로 시작한다.
[System.Serializable]
public class MinimapEdgeStyle
{
    [Header("Shape")]
    [Range(0f, 1f)] public float CenterX = 0.5f;
    [Range(0f, 1f)] public float CenterY = 0.5f;
    [Range(0.05f, 1.2f)] public float RadiusX = 0.51f;
    [Range(0.05f, 1.2f)] public float RadiusY = 0.52f;
    [Range(0.01f, 1f)] public float Softness = 0.207f;

    [Header("Wobble")]
    [Range(0f, 0.6f)] public float WobbleAmount = 0.123f;
    [Range(1f, 8f)] public float WobbleFrequency = 7.35f;
    [Range(0f, 10f)] public float WobbleSeed = 10f;

    // 슬라이더로 못 맞추는 모양은 흑백 마스크로 직접 그려 넣는다 (흰색 = 보임). 비우면 슬라이더 값만 쓴다.
    [Header("Optional Mask")]
    public Texture2D Mask;

    [Header("Outline")]
    public Color OutlineColor = new(0.35220122f, 0.17249416f, 0.15173447f, 0.34117648f);
    [Range(1f, 16f)] public float OutlineSharpness = 2.8f;
    [Range(0f, 1f)] public float OutlineGlow = 0.185f;
}

// MinimapCaptureWindow가 스크린샷과 함께 저장하는 맵 하나분의 미니맵 정보.
// 런타임에서 월드 좌표를 미니맵 이미지 안의 상대 위치(0~1)로 변환할 때 쓴다.
[CreateAssetMenu(menuName = "Minimap/Minimap Capture Data")]
public class MinimapCaptureData : ScriptableObject
{
    public string SceneName;    // MinimapCatalog가 이 이름으로 현재 씬의 데이터를 찾는다
    public Texture2D MinimapTexture;
    public Sprite MinimapSprite; // Image에 물리는 건 Texture2D가 아니라 Sprite다
    public Vector3 WorldCenter; // Y는 안 씀, X/Z만 유효
    public float WorldSize;     // 정사각형 촬영 폭 (한 변, m)

    public MinimapEdgeStyle EdgeStyle = new();
}
