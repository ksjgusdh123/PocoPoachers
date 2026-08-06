using System.Collections;
using UnityEngine;

// 잔상 오브젝트 자신에게 붙어서 페이드/파괴를 진행한다.
// RescueBeamEffect(포드) 위에서 코루틴을 돌리면 포드가 먼저 Destroy될 때 코루틴도 같이 끊겨
// 잔상이 페이드 도중 멈춘 채 안 사라지는 문제가 있어 분리했다.
public class RescuePodGhostFade : MonoBehaviour
{
    private Material _material;
    private float _duration;

    public void Init(Material material, float duration)
    {
        _material = material;
        _duration = duration;
        StartCoroutine(FadeAndDestroy());
    }

    private IEnumerator FadeAndDestroy()
    {
        float t = 0f;
        Color start = _material.color;
        while (t < _duration)
        {
            t += Time.deltaTime;
            _material.color = new Color(start.r, start.g, start.b, Mathf.Lerp(start.a, 0f, t / _duration));
            yield return null;
        }
        Destroy(gameObject);
    }
}
