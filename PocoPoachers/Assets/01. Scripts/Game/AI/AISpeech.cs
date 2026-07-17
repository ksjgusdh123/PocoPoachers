using System.Linq;
using UnityEngine;

public class AISpeech : MonoBehaviour
{
    [SerializeField] private float _defaultDuration = 2f;

    private PlayerVision _localPlayerVision;
    private SpeechBubble _activeBubble;
    private EnemyNetSync _netSync;

    private void Awake()
    {
        _localPlayerVision = FindLocalPlayerVision();
        _netSync = GetComponentInParent<EnemyNetSync>();
    }

    private void OnEnable()
    {
        if (_localPlayerVision != null)
            _localPlayerVision.OnTargetLost += OnTargetLost;
    }

    private void OnDisable()
    {
        if (_localPlayerVision != null)
            _localPlayerVision.OnTargetLost -= OnTargetLost;
    }

    public void Say(string message)
    {
        Say(message, _defaultDuration);
    }

    public void Say(string message, float duration)
    {
        // AI는 호스트에서만 돌아 이 경로도 호스트에서만 실행된다.
        // 게스트는 AI가 없어 대사가 안 뜨므로, 호스트가 대사를 전파해 각 게스트가 자기 시야로 표시하게 한다.
        if (RoomManager.IsHost && _netSync != null && _netSync.EnemyId != 0)
            RoomSync.EnemySpeak(_netSync.EnemyId, message, duration);

        ShowBubble(message, duration);
    }

    // 네트워크로 받은 대사를 로컬에만 표시 (재전파 없음)
    public void ShowRemote(string message, float duration) => ShowBubble(message, duration);

    private void ShowBubble(string message, float duration)
    {
        // 표시 판정은 각 클라이언트의 로컬 플레이어 시야 기준 — 자기가 보는 적의 대사만 뜬다
        if (_localPlayerVision != null && !_localPlayerVision.DetectedTargets.Contains(gameObject))
            return;

        _activeBubble = WorldUIManager.Instance.Create<SpeechBubble>(WorldUIType.SpeechBubble, transform);
        _activeBubble.Show(message, duration);
    }

    private void OnTargetLost(GameObject target)
    {
        if (target != gameObject || _activeBubble == null)
            return;

        _activeBubble.Release();
        _activeBubble = null;
    }

    // 치트콘솔의 FindLocalPlayer()와 동일한 방식으로 로컬 플레이어를 식별
    private static PlayerVision FindLocalPlayerVision()
    {
        var inputHandlers = FindObjectsByType<PlayerInputHandler>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var input in inputHandlers)
        {
            if (input.isActiveAndEnabled)
                return input.GetComponent<PlayerVision>();
        }

        return null;
    }
}
