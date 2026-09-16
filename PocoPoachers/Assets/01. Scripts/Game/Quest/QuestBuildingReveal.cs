using System.Collections;
using UnityEngine;

// 퀘스트를 완료하기 전에는 건물을 숨겨두고, 완료되면 건설 연출 뒤에 나타나게 한다.
// 숨긴 건물 안의 스크립트는 멈추므로 이 컴포넌트는 건물 바깥(항상 켜져 있는 부모 등)에 붙인다.
// 연출 여부를 따로 저장하면 퀘스트 진행(씬 이동 시 저장)과 어긋나므로 퀘스트 상태만으로 판단한다.
public class QuestBuildingReveal : MonoBehaviour
{
    [SerializeField] private int _questId = 9101;
    [SerializeField] private GameObject _building;

    [Header("연출")]
    [SerializeField] private float _buildDuration = 3f;
    [SerializeField] private float _appearDuration = 0.4f;

    [Tooltip("건설 중에 재생할 이펙트. 비우면 코드로 만든 기본 먼지 파티클을 쓴다")]
    [SerializeField] private GameObject _buildEffectPrefab;

    [SerializeField] private string _buildSfxKey;
    [SerializeField] private string _completeSfxKey;

    [Header("완료한 사람의 연출 (대화창을 숨기고 카메라로 건물을 비춤)")]
    [Tooltip("건물이 나타난 뒤 대화창을 다시 띄워 보여줄 대사 ID (0이면 퀘스트 목록으로 바로 돌아감)")]
    [SerializeField] private int _afterBuildDialogueId;
    [SerializeField] private float _cameraMoveTime = 1.2f;
    [Tooltip("건물이 나타난 뒤 카메라가 머무는 시간(초)")]
    [SerializeField] private float _holdTime = 1f;

    private bool _revealing;

    private void Awake()
    {
        if (_building == null)
        {
            Debug.LogWarning("[QuestBuildingReveal] 건물 오브젝트가 비어 있습니다.", this);
            enabled = false;
            return;
        }

        if (transform.IsChildOf(_building.transform))
            Debug.LogWarning("[QuestBuildingReveal] 건물 자신에게 붙이면 숨길 때 이 컴포넌트도 멈춥니다. 부모에 붙여주세요.", this);

        _building.SetActive(false);
    }

    private void OnEnable() => QuestManager.OnQuestStateChanged += HandleQuestStateChanged;
    private void OnDisable() => QuestManager.OnQuestStateChanged -= HandleQuestStateChanged;

    // 씬에 들어올 때 이미 완료돼 있으면 연출 없이 바로 보여준다
    private void Start()
    {
        if (QuestManager.GetState(_questId) == QuestState.Completed) _building.SetActive(true);
    }

    // 플레이 중에 완료되는 순간에만 연출한다
    private void HandleQuestStateChanged(int questId, QuestState state)
    {
        if (questId != _questId || state != QuestState.Completed) return;
        if (_revealing || _building.activeSelf) return;

        // 대화창에서 완료한 사람이면 여기서 바로 연출을 시작해야 한다.
        // 완료 요청 직후 대화창이 목록으로 넘어가는데, 남은 퀘스트가 없으면 그대로 닫혀버리기 때문이다.
        var dialogue = FindAnyObjectByType<DialogueUI>(FindObjectsInactive.Include);
        if (dialogue != null && dialogue.IsCompletingQuest(_questId))
        {
            dialogue.BeginCutscene();
            StartCoroutine(CutsceneRoutine(dialogue));
            return;
        }

        StartCoroutine(RevealRoutine());
    }

    private IEnumerator CutsceneRoutine(DialogueUI dialogue)
    {
        _revealing = true;

        var cameraController = FindAnyObjectByType<CameraController>();
        cameraController?.SetFocusPoint(_building.transform.position);
        yield return new WaitForSeconds(_cameraMoveTime);

        yield return BuildRoutine();
        yield return new WaitForSeconds(_holdTime);

        cameraController?.SetFocusPoint(null);
        yield return new WaitForSeconds(_cameraMoveTime);

        dialogue.EndCutscene(_afterBuildDialogueId);
        _revealing = false;
    }

    private IEnumerator RevealRoutine()
    {
        _revealing = true;

        // 다른 사람이 완료했거나 씬에 들어와서 보는 경우 — 대화 중이면 닫힌 뒤에 보여준다
        while (UIManager.GetInstance().IsOpen(UIType.Dialogue)) yield return null;

        yield return BuildRoutine();
        _revealing = false;
    }

    private IEnumerator BuildRoutine()
    {
        Vector3 position = _building.transform.position;
        GameObject effect = _buildEffectPrefab != null
            ? Instantiate(_buildEffectPrefab, position, Quaternion.identity)
            : CreateDefaultDust(position);
        SoundManager.GetInstance().PlaySfxAt(_buildSfxKey, position);

        yield return new WaitForSeconds(_buildDuration);

        StopEffect(effect);
        SoundManager.GetInstance().PlaySfxAt(_completeSfxKey, position);
        yield return Appear();
    }

    // 살짝 크게 튀어나왔다가 제자리로 돌아온다 (easeOutBack)
    private IEnumerator Appear()
    {
        Transform target = _building.transform;
        Vector3 scale = target.localScale;
        target.localScale = Vector3.zero;
        _building.SetActive(true);

        for (float elapsed = 0f; elapsed < _appearDuration; elapsed += Time.deltaTime)
        {
            float t = elapsed / _appearDuration - 1f;
            target.localScale = scale * (1f + 2.70158f * t * t * t + 1.70158f * t * t);
            yield return null;
        }

        target.localScale = scale;
    }

    private static void StopEffect(GameObject effect)
    {
        if (effect == null) return;

        foreach (var particle in effect.GetComponentsInChildren<ParticleSystem>())
            particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        // 이미 나온 먼지가 사라질 시간을 준다
        Destroy(effect, 3f);
    }

    // 이펙트 프리팹이 없을 때 테스트용 기본 먼지. Shader.Find는 빌드에서 셰이더가 빠질 수 있어 실제 사용 시에는 프리팹을 지정할 것.
    private static GameObject CreateDefaultDust(Vector3 position)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) return null;

        var go = new GameObject("BuildDust");
        go.transform.SetPositionAndRotation(position, Quaternion.Euler(-90f, 0f, 0f));

        var particle = go.AddComponent<ParticleSystem>();
        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = particle.main;
        main.duration = 1f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startColor = new Color(0.75f, 0.68f, 0.58f, 0.6f);
        main.gravityModifier = -0.05f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = particle.emission;
        emission.rateOverTime = 25f;

        var shape = particle.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 1.5f;

        var colorOverLifetime = particle.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = gradient;

        var material = new Material(shader);
        material.SetFloat("_Surface", 1f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        go.GetComponent<ParticleSystemRenderer>().material = material;

        particle.Play();
        return go;
    }
}
