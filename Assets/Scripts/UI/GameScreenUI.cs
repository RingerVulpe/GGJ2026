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
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameScreenUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private MaskLibrarySO _maskLibrary;
    [SerializeField] private ClientSessionRunner _clientSession;

    [Header("UI")]
    [SerializeField] private MaskGridUI _maskGridUi;
    [SerializeField] private TMP_Text _requestText;
    [SerializeField] private TMP_Text _clientResponseText;
    [SerializeField] private Image _selectedMaskIcon;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _nextButton;
    [SerializeField] private TMP_Text _nextButtonText;
    [SerializeField] private MaskSelectionTravelFX _selectionTravelFx;

    [Header("Flow")]
    [SerializeField] private string _creditsSceneName = "CreditsScene";

    [Header("Request Text")]
    [Tooltip("Persistent intro shown above all client requests/outros/scores. Never cleared at runtime.")]
    [SerializeField, TextArea(3, 10)]
    private string _requestIntroText =
        "Select a mask and confirm your choice.\nListen carefully — people rarely say what they mean.";

    [Header("Final Outcome")]
    [Tooltip("If no outcomes match, this is used as the narrative outro.")]
    [SerializeField, TextArea(3, 12)]
    private string _fallbackOutcomeText =
        "The final client is gone.\n\nYour work is done... for now.";

    [Tooltip("Score thresholds -> narrative outro. Highest threshold that is <= final score wins.")]
    [SerializeField] private List<ScoreOutcome> _scoreOutcomes = new List<ScoreOutcome>();

    [Header("Final Score Screen")]
    [SerializeField, TextArea(2, 6)] private string _scoreScreenTitle = "Your Results";

    [SerializeField, TextArea(3, 12)]
    private string _scoreScreenBodyFormat =
        "Final score: {score}\nClients served: {clients}\nTotal time: {total:0.0}s\nAverage time: {avg:0.0}s/client";

    [Header("Debug")]
    [SerializeField] private bool _log;

    private bool _isConfirmProcessing;
    private Coroutine _requestRefreshRoutine;

    private MaskDefinitionSO _selectedMask;

    private bool _isGameStarted = false;

    private string _currentRequestBody = string.Empty;

    private ClientSessionRunner.SessionSummary _finalSummary;
    private bool _hasFinalSummary;

    private enum FlowState
    {
        WaitingToStart,
        ChoosingMask,
        ShowingResponse,
        TransitioningClient,
        ShowingFinalOutro,  
        ShowingFinalScore,   
    }

    private FlowState _flowState = FlowState.WaitingToStart;

    [Serializable]
    public class ScoreOutcome
    {
        [Tooltip("This outcome is used if final score >= minScore.")]
        public int minScore = 0;

        [Tooltip("Optional headline shown at the top of the outro.")]
        public string title = "Outcome";

        [TextArea(3, 12)]
        public string body = "You did something memorable.";
    }

    #region Unity Events

    private void Start()
    {
        if (_maskGridUi != null)
            _maskGridUi.Bind(_maskLibrary, OnMaskClicked);

        if (_confirmButton != null)
        {
            _confirmButton.onClick.RemoveAllListeners();
            _confirmButton.onClick.AddListener(OnConfirmPressed);
            _confirmButton.interactable = false;
        }

        if (_nextButton != null)
        {
            _nextButton.onClick.RemoveAllListeners();
            _nextButton.onClick.AddListener(OnNextPressed);
            _nextButton.interactable = true;
        }

        RefreshSelectedMask(null);

        _isGameStarted = false;
        _flowState = FlowState.WaitingToStart;

        if (_clientSession != null)
        {
            _clientSession.clientShown += OnClientShown;
            _clientSession.sessionComplete += OnSessionComplete;
            _clientSession.sessionCompleteWithSummary += OnSessionCompleteWithSummary;
        }



        RefreshFlowUI();
    }

    private void OnDestroy()
    {
        if (_clientSession != null)
        {
            _clientSession.clientShown -= OnClientShown;
            _clientSession.sessionComplete -= OnSessionComplete;
            _clientSession.sessionCompleteWithSummary -= OnSessionCompleteWithSummary;
        }
    }

    #endregion

    #region Request Text Composition

    private void SetRequestBody(string body)
    {
        _currentRequestBody = body ?? string.Empty;

        if (_requestText == null)
            return;

        if (string.IsNullOrEmpty(_requestIntroText))
        {
            _requestText.text = _currentRequestBody;
            return;
        }

        if (string.IsNullOrEmpty(_currentRequestBody))
        {
            _requestText.text = _requestIntroText;
            return;
        }

        _requestText.text = $"{_currentRequestBody}";
    }

    private void SetResponseText(string text)
    {
        if (_clientResponseText != null)
            _clientResponseText.text = text;
    }

    #endregion

    #region Private Methods

    private void Log(string msg)
    {
        if (_log)
            Debug.Log(msg);
    }

    private void StartRequestRefreshRoutine()
    {
        if (_requestRefreshRoutine != null)
            StopCoroutine(_requestRefreshRoutine);

        _requestRefreshRoutine = StartCoroutine(CoRefreshRequestWhenReady());
    }

    private IEnumerator CoRefreshRequestWhenReady()
    {
        if (_clientSession == null)
            yield break;

        // 1) Wait for transition to actually start (client leaves center)
        while (_clientSession.isClientInPosition)
            yield return null;

        // 2) Wait until a client exists
        while (_clientSession.currentClient == null)
            yield return null;

        // 3) Wait until the new client is in position
        while (!_clientSession.isClientInPosition)
            yield return null;

        // If we’re in end screens, don't overwrite with new request.
        if (_flowState != FlowState.ShowingFinalOutro && _flowState != FlowState.ShowingFinalScore)
            SetRequestBody(_clientSession.currentClient.requestText);

        _requestRefreshRoutine = null;
    }

    private void OnMaskClicked(MaskDefinitionSO mask, RectTransform clickedRect)
    {
        if (!CanInteractWithMasks())
        {
            Log("[GameScreenUI] Click ignored: CanInteractWithMasks == false");
            return;
        }

        Log($"[GameScreenUI] Mask clicked: {(mask != null ? mask.name : "NULL")} | rect: {(clickedRect != null ? clickedRect.name : "NULL")}");

        _selectedMask = mask;

        bool hasFx = _selectionTravelFx != null;
        bool hasIcon = mask != null && mask.icon != null;
        bool hasFrom = clickedRect != null;

        Log($"[GameScreenUI] hasFx={hasFx} hasIcon={hasIcon} hasFrom={hasFrom}");

        // Fallback if anything is missing
        if (!hasFx || !hasIcon || !hasFrom)
        {
            Log("[GameScreenUI] Fallback: instant RefreshSelectedMask");
            RefreshSelectedMask(mask);
            RefreshFlowUI();
            return;
        }

        Log("[GameScreenUI] Playing Selection Travel FX...");

        // Hide slot icon until arrival
        RefreshSelectedMask(null);

        _selectionTravelFx.Play(mask.icon, clickedRect, () =>
        {
            Log("[GameScreenUI] FX Arrived -> applying selected icon");
            RefreshSelectedMask(mask);
            RefreshFlowUI();
        });
    }

    private void OnConfirmPressed()
    {
        if (_isConfirmProcessing)
            return;

        if (_clientSession == null || !_isGameStarted)
            return;

        if (_selectedMask == null)
            return;

        if (!_clientSession.isClientInPosition || _clientSession.hasSubmittedForCurrentClient)
            return;

        _isConfirmProcessing = true;

        if (_confirmButton != null)
            _confirmButton.interactable = false;

        MaskDefinitionSO chosenMask = _selectedMask;

        _selectedMask = null;
        RefreshSelectedMask(null);

        int matchCount;
        string response = _clientSession.SubmitMask(chosenMask, out matchCount);

        if (string.IsNullOrEmpty(response))
        {
            _isConfirmProcessing = false;
            RefreshFlowUI();
            return;
        }

        SetResponseText(response);

        _flowState = FlowState.ShowingResponse;
        RefreshFlowUI();

        _isConfirmProcessing = false;
    }

    private void OnNextPressed()
    {
        if (_clientSession == null)
            return;

        if (_isConfirmProcessing)
            return;

        if (_nextButton != null)
            _nextButton.gameObject.SetActive(false);

        // 0) Outro -> Score screen
        if (_flowState == FlowState.ShowingFinalOutro)
        {
            ShowFinalScoreScreen();
            return;
        }

        // 0b) Score screen -> Credits
        if (_flowState == FlowState.ShowingFinalScore)
        {
            SceneManager.LoadScene(_creditsSceneName);
            return;
        }

        // 1) Start game
        if (!_isGameStarted || _flowState == FlowState.WaitingToStart)
        {
            _isGameStarted = true;

            SetResponseText(string.Empty);

            SetRequestBody(string.Empty);

            _selectedMask = null;
            RefreshSelectedMask(null);

            if (_confirmButton != null)
                _confirmButton.interactable = false;

            _clientSession.BeginCurrentClient();

            StartRequestRefreshRoutine();

            _flowState = FlowState.ChoosingMask;
            RefreshFlowUI();
            return;
        }

        // 2) Next client
        if (_clientSession.isWaitingForProceed)
        {
            _flowState = FlowState.TransitioningClient;

            _selectedMask = null;
            RefreshSelectedMask(null);

            if (_confirmButton != null)
                _confirmButton.interactable = false;

            SetRequestBody(string.Empty);
            SetResponseText(string.Empty);

            _clientSession.ProceedToNextClient();

            if (_nextButton != null)
                _nextButton.interactable = false;

            StartRequestRefreshRoutine();

            RefreshFlowUI();
            return;
        }
    }

    private void OnClientShown(ClientDefinitionSO client)
    {
        if (!_isGameStarted || client == null)
            return;

        if (_flowState == FlowState.ShowingFinalOutro || _flowState == FlowState.ShowingFinalScore)
            return;

        SetRequestBody(client.requestText);
        SetResponseText(string.Empty);

        _selectedMask = null;
        RefreshSelectedMask(null);

        _flowState = FlowState.ChoosingMask;
        RefreshFlowUI();
    }

    // Old hook: if summary doesn't arrive, still show an outro (fallback).
    private void OnSessionComplete()
    {
        if (_flowState == FlowState.ShowingFinalOutro || _flowState == FlowState.ShowingFinalScore)
            return;

        // If we never got the summary event, show fallback outro.
        _hasFinalSummary = false;
        _finalSummary = default;

        ShowFinalOutro();
    }

    private void OnSessionCompleteWithSummary(ClientSessionRunner.SessionSummary summary)
    {
        _hasFinalSummary = true;
        _finalSummary = summary;

        ShowFinalOutro();
    }

    private void ShowFinalOutro()
    {
        _flowState = FlowState.ShowingFinalOutro;

        _selectedMask = null;
        RefreshSelectedMask(null);

        if (_confirmButton != null)
            _confirmButton.interactable = false;

        SetResponseText(string.Empty);

        string outro = BuildOutroText();
        SetRequestBody(outro);

        RefreshFlowUI();
    }

    private void ShowFinalScoreScreen()
    {
        _flowState = FlowState.ShowingFinalScore;

        _selectedMask = null;
        RefreshSelectedMask(null);

        if (_confirmButton != null)
            _confirmButton.interactable = false;

        SetResponseText(string.Empty);

        string scoreText = BuildScoreText();
        SetRequestBody(scoreText);

        RefreshFlowUI();
    }

    private string BuildOutroText()
    {
        if (!_hasFinalSummary)
            return _fallbackOutcomeText;

        int finalScore = _finalSummary.finalScore;

        ScoreOutcome picked = PickOutcome(finalScore);
        if (picked == null)
            return _fallbackOutcomeText;

        if (!string.IsNullOrWhiteSpace(picked.title))
            return $"{picked.title}\n\n{picked.body}";

        return picked.body;
    }

    private string BuildScoreText()
    {
        if (!_hasFinalSummary)
            return $"{_scoreScreenTitle}\n\nFinal score: ?";

        string body =
            _scoreScreenBodyFormat
                .Replace("{score}", _finalSummary.finalScore.ToString())
                .Replace("{clients}", _finalSummary.clientsServed.ToString())
                .Replace("{total:0.0}", _finalSummary.totalTimeSeconds.ToString("0.0"))
                .Replace("{avg:0.0}", _finalSummary.avgTimeSeconds.ToString("0.0"));

        if (string.IsNullOrWhiteSpace(_scoreScreenTitle))
            return body;

        return $"{_scoreScreenTitle}\n\n{body}";
    }

    private ScoreOutcome PickOutcome(int finalScore)
    {
        if (_scoreOutcomes == null || _scoreOutcomes.Count == 0)
            return null;

        ScoreOutcome best = null;
        int bestMin = int.MinValue;

        for (int i = 0; i < _scoreOutcomes.Count; i++)
        {
            var o = _scoreOutcomes[i];
            if (o == null)
                continue;

            if (finalScore >= o.minScore && o.minScore >= bestMin)
            {
                bestMin = o.minScore;
                best = o;
            }
        }

        return best;
    }

    private void RefreshNextButton()
    {
        if (_nextButton == null)
            return;

        _nextButton.gameObject.SetActive(false);

        // Start state
        if (!_isGameStarted || _flowState == FlowState.WaitingToStart)
        {
            _nextButton.gameObject.SetActive(true);
            _nextButton.interactable = true;

            if (_nextButtonText != null)
                _nextButtonText.text = "Start";

            return;
        }

        // Outro OR Score screen
        if (_flowState == FlowState.ShowingFinalOutro || _flowState == FlowState.ShowingFinalScore)
        {
            _nextButton.gameObject.SetActive(true);
            _nextButton.interactable = true;

            if (_nextButtonText != null)
                _nextButtonText.text = "Click to continue";

            return;
        }

        if (_flowState == FlowState.TransitioningClient)
        {
            _nextButton.gameObject.SetActive(false);
            return;
        }

        if (_clientSession == null)
            return;

        if (_flowState == FlowState.ChoosingMask)
        {
            _nextButton.gameObject.SetActive(false);
            return;
        }

        if (_flowState == FlowState.ShowingResponse)
        {
            bool canProceed = _clientSession.isWaitingForProceed;

            _nextButton.gameObject.SetActive(canProceed);
            _nextButton.interactable = canProceed;

            if (canProceed && _nextButtonText != null)
                _nextButtonText.text = "Click for Next Client";

            return;
        }

        _nextButton.gameObject.SetActive(false);
    }

    private void RefreshFlowUI()
    {
        if (_clientSession == null)
            return;

        bool canConfirm =
            _flowState == FlowState.ChoosingMask &&
            _isGameStarted &&
            !_isConfirmProcessing &&
            _selectedMask != null &&
            _clientSession.isClientInPosition &&
            !_clientSession.hasSubmittedForCurrentClient &&
            !_clientSession.isWaitingForProceed;

        if (_confirmButton != null)
            _confirmButton.interactable = canConfirm;

        RefreshNextButton();
        RefreshMaskGridInteractivity();
    }

    private void RefreshMaskGridInteractivity()
    {
        if (_maskGridUi == null)
            return;

        bool canInteract = CanInteractWithMasks();

        var cg = _maskGridUi.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.interactable = canInteract;
            cg.blocksRaycasts = canInteract;
            cg.alpha = canInteract ? 1f : 0.6f;
        }
    }

    private bool CanInteractWithMasks()
    {
        if (!_isGameStarted)
            return false;

        if (_clientSession == null)
            return false;

        // No interaction during end screens
        if (_flowState == FlowState.ShowingFinalOutro || _flowState == FlowState.ShowingFinalScore)
            return false;

        if (_flowState != FlowState.ChoosingMask)
            return false;

        if (!_clientSession.isClientInPosition)
            return false;

        if (_clientSession.hasSubmittedForCurrentClient || _clientSession.isWaitingForProceed)
            return false;

        return true;
    }

    private void RefreshSelectedMask(MaskDefinitionSO mask)
    {
        if (_selectedMaskIcon == null)
            return;

        _selectedMaskIcon.enabled = mask != null && mask.icon != null;
        _selectedMaskIcon.sprite = mask != null ? mask.icon : null;
    }

    #endregion
}
