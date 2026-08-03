using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingSceneController : MonoBehaviour
{
    [SerializeField] private LoadingScreenUI _loadingScreenUI;
    [SerializeField] private float _minDisplayDuration = 3f; // 발사 연출까지 포함해서, 실제 로딩이 더 빨리 끝나도 이 시간만큼은 로딩씬을 유지한다
    [SerializeField] private IdleFloatMotion _rocket; // 비워두면 발사 연출 없이 바로 전환
    [SerializeField] private float _launchDuration = 0.5f; // PlayLaunch()에 넘기는 값과 맞춰야 함

    private void Start()
    {
        StartCoroutine(LoadTargetScene());
    }

    private IEnumerator LoadTargetScene()
    {
        string targetScene = SceneLoader.Instance.TargetSceneName;
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

        // 발사 연출 시간을 뺀 나머지를 최소 노출 시간으로 채운다 — 발사 연출이 3초 안에 포함되도록
        float launchDuration = _rocket != null ? _launchDuration : 0f;
        float remaining = _minDisplayDuration - launchDuration - (Time.unscaledTime - startTime);
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
