using System.Collections;
using UnityEngine;

// 튜토리얼에서 처음 죽었을 때 딱 한 번 대사를 띄운다. 두 번째부터는 아무 일도 하지 않는다.
//
// 부활이 끝난 뒤에 열어야 한다. PlayerRespawnPoint.RespawnRoutine이 쓰러진 동안 입력맵을
// Inventory로 돌렸다가 부활 후 SwitchToGameplayMapNextFrame으로 되돌리는데, 그보다 먼저 열면
// DialogueUI가 잡은 Dialogue 맵이 덮어써져 F로 다음 줄을 넘길 수 없다.
// (TutorialIntroDialogue가 PlayerInputHandler.Start를 피해 여는 것과 같은 이유)
public class TutorialDeathDialogue : MonoBehaviour
{
    [SerializeField] private int _dialogueId;

    [Tooltip("사망 후 대사가 뜨기까지의 대기 시간(초). PlayerRespawnPoint의 부활 대기보다 길어야 한다")]
    [SerializeField] private float _delay = 1.5f;

    [Tooltip("이 시간 안에 플레이어를 못 찾으면 포기한다(초)")]
    [SerializeField] private float _playerWaitTimeout = 5f;

    private PlayerController _player;
    private PlayerStat _stat;

    // 플레이어는 PlayerSpawner가 런타임에 만들기 때문에 씬 로드 직후에는 없다
    private IEnumerator Start()
    {
        float elapsed = 0f;

        while (_player == null && elapsed < _playerWaitTimeout)
        {
            yield return null;
            elapsed += Time.deltaTime;
            _player = FindAnyObjectByType<PlayerController>();
        }

        if (_player == null)
        {
            Debug.LogWarning("[TutorialDeathDialogue] 플레이어를 찾지 못해 사망 대사를 걸지 못했습니다.");
            yield break;
        }

        _stat = _player.GetComponent<PlayerStat>();
        if (_stat != null) _stat.OnDie += HandleDeath;
    }

    private void OnDestroy()
    {
        if (_stat != null) _stat.OnDie -= HandleDeath;
    }

    private void HandleDeath()
    {
        // 한 번 쓰고 마는 구독이라 여기서 바로 끊는다 — 두 번째 사망은 이 컴포넌트를 타지 않는다
        _stat.OnDie -= HandleDeath;
        _stat = null;

        StartCoroutine(OpenAfterRespawn());
    }

    private IEnumerator OpenAfterRespawn()
    {
        yield return new WaitForSeconds(_delay);
        TutorialDialogue.Open(_dialogueId, _player);
    }
}
