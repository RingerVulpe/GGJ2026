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
using UnityEngine;

public class ClientSessionRunner : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private ClientQueueSO _clientQueue;

    [Header("Client Spawn + Movement")]
    [SerializeField] private RectTransform _clientParent;
    [SerializeField] private ClientView _clientPrefab;

    [SerializeField] private RectTransform _startSlidePos;   // offscreen right
    [SerializeField] private RectTransform _centerPos;       // visible slot
    [SerializeField] private RectTransform _exitSlidePos;    // offscreen left

    [Header("Motion Tuning")]
    [SerializeField] private float _slideSmoothTime = 0.18f;     // lower = snappier, higher = floatier
    [SerializeField] private float _slideMaxSpeed = 3800f;       // UI units/sec clamp
    [SerializeField] private float _arriveDistance = 0.35f;      // how close is "arrived"
    [SerializeField] private float _arriveSpeed = 10f;           // how slow is "arrived"
    [SerializeField] private float _maxSlideTime = 2.25f;        // failsafe

    [Header("Squish (Velocity Driven)")]
    [SerializeField] private float _squishAmount = 0.10f;        // 0..0.2 usually
    [SerializeField] private float _squishAtSpeed = 900f;        // speed where squish peaks
    [SerializeField] private float _squishResponse = 16f;        // how fast scale follows target

    [Header("Settle Jiggle (After Arrive)")]
    [SerializeField] private float _settleJiggleAmount = 0.02f;  // 0..0.05 subtle
    [SerializeField] private float _settleFrequency = 10f;       // wiggle speed
    [SerializeField] private float _settleDecay = 16f;           // higher = dies quicker

    [Header("Scoring")]
    [Tooltip("Base score awarded per mask match count (matchCount * pointsPerMatch).")]
    [SerializeField] private int _pointsPerMatch = 1;

    [Tooltip("Optional: time bonus if you answer quickly. 0 disables.")]
    [SerializeField] private int _maxTimeBonus = 2;

    [Tooltip("This outcome is used if final score >= minScore.")]
    [SerializeField] private float _timeBonusTargetSeconds = 8f;

    [Tooltip("At/above this time, time bonus becomes 0 (linear falloff).")]
    [SerializeField] private float _timeBonusMaxSeconds = 25f;

    [Header("Debug")]
    [SerializeField] private bool _log;

    private int _clientIndex;

    private ClientView _currentClientView;
    private Coroutine _moveRoutine;

    private bool _isWaitingForProceed;
    private bool _isClientInPosition;
    private bool _hasSubmittedForCurrentClient;

    // Timing + score
    private float _clientStartTime;
    private float _sessionStartTime;
    private int _finalScore;
    private int _clientsServed;

    public ClientDefinitionSO currentClient => GetCurrentClient();
    public bool isComplete => _clientQueue == null || _clientQueue.clients == null || _clientIndex >= _clientQueue.clients.Length;
    public ClientView currentClientView => _currentClientView;
    public bool isWaitingForProceed => _isWaitingForProceed;
    public bool isClientInPosition => _isClientInPosition;
    public bool hasSubmittedForCurrentClient => _hasSubmittedForCurrentClient;

    // Optional hooks so UI can listen without hard references
    public Action<ClientDefinitionSO> clientShown;
    public Action<string, int> clientResponded;

    // Original hook kept (non-breaking)
    public Action sessionComplete;

    // New: summary hook for end screens (outro/score)
    public Action<SessionSummary> sessionCompleteWithSummary;

    [Serializable]
    public struct SessionSummary
    {
        public int finalScore;
        public int clientsServed;
        public float totalTimeSeconds;
        public float avgTimeSeconds;
    }

    #region Unity Events

    private void Awake()
    {
        _clientIndex = 0;

        _finalScore = 0;
        _clientsServed = 0;

        _sessionStartTime = 0f;
        _clientStartTime = 0f;
    }

    #endregion

    #region Public Methods

    public void ResetSession()
    {
        _clientIndex = 0;
        _isWaitingForProceed = false;
        _isClientInPosition = false;
        _hasSubmittedForCurrentClient = false;

        _finalScore = 0;
        _clientsServed = 0;
        _sessionStartTime = 0f;
        _clientStartTime = 0f;

        DespawnCurrentClient();
    }

    public void BeginCurrentClient()
    {
        if (isComplete)
        {
            InvokeSessionComplete();
            return;
        }

        if (_sessionStartTime <= 0f)
            _sessionStartTime = Time.time;

        _isWaitingForProceed = false;
        _isClientInPosition = false;
        _hasSubmittedForCurrentClient = false;

        SpawnCurrentClient();

        SlideClientTo(_centerPos, onArrived: () =>
        {
            _isClientInPosition = true;
            _clientStartTime = Time.time; // timer starts when client arrives

            // SFX: arrive whoosh
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayClientArrive();

            // VO: request line
            var c = GetCurrentClient();
            if (c != null && AudioManager.Instance != null)
                AudioManager.Instance.PlayClientRequest(c.requestVo);
        });

        clientShown?.Invoke(GetCurrentClient());
    }

    public string SubmitMask(MaskDefinitionSO chosenMask, out int matchCount)
    {
        matchCount = 0;

        if (!_isClientInPosition)
            return string.Empty;

        if (_hasSubmittedForCurrentClient)
            return string.Empty;

        var client = GetCurrentClient();
        if (client == null)
            return string.Empty;

        _hasSubmittedForCurrentClient = true;

        matchCount = MaskJudge.GetMatchCount(client, chosenMask);
        string response = MaskJudge.GetResponseText(client, matchCount);

        // Apply the mask ONLY on confirm (no preview-on-select).
        if (_currentClientView != null)
            _currentClientView.AttachMask(chosenMask != null ? chosenMask.icon : null);

        // Score + time accounting
        float clientTime = Mathf.Max(0f, Time.time - _clientStartTime);
        _finalScore += matchCount;
        _clientsServed++;

        if (AudioManager.Instance != null)
        {
            AudioClip responseClip = null;

            if (matchCount <= 0)
                responseClip = client.responseIncorrectVo;
            else if (matchCount == 1)
                responseClip = client.responsePartialVo;
            else
                responseClip = client.responseCorrectVo;

            AudioManager.Instance.PlayClientResponse(responseClip);
        }

        clientResponded?.Invoke(response, matchCount);

        // Wait here until UI triggers ProceedToNextClient().
        _isWaitingForProceed = true;

        return response;
    }

    public void ProceedToNextClient()
    {
        if (!_isWaitingForProceed)
            return;

        _isWaitingForProceed = false;

        // VO: exit line + SFX leave whoosh (triggered when leaving starts)
        var client = GetCurrentClient();
        if (client != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayClientExit(client.exitVo);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayClientLeave();

        // Slide out current client, then advance and bring the next one in.
        if (_currentClientView != null)
        {
            SlideClientTo(_exitSlidePos, onArrived: () =>
            {
                DespawnCurrentClient();
                AdvanceAndBeginNextClient();
            });
        }
        else
        {
            AdvanceAndBeginNextClient();
        }
    }

    #endregion

    #region Private Methods

    private void AdvanceAndBeginNextClient()
    {
        _clientIndex++;

        if (isComplete)
        {
            InvokeSessionComplete();
            return;
        }

        BeginCurrentClient();
    }

    private void InvokeSessionComplete()
    {
        float totalTime = (_sessionStartTime > 0f) ? Mathf.Max(0f, Time.time - _sessionStartTime) : 0f;
        float avgTime = (_clientsServed > 0) ? (totalTime / _clientsServed) : 0f;

        SessionSummary summary = new SessionSummary
        {
            finalScore = _finalScore,
            clientsServed = _clientsServed,
            totalTimeSeconds = totalTime,
            avgTimeSeconds = avgTime
        };

        if (_log)
            Debug.Log($"[ClientSessionRunner] Session complete. clients={_clientsServed} score={_finalScore} totalTime={totalTime:0.00}s");

        // Fire new summary hook first so UI can show outro/score.
        sessionCompleteWithSummary?.Invoke(summary);

        // Fire original hook too (non-breaking for any other listeners).
        sessionComplete?.Invoke();
    }

    private ClientDefinitionSO GetCurrentClient()
    {
        if (_clientQueue == null || _clientQueue.clients == null)
            return null;

        if (_clientIndex < 0 || _clientIndex >= _clientQueue.clients.Length)
            return null;

        return _clientQueue.clients[_clientIndex];
    }

    private void SpawnCurrentClient()
    {
        DespawnCurrentClient();

        if (_clientPrefab == null || _clientParent == null || _startSlidePos == null)
        {
            Debug.LogWarning("ClientSessionRunner -> Missing prefab/parent/startSlidePos.");
            return;
        }

        _currentClientView = Instantiate(_clientPrefab, _clientParent);

        // Start hidden offscreen right
        _currentClientView.rectTransform.anchoredPosition = _startSlidePos.anchoredPosition;
        _currentClientView.rectTransform.localScale = Vector3.one;

        var client = GetCurrentClient();
        _currentClientView.SetClientDefinition(client);
        _currentClientView.ClearMask();

        // Timer will be set again when arrived at center; keep safe start anyway.
        _clientStartTime = Time.time;
    }

    private void DespawnCurrentClient()
    {
        if (_moveRoutine != null)
        {
            StopCoroutine(_moveRoutine);
            _moveRoutine = null;
        }

        if (_currentClientView != null)
        {
            Destroy(_currentClientView.gameObject);
            _currentClientView = null;
        }
    }

    private void SlideClientTo(RectTransform target, Action onArrived)
    {
        if (_currentClientView == null || target == null)
            return;

        if (_moveRoutine != null)
            StopCoroutine(_moveRoutine);

        _isClientInPosition = false;

        _moveRoutine = StartCoroutine(SlideRoutine(_currentClientView.rectTransform, target.anchoredPosition, onArrived));
    }

    private IEnumerator SlideRoutine(RectTransform clientRect, Vector2 targetPos, Action onArrived)
    {
        Vector2 velocity = Vector2.zero;
        Vector2 scale = new Vector2(clientRect.localScale.x, clientRect.localScale.y);

        float elapsed = 0f;
        float settleTimer = 0f;
        bool settledPhase = false;

        if (!IsFinite(clientRect.anchoredPosition))
            clientRect.anchoredPosition = targetPos;

        if (!IsFinite(scale))
            scale = Vector2.one;

        while (elapsed < _maxSlideTime)
        {
            float dt = Time.deltaTime;
            if (dt <= 0f)
            {
                yield return null;
                continue;
            }

            elapsed += dt;

            Vector2 currentPos = clientRect.anchoredPosition;
            if (!IsFinite(currentPos))
                currentPos = targetPos;

            Vector2 newPos = Vector2.SmoothDamp(
                currentPos,
                targetPos,
                ref velocity,
                Mathf.Max(0.0001f, _slideSmoothTime),
                Mathf.Max(1f, _slideMaxSpeed),
                dt
            );

            if (!IsFinite(newPos))
                newPos = targetPos;

            clientRect.anchoredPosition = newPos;

            float speed = velocity.magnitude;
            float distance = Vector2.Distance(newPos, targetPos);

            bool arrived = distance <= _arriveDistance && speed <= _arriveSpeed;

            if (arrived && !settledPhase)
            {
                settledPhase = true;
                settleTimer = 0f;

                velocity = Vector2.zero;
                clientRect.anchoredPosition = targetPos;
            }

            float speed01 = (_squishAtSpeed <= 0f) ? 0f : Mathf.Clamp01(speed / _squishAtSpeed);
            Vector2 targetScale = ComputeVelocitySquishScale(velocity, speed01, _squishAmount);

            if (settledPhase && _settleJiggleAmount > 0f)
            {
                settleTimer += dt;

                float decay = Mathf.Exp(-_settleDecay * settleTimer);
                float wiggle = Mathf.Sin(settleTimer * _settleFrequency * Mathf.PI * 2f) * _settleJiggleAmount * decay;

                targetScale.x += wiggle * 0.7f;
                targetScale.y -= wiggle;
            }

            float lerp = 1f - Mathf.Exp(-_squishResponse * dt);
            scale = Vector2.Lerp(scale, targetScale, lerp);

            if (!IsFinite(scale))
                scale = Vector2.one;

            clientRect.localScale = new Vector3(scale.x, scale.y, 1f);

            if (settledPhase)
            {
                bool jiggleDone = _settleJiggleAmount <= 0f || settleTimer >= 0.20f;
                if (jiggleDone)
                    break;
            }

            if (!settledPhase && distance <= _arriveDistance * 0.5f && speed <= _arriveSpeed * 0.5f)
            {
                clientRect.anchoredPosition = targetPos;
                settledPhase = true;
                settleTimer = 0f;
                velocity = Vector2.zero;
            }

            yield return null;
        }

        clientRect.anchoredPosition = targetPos;
        yield return StartCoroutine(ReturnScaleToOne(clientRect));

        _moveRoutine = null;
        onArrived?.Invoke();
    }

    private IEnumerator ReturnScaleToOne(RectTransform clientRect)
    {
        Vector2 start = new Vector2(clientRect.localScale.x, clientRect.localScale.y);
        if (!IsFinite(start))
            start = Vector2.one;

        float t = 0f;
        const float duration = 0.12f;

        while (t < duration)
        {
            float dt = Time.deltaTime;
            if (dt <= 0f)
            {
                yield return null;
                continue;
            }

            t += dt;

            float u = 1f - Mathf.Exp(-20f * t);
            Vector2 s = Vector2.Lerp(start, Vector2.one, u);

            if (!IsFinite(s))
                s = Vector2.one;

            clientRect.localScale = new Vector3(s.x, s.y, 1f);
            yield return null;
        }

        clientRect.localScale = Vector3.one;
    }

    private Vector2 ComputeVelocitySquishScale(Vector2 velocity, float speed01, float amount)
    {
        if (amount <= 0f || speed01 <= 0f)
            return Vector2.one;

        Vector2 dir = velocity.sqrMagnitude > 0.0001f ? velocity.normalized : Vector2.right;

        float horiz = Mathf.Abs(dir.x);
        float vert = Mathf.Abs(dir.y);

        float stretch = 1f + (amount * speed01);
        float squash = 1f - (amount * 0.85f * speed01);

        float x = Mathf.Lerp(squash, stretch, horiz);
        float y = Mathf.Lerp(squash, stretch, vert);

        x = Mathf.Clamp(x, 0.7f, 1.35f);
        y = Mathf.Clamp(y, 0.7f, 1.35f);

        return new Vector2(x, y);
    }

    private bool IsFinite(Vector2 v)
    {
        return !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsInfinity(v.x) || float.IsInfinity(v.y));
    }

    #endregion
}
