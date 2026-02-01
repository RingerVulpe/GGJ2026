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

public class MaskGridUI : MonoBehaviour
{
    [SerializeField] private MaskSlotUI[] _slots;
    [SerializeField] private CanvasGroup _canvasGroup; // optional, assign in inspector

    #region Public Methods

    public void Bind(MaskLibrarySO maskLibrary, System.Action<MaskDefinitionSO, RectTransform> onMaskClicked)
    {
        if (_slots == null || _slots.Length == 0)
            return;

        for (int i = 0; i < _slots.Length; i++)
        {
            MaskDefinitionSO mask = null;

            if (maskLibrary != null && maskLibrary.masks != null && i < maskLibrary.masks.Length)
                mask = maskLibrary.masks[i];

            _slots[i].Bind(mask, onMaskClicked);
        }
    }


    public void SetInteractable(bool isInteractable)
    {
        // Visual + global input gate (optional but recommended)
        if (_canvasGroup != null)
        {
            _canvasGroup.interactable = isInteractable;
            _canvasGroup.blocksRaycasts = isInteractable;
            _canvasGroup.alpha = isInteractable ? 1f : 0.6f;
        }

        // Hard gate per-slot (works even without CanvasGroup)
        if (_slots == null)
            return;

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] != null)
                _slots[i].SetInteractable(isInteractable);
        }
    }

    #endregion
}
