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

public class ScrollReset : MonoBehaviour
{
    [SerializeField] private ScrollRect _scrollRect;

    private void OnEnable()
    {
        if (_scrollRect != null)
            _scrollRect.verticalNormalizedPosition = 1f;
    }
}
