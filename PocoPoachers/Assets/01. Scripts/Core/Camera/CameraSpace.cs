using UnityEngine;

// 카메라가 Y축으로 돌아간 구도(마름모 뷰)에서는 이동 입력을 월드 축 그대로 쓰면
// W를 눌렀을 때 캐릭터가 화면상 대각선으로 간다. 입력을 카메라 yaw만큼 돌려
// "화면 위쪽 = W"가 되도록 맞춘다. yaw가 0이면 기존 동작과 완전히 같다.
public static class CameraSpace
{
    private static Camera _camera;

    public static float Yaw
    {
        get
        {
            // Camera.main은 태그 탐색이므로 캐싱하고, 씬 전환으로 교체되면 다시 잡는다
            if (_camera == null) _camera = Camera.main;
            return _camera != null ? _camera.transform.eulerAngles.y : 0f;
        }
    }

    // 월드 UI를 화면과 나란히 세우는 회전(빌보드). 카메라 각도를 바꿔도 UI가 따라온다.
    public static Quaternion Rotation
    {
        get
        {
            if (_camera == null) _camera = Camera.main;
            return _camera != null ? _camera.transform.rotation : Quaternion.identity;
        }
    }

    public static Vector3 InputToWorld(Vector2 input) =>
        Quaternion.Euler(0f, Yaw, 0f) * new Vector3(input.x, 0f, input.y);
}
