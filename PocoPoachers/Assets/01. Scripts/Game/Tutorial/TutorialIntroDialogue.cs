using System.Collections;
using UnityEngine;

// 튜토리얼 씬에 들어오면 F 상호작용 없이 곧바로 도입부 대화를 연다.
// 플레이어는 PlayerSpawner.Start에서 생성되고 PlayerInputHandler.Start가 게임플레이 입력맵으로
// 되돌리기 때문에, 그보다 늦게 열어야 DialogueUI가 잡은 Dialogue 입력맵이 덮어써지지 않는다.
public class TutorialIntroDialogue : MonoBehaviour
{
    [SerializeField] private int _startDialogueId = 10;

    [Tooltip("플레이어가 스폰된 뒤 대화창이 뜨기까지의 여유 시간(초)")]
    [SerializeField] private float _delay = 0.5f;

    [Tooltip("이 시간 안에 플레이어를 못 찾으면 포기한다(초)")]
    [SerializeField] private float _playerWaitTimeout = 5f;

    private IEnumerator Start()
    {
        PlayerController player = null;
        float elapsed = 0f;

        while (player == null && elapsed < _playerWaitTimeout)
        {
            yield return null;
            elapsed += Time.deltaTime;
            player = FindAnyObjectByType<PlayerController>();
        }

        if (player == null)
        {
            Debug.LogWarning("[TutorialIntroDialogue] 플레이어를 찾지 못해 도입부 대화를 열지 못했습니다.");
            yield break;
        }

        if (_delay > 0f) yield return new WaitForSeconds(_delay);

        TutorialDialogue.Open(_startDialogueId, player);
    }
}
