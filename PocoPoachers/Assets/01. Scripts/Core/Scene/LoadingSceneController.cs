using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingSceneController : MonoBehaviour
{
    [SerializeField] private LoadingScreenUI _loadingScreenUI;

    private void Start()
    {
        StartCoroutine(LoadTargetScene());
    }

    private IEnumerator LoadTargetScene()
    {
        string targetScene = SceneLoader.Instance.TargetSceneName;
        _loadingScreenUI.Begin();

        AsyncOperation op = SceneManager.LoadSceneAsync(targetScene);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            _loadingScreenUI.Report(op.progress / 0.9f);
            yield return null;
        }

        _loadingScreenUI.Report(1f);
        op.allowSceneActivation = true;
    }
}
