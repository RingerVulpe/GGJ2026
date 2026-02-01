/*
 *  Author: Anthony Therrien
 *  Event: Global Game Jam 2026
 *
 *  This code is public because it has to be.
 *  Not because it is asking for feedback.
 *
 *  It was written fast, under pressure, to ship a game.
 *  That goal was achieved.
 *
 *  If you are reading this with opinions about style,
 *  architecture, or "how you would have done it":
 *  I genuinely do not care.
 *
 *  The code does what it needs to do.
 *  The jam is over.
 *  End of discussion.
 */
using UnityEngine;
using UnityEngine.UI;

public class MaskSlotUI : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private Image _icon;

    private MaskDefinitionSO _mask;
    private System.Action<MaskDefinitionSO, RectTransform> _onClicked;

    public void Bind(
        MaskDefinitionSO mask,
        System.Action<MaskDefinitionSO, RectTransform> onClicked
    )
    {
        _mask = mask;
        _onClicked = onClicked;

        if (_icon != null)
        {
            _icon.enabled = _mask != null && _mask.icon != null;
            _icon.sprite = _mask != null ? _mask.icon : null;
        }

        if (_button != null)
        {
            _button.interactable = _mask != null;
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(OnClicked);
        }
    }
    public void SetInteractable(bool isInteractable)
    {
        if (_button == null)
            return;

        // Only interactable if:
        // - the slot itself is enabled
        // - AND the grid allows interaction
        _button.interactable = isInteractable && _mask != null;
    }
    private void OnClicked()
    {
        if (_mask == null)
            return;

        _onClicked?.Invoke(_mask, (RectTransform)transform);
    }
}
