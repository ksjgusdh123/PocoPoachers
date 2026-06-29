using System.Linq;
using UnityEngine;

public class AISpeech : MonoBehaviour
{
    [SerializeField] private float _defaultDuration = 2f;

    private PlayerVision _localPlayerVision;
    private SpeechBubble _activeBubble;

    private void Awake()
    {
        _localPlayerVision = FindLocalPlayerVision();
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
