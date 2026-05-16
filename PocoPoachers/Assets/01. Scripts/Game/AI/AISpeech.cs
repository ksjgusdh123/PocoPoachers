using UnityEngine;

public class AISpeech : MonoBehaviour
{
    [SerializeField] private float _defaultDuration = 2f;

    public void Say(string message)
    {
        Say(message, _defaultDuration);
    }

    public void Say(string message, float duration)
    {
        SpeechBubble bubble = WorldUIManager.Instance.Create<SpeechBubble>(WorldUIType.SpeechBubble, transform);
        bubble.Show(message, duration);
    }
}
