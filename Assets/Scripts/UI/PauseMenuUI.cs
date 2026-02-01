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
using UnityEngine.InputSystem;

public class PauseMenuUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject _pauseMenuRoot;

    [Header("Settings")]
    [SerializeField] private bool _pauseTimeScale = true;

    private bool _isPaused;
    private float _cachedTimeScale = 1f;

    private void Awake()
    {
        if (_pauseMenuRoot == null)
            _pauseMenuRoot = gameObject;

        SetPauseUiVisible(false);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            TogglePause();
    }

    #region Public API (Buttons)

    public void Resume()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayClick();

        SetPaused(false);
    }

    public void ReturnToMainMenu()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayClick();

        SetPaused(false);
        ServiceLocator.Get<IGameManager>().ReturnToMainMenu();
    }

    #endregion

    #region Pause Logic

    private void TogglePause()
    {
        // Optional: click sound when toggling with ESC as well
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayClick();

        SetPaused(!_isPaused);
    }

    private void SetPaused(bool isPaused)
    {
        if (_isPaused == isPaused)
            return;

        _isPaused = isPaused;

        if (_pauseTimeScale)
        {
            if (_isPaused)
            {
                _cachedTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = _cachedTimeScale <= 0f ? 1f : _cachedTimeScale;
            }
        }

        SetPauseUiVisible(_isPaused);
    }

    private void SetPauseUiVisible(bool isVisible)
    {
        if (_pauseMenuRoot != null)
            _pauseMenuRoot.SetActive(isVisible);
    }

    // exit game
    public void ExitGame()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayClick();

        ServiceLocator.Get<IGameManager>().ExitGame();
    }

    #endregion
}
