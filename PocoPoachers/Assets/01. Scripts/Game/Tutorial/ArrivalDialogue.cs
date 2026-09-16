using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 씬 이동 후 도착한 씬에서 대사를 자동으로 연다 (튜토리얼을 마치고 쉘터에 도착했을 때 박사의 호출 등).
// ScreenWakeUp과 같은 방식 — 씬 이동 쪽에서 목적지와 대사를 예약해두면 그 씬이 로드될 때 열린다.
public class ArrivalDialogue : MonoBehaviour
{
    private const float PlayerWaitTimeout = 5f;

    private static string _pendingScene;
    private static int _pendingDialogueId;
    private static float _pendingDelay;

    public static void OpenOnSceneLoaded(string sceneName, int dialogueId, float delay)
    {
        if (string.IsNullOrEmpty(sceneName) || dialogueId <= 0) return;

        _pendingScene = sceneName;
        _pendingDialogueId = dialogueId;
        _pendingDelay = delay;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != _pendingScene) return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        _pendingScene = null;

        var runner = new GameObject(nameof(ArrivalDialogue)).AddComponent<ArrivalDialogue>();
        runner.StartCoroutine(runner.Routine(_pendingDialogueId, _pendingDelay));
    }

    private IEnumerator Routine(int dialogueId, float delay)
    {
        PlayerController player = null;
        float elapsed = 0f;

        while (player == null && elapsed < PlayerWaitTimeout)
        {
            yield return null;
            elapsed += Time.deltaTime;
            player = FindAnyObjectByType<PlayerController>();
        }

        if (player == null)
        {
            Debug.LogWarning("[ArrivalDialogue] 플레이어를 찾지 못해 도착 대사를 열지 못했습니다.");
        }
        else
        {
            // PlayerInputHandler.Start가 입력맵을 되돌린 뒤에 열어야 대화 입력맵이 유지된다
            if (delay > 0f) yield return new WaitForSeconds(delay);
            TutorialDialogue.Open(dialogueId, player);
        }

        Destroy(gameObject);
    }
}
