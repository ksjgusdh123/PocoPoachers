using TMPro;
using UnityEngine;

public abstract class PopupUIBase : UIBase
{
    [Header("Frame")]
    [SerializeField] private UIPopupFrame _frame;

    protected abstract TextMeshProUGUI MessageText { get; }

    public void SetContent(string title, string message)
    {
        _frame.SetTitle(title);
        if (MessageText != null) MessageText.text = message;
    }
}
