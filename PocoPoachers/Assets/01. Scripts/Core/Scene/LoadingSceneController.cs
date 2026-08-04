using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingSceneController : MonoBehaviour
{
    [SerializeField] private LoadingScreenUI _loadingScreenUI;
    [SerializeField] private float _minDisplayDuration = 3f; // 실제 로딩이 더 빨리 끝나도 이 시간만큼은 로딩씬을 유지한다 (발사 연출 시간은 별도로 더 붙음)
    [SerializeField] private IdleFloatMotion _rocket; // 비워두면 발사 연출 없이 바로 전환
    [SerializeField] private float _launchDuration = 0.5f; // PlayLaunch()에 넘기는 값과 맞춰야 함
    [SerializeField] private PassingMeteorSpawner _meteorSpawner; // 비워두면 방향 반전 안 함
    [SerializeField] private ImageScrollUI _scrollingBackground;  // 비워두면 방향 반전 안 함

    private void Start()
    {
        StartCoroutine(LoadTargetScene());
    }

    private IEnumerator LoadTargetScene()
    {
        string targetScene = SceneLoader.Instance.TargetSceneName;

        // Shelter로 돌아가는 길이면 로켓/운석/배경 모두 반대 방향(귀환)을, 아니면 원래 방향(출발)을 향하게 한다
        bool reversed = SceneName.IsShelter(targetScene);
        if (_rocket != null) _rocket.SetReversed(reversed);
        if (_meteorSpawner != null) _meteorSpawner.SetReversed(reversed);
        if (_scrollingBackground != null) _scrollingBackground.SetReversed(reversed);

        _loadingScreenUI.Begin();

        float startTime = Time.unscaledTime;

        AsyncOperation op = SceneManager.LoadSceneAsync(targetScene);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            _loadingScreenUI.Report(op.progress / 0.9f);
            yield return null;
        }

        _loadingScreenUI.Report(1f);

        // 로딩 완료든 최소 노출 시간이든, 더 늦게 끝나는 쪽까지 기다린다 (발사 연출은 그 다음)
        float remaining = _minDisplayDuration - (Time.unscaledTime - startTime);
        if (remaining > 0f)
            yield return new WaitForSecondsRealtime(remaining);

        // 씬이 실제로 전환되기 직전, 로켓이 앞으로 쭉 나가며 사라지는 발사 연출을 재생하고 끝날 때까지 기다린다
        if (_rocket != null)
        {
            bool launchDone = false;
            _rocket.PlayLaunch(_launchDuration).OnComplete(() => launchDone = true);
            yield return new WaitUntil(() => launchDone);
        }

        op.allowSceneActivation = true;
    }
}
