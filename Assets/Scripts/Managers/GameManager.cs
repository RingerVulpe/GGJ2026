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
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour, IGameManager
{
    // Build Settings order (Scenes In Build)
    private const int TitleSceneIndex = 1;
    private const int GameSceneIndex = 2;
    private const int CreditsSceneIndex = 3;

    private bool _isLoading;

    #region Public Methods

    public void Initialize()
    {
        Debug.Log("GameManager -> Initialize()");

        // Assume we start on the title scene
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayTitleMusic();
    }

    public void StartGame()
    {
        // Stop menu music, start game music
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
            AudioManager.Instance.PlayGameMusic();
        }

        LoadScene(GameSceneIndex);
    }

    public void ReturnToMainMenu()
    {
        // Stop game music, return to title music
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
            AudioManager.Instance.PlayTitleMusic();
        }

        LoadScene(TitleSceneIndex);
    }

    public void OpenCredits()
    {
        // Optional: stop all music for credits
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopMusic();

        LoadScene(CreditsSceneIndex);
    }

    public void ExitGame()
    {
        Debug.Log("GameManager -> ExitGame()");
        Application.Quit();
    }

    #endregion

    #region Private Methods

    private void LoadScene(int sceneIndex)
    {
        if (_isLoading)
            return;

        var sceneCount = SceneManager.sceneCountInBuildSettings;
        if (sceneIndex < 0 || sceneIndex >= sceneCount)
        {
            Debug.LogWarning($"GameManager -> LoadScene({sceneIndex}) invalid. ScenesInBuild count: {sceneCount}");
            return;
        }

        if (SceneManager.GetActiveScene().buildIndex == sceneIndex)
            return;

        _isLoading = true;

        Debug.Log($"GameManager -> Loading scene index: {sceneIndex}");
        SceneManager.LoadScene(sceneIndex, LoadSceneMode.Single);

        // LoadScene is synchronous; if you later switch to async, unlock on completion.
        _isLoading = false;
    }

    #endregion
}
