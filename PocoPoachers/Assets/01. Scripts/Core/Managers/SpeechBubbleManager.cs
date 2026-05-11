using UnityEngine;

public class SpeechBubbleManager : Singleton<SpeechBubbleManager>
{
    [SerializeField] private Canvas _worldCanvas;
    [SerializeField] private SpeechBubble _bubblePrefab;
    [SerializeField] private Vector3 _defaultOffset = new Vector3(0f, 2.5f, 0f);

    public SpeechBubble Create(Transform target)
    {
        SpeechBubble bubble = Instantiate(_bubblePrefab, _worldCanvas.transform);
        bubble.Init(target, _defaultOffset);
        return bubble;
    }
}
