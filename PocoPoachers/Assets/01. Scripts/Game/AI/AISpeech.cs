using UnityEngine;

public class AISpeech : MonoBehaviour
{
    [SerializeField] private float _defaultDuration = 2f;

    private SpeechBubble _bubble;

    private void Start()
    {
        _bubble = SpeechBubbleManager.Instance.Create(transform);
    }

    public void Say(string message)
    {
        _bubble.Show(message, _defaultDuration);
    }

    public void Say(string message, float duration)
    {
        _bubble.Show(message, duration);
    }
}
