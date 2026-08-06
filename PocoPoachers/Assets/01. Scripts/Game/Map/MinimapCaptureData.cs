using UnityEngine;

// MinimapCaptureWindow가 스크린샷과 함께 저장하는 촬영 범위 정보.
// 런타임에서 월드 좌표를 미니맵 이미지 안의 상대 위치(0~1)로 변환할 때 쓴다.
[CreateAssetMenu(menuName = "Minimap/Minimap Capture Data")]
public class MinimapCaptureData : ScriptableObject
{
    public Texture2D MinimapTexture;
    public Vector3 WorldCenter; // Y는 안 씀, X/Z만 유효
    public float WorldSize;     // 정사각형 촬영 폭 (한 변, m)
}
