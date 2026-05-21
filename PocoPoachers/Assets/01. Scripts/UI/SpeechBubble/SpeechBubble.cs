using System.Collections;
using TMPro;
using UnityEngine;

public class SpeechBubble : WorldUIElement
{
    [SerializeField] private TextMeshProUGUI _text;

    private Coroutine _hideCoroutine;

    public void Show(string message, float duration = 2f)
    {
        _text.text = message;

        if (_hideCoroutine != null)
            StopCoroutine(_hideCoroutine);
        _hideCoroutine = StartCoroutine(HideAfter(duration));
    }

    private IEnumerator HideAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        _hideCoroutine = null;
        Release();
    }
}
