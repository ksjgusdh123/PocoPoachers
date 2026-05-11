using System.Collections;
using TMPro;
using UnityEngine;

public class SpeechBubble : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private Vector3 _offset;

    private Transform _target;
    private Coroutine _hideCoroutine;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (_target != null)
            transform.position = _target.position + _offset;
    }

    public void Init(Transform target, Vector3 offset)
    {
        _target = target;
        _offset = offset;
    }

    public void Show(string message, float duration = 2f)
    {
        _text.text = message;
        gameObject.SetActive(true);

        if (_hideCoroutine != null)
            StopCoroutine(_hideCoroutine);
        _hideCoroutine = StartCoroutine(HideAfter(duration));
    }

    private IEnumerator HideAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        gameObject.SetActive(false);
        _hideCoroutine = null;
    }
}
