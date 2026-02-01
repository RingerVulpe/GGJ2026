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
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ReferenceBookUI : MonoBehaviour
{
    [Header("UI - Text (2 pages)")]
    [SerializeField] private TMP_Text _leftPageText;
    [SerializeField] private TMP_Text _rightPageText;

    [Header("UI - Navigation")]
    [SerializeField] private Button _nextButton;
    [SerializeField] private Button _prevButton;

    [Header("UI - Navigation Arrows (visuals)")]
    [Tooltip("Optional: the arrow Image under/inside the Next button that should hide when Next is disabled.")]
    [SerializeField] private Image _nextArrowImage;

    [Tooltip("Optional: the arrow Image under/inside the Prev button that should hide when Prev is disabled.")]
    [SerializeField] private Image _prevArrowImage;

    [Header("UI - Page Corners (hover hints)")]
    [SerializeField] private Image _rightPageCornerImage; // shows when hovering Next
    [SerializeField] private Image _leftPageCornerImage;  // shows when hovering Prev

    [Header("Pages (Expandable)")]
    [TextArea(3, 20)]
    [SerializeField] private List<string> _pages = new List<string>(8);

    // Index of the LEFT page in the current spread (0-based).
    // Spread is [leftIndex] and [leftIndex + 1]
    private int _leftPageIndex;

    private void Awake()
    {
        // Default hover visuals off
        SetCorner(_rightPageCornerImage, false);
        SetCorner(_leftPageCornerImage, false);

        // Wire buttons
        if (_nextButton != null) _nextButton.onClick.AddListener(NextSpread);
        if (_prevButton != null) _prevButton.onClick.AddListener(PrevSpread);

        // Add hover listeners to buttons
        AddHoverEvents(
            _nextButton,
            onEnter: () =>
            {
                // Only show hint if you can actually go next
                if (CanGoNext())
                    SetCorner(_rightPageCornerImage, true);
            },
            onExit: () => SetCorner(_rightPageCornerImage, false)
        );

        AddHoverEvents(
            _prevButton,
            onEnter: () =>
            {
                // Only show hint if you can actually go prev
                if (CanGoPrev())
                    SetCorner(_leftPageCornerImage, true);
            },
            onExit: () => SetCorner(_leftPageCornerImage, false)
        );
    }

    private void Start()
    {
        // Ensure we start at page 1+2 (index 0+1)
        _leftPageIndex = 0;
        Refresh();
    }

    #region Public API

    public void SetPages(List<string> pages, int startLeftPageIndex = 0)
    {
        _pages = pages ?? new List<string>();
        _leftPageIndex = Mathf.Clamp(startLeftPageIndex, 0, GetMaxLeftIndex());
        Refresh();
    }

    public void OpenToPage(int pageNumber1Based)
    {
        // Page 1-based -> index 0-based
        int index = Mathf.Clamp(pageNumber1Based - 1, 0, Mathf.Max(0, _pages.Count - 1));

        // Snap to a left-page index (even index) so we always show a spread
        _leftPageIndex = (index / 2) * 2;
        _leftPageIndex = Mathf.Clamp(_leftPageIndex, 0, GetMaxLeftIndex());

        Refresh();
    }

    #endregion

    #region Navigation

    private void NextSpread()
    {
        if (!CanGoNext())
            return;

        _leftPageIndex = Mathf.Clamp(_leftPageIndex + 2, 0, GetMaxLeftIndex());
        Refresh();
    }

    private void PrevSpread()
    {
        if (!CanGoPrev())
            return;

        _leftPageIndex = Mathf.Clamp(_leftPageIndex - 2, 0, GetMaxLeftIndex());
        Refresh();
    }

    private bool CanGoNext()
    {
        // Can go next if there exists at least one page beyond the current right page
        int nextLeft = _leftPageIndex + 2;
        return _pages != null && nextLeft < _pages.Count;
    }

    private bool CanGoPrev()
    {
        return _pages != null && (_leftPageIndex - 2) >= 0;
    }

    #endregion

    #region Rendering

    private void Refresh()
    {
        // Safety: handle 0 pages
        if (_pages == null || _pages.Count == 0)
        {
            if (_leftPageText != null) _leftPageText.text = "";
            if (_rightPageText != null) _rightPageText.text = "";

            SetCorner(_rightPageCornerImage, false);
            SetCorner(_leftPageCornerImage, false);

            ApplyNavState(canPrev: false, canNext: false);
            return;
        }

        _leftPageIndex = Mathf.Clamp(_leftPageIndex, 0, GetMaxLeftIndex());

        int rightIndex = _leftPageIndex + 1;

        if (_leftPageText != null)
            _leftPageText.text = SafeGetPage(_leftPageIndex);

        if (_rightPageText != null)
            _rightPageText.text = (rightIndex < _pages.Count) ? SafeGetPage(rightIndex) : "";

        bool canPrev = CanGoPrev();
        bool canNext = CanGoNext();

        ApplyNavState(canPrev, canNext);

        // If you can't navigate that direction, ensure the hint isn't visible.
        if (!canPrev) SetCorner(_leftPageCornerImage, false);
        if (!canNext) SetCorner(_rightPageCornerImage, false);
    }

    private void ApplyNavState(bool canPrev, bool canNext)
    {
        SetButtonInteractable(_prevButton, canPrev);
        SetButtonInteractable(_nextButton, canNext);

        // Hide/show arrow visuals to match interactable state.
        SetArrowVisible(_prevArrowImage, canPrev);
        SetArrowVisible(_nextArrowImage, canNext);
    }

    private string SafeGetPage(int index)
    {
        if (_pages == null || index < 0 || index >= _pages.Count)
            return "";

        return _pages[index] ?? "";
    }

    private int GetMaxLeftIndex()
    {
        return Mathf.Max(0, _pages.Count - 1);
    }

    #endregion

    #region Hover Helpers

    private static void AddHoverEvents(Button button, Action onEnter, Action onExit)
    {
        if (button == null) return;

        var trigger = button.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = button.gameObject.AddComponent<EventTrigger>();

        trigger.triggers ??= new List<EventTrigger.Entry>();

        var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener(_ => onEnter?.Invoke());

        var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener(_ => onExit?.Invoke());

        trigger.triggers.Add(enterEntry);
        trigger.triggers.Add(exitEntry);
    }

    private static void SetCorner(Image img, bool enabled)
    {
        if (img != null)
            img.enabled = enabled;
    }

    private static void SetArrowVisible(Image img, bool visible)
    {
        if (img != null)
            img.enabled = visible;
    }

    private static void SetButtonInteractable(Button button, bool interactable)
    {
        if (button != null)
            button.interactable = interactable;
    }

    #endregion
}
