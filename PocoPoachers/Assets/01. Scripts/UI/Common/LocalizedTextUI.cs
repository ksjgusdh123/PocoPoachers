using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedTextUI : MonoBehaviour
{
    [SerializeField] private string _key;

    private TextMeshProUGUI _text;

    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        Refresh();
        LocalizationManager.GetInstance().OnLanguageChanged += Refresh;
    }

    private void OnDisable()
    {
        var manager = LocalizationManager.GetInstance();
        if (manager == null) return;
        manager.OnLanguageChanged -= Refresh;
    }

    private void Refresh()
    {
        _text.text = LocalizationManager.GetInstance().GetString(_key);
    }
}
