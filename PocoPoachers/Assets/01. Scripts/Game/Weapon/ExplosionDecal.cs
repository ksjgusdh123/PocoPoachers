using UnityEngine;
using UnityEngine.Rendering.Universal;

// 수류탄 폭발 자국(Decal Projector)의 수명 관리. 일정 시간 유지된 뒤 fadeFactor를 줄여가며 사라지고 파괴된다.
[RequireComponent(typeof(DecalProjector))]
public class ExplosionDecal : MonoBehaviour
{
    [SerializeField] private float lifetime = 6f;
    [SerializeField] private float fadeDuration = 1.5f;

    private DecalProjector _projector;
    private float _elapsed;

    private void Awake()
    {
        _projector = GetComponent<DecalProjector>();
        _projector.fadeFactor = 1f;
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;

        float fadeStart = lifetime - fadeDuration;
        if (_elapsed >= fadeStart)
        {
            float t = fadeDuration > 0f ? Mathf.Clamp01((_elapsed - fadeStart) / fadeDuration) : 1f;
            _projector.fadeFactor = 1f - t;
        }

        if (_elapsed >= lifetime)
            Destroy(gameObject);
    }
}
