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
using UnityEngine.UI;

public class MaskSelectionTravelFX : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Canvas _rootCanvas;
    [SerializeField] private RectTransform _fxParent;

    [Tooltip("Assign this to Panel_SelectedMask/SelectedMask_Display (the visible slot image object).")]
    [SerializeField] private RectTransform _slotDisplayTarget;

    [Header("Ghost Look")]
    [SerializeField, Range(0.35f, 1f)] private float _ghostStartAlpha = 0.92f;
    [SerializeField, Range(0.05f, 0.65f)] private float _ghostEndAlpha = 0.05f;

    [Header("Pop")]
    [SerializeField, Range(0.03f, 0.5f)] private float _popDuration = 0.08f;
    [SerializeField, Range(0.00f, 0.12f)] private float _popHold = 0.02f;
    [SerializeField, Range(0.90f, 2.0f)] private float _popStartScale = 0.92f;
    [SerializeField, Range(1.00f, 2.0f)] private float _popPeakScale = 1.20f;
    [SerializeField, Range(0.00f, 18f)] private float _popRotateJitter = 6f;

    [Header("Travel")]
    [SerializeField, Range(0.08f, 1.0f)] private float _travelDuration = 0.20f;
    [SerializeField, Range(0.75f, 2.0f)] private float _travelStartScale = 1.06f;
    [SerializeField, Range(0.70f, 1.35f)] private float _travelEndScale = 1.00f;

    [Header("Arc Shape")]
    [SerializeField, Range(0f, 180f)] private float _arcPixels = 28f;
    [SerializeField, Range(0f, 3.0f)] private float _arcByDistance = 0.85f;
    [SerializeField, Range(0f, 150f)] private float _arcRandomPixels = 26f;
    [SerializeField, Range(0f, 120f)] private float _sideJitterPixels = 18f;
    [SerializeField, Range(0f, 60f)] private float _snapNearEndPixels = 16f;

    [Header("Motion Polish")]
    [SerializeField, Range(0f, 30f)] private float _bankDegrees = 10f;
    [SerializeField, Range(0f, 35f)] private float _rollJitterDegrees = 6f;
    [SerializeField, Range(0f, 0.30f)] private float _rollJitterSpeed = 0.12f;

    [Header("Handoff")]
    [SerializeField] private bool _hideSlotDuringTravel = true;
    [SerializeField, Range(0.02f, 0.5f)] private float _handoffWindow = 0.08f;

    [Header("Landing Wobble")]
    [SerializeField, Range(0f, 0.35f)] private float _landDuration = 0.16f;
    [SerializeField, Range(0.02f, 0.22f)] private float _landScaleAmount = 0.10f;
    [SerializeField, Range(0f, 18f)] private float _landRotateDegrees = 6f;
    [SerializeField, Range(10f, 70f)] private float _landFrequency = 28f;
    [SerializeField, Range(0.6f, 3.0f)] private float _landDamping = 1.45f;

    [Header("Curves")]
    [SerializeField] private AnimationCurve _travelCurve = EaseInCubicCurve();
    [SerializeField] private AnimationCurve _popCurve = EaseOutBackCurve();

    private Coroutine _routine;
    private int _runId;
    private Vector3 _slotBaseScale = Vector3.one;
    private Quaternion _slotBaseRot = Quaternion.identity;
    private bool _hasCachedSlotBase;

    private GameObject _activeGhostGo;

    public bool isPlaying => _routine != null;

    #region Unity

    private void OnDisable()
    {
        Stop();
    }

    #endregion

    #region Public

    public void Play(Sprite sprite, RectTransform from, Action onArrive)
    {
        if (sprite == null || from == null || _slotDisplayTarget == null)
        {
            onArrive?.Invoke();
            return;
        }

        Stop();              
        _runId++;

        _routine = StartCoroutine(CoPlay(sprite, from, onArrive, _runId));
    }

    public void Stop()
    {
        if (_routine != null)
            StopCoroutine(_routine);

        _routine = null;

        CleanupGhost();


        if (_slotDisplayTarget != null)
        {
            CanvasGroup slotCg = GetOrAddCanvasGroup(_slotDisplayTarget);
            slotCg.alpha = 1f;

            Image slotImg = _slotDisplayTarget.GetComponent<Image>();
            if (slotImg != null)
            {
                Color c = slotImg.color;
                c.a = 1f;
                slotImg.color = c;
            }

            if (_hasCachedSlotBase)
            {
                _slotDisplayTarget.localScale = _slotBaseScale;
                _slotDisplayTarget.localRotation = _slotBaseRot;
            }
            else
            {
                _slotDisplayTarget.localScale = Vector3.one;
                _slotDisplayTarget.localRotation = Quaternion.identity;
            }
        }
    }


    #endregion

    #region Core

    private IEnumerator CoPlay(Sprite sprite, RectTransform from, Action onArrive, int runId)
    {
        EnsureSetup();

        Image slotImg = _slotDisplayTarget.GetComponent<Image>();
        if (slotImg == null)
        {
            onArrive?.Invoke();
            _routine = null;
            yield break;
        }

        // Apply sprite to slot display.
        slotImg.sprite = sprite;
        slotImg.preserveAspect = true;

        CanvasGroup slotCg = GetOrAddCanvasGroup(_slotDisplayTarget);

        // Force the slot to a known-good visual state every run.
        slotCg.alpha = 1f;

        Color slotColor = slotImg.color;
        slotColor.a = 1f;
        slotImg.color = slotColor;

        // We ALWAYS want to land at full alpha for the selected display.
        float slotAlphaBefore = 1f;

        if (_hideSlotDuringTravel)
            slotCg.alpha = 1f;


        // Create ghost (and register it as active so Stop() can delete it).
        CreateGhost(sprite, from, out RectTransform ghostRt, out CanvasGroup ghostCg);

        Vector2 startPos = WorldToFxLocal(from);
        Vector2 endPos = WorldToFxLocal(_slotDisplayTarget);

        ghostRt.anchoredPosition = startPos;

        Vector3 slotScaleStart = _slotDisplayTarget.localScale;
        Quaternion slotRotStart = _slotDisplayTarget.localRotation;

        // Pop in.
        yield return CoPopIn(runId, ghostRt, ghostCg);
        if (runId != _runId) yield break;

        if (_popHold > 0f)
        {
            float hold = 0f;
            while (hold < _popHold)
            {
                if (runId != _runId) yield break;
                hold += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        // Arc.
        BuildArc(startPos, endPos, out Vector2 p0, out Vector2 p1, out Vector2 p2, out Vector2 p3, out float sideSign);

        float travelDur = Mathf.Max(0.0001f, _travelDuration);
        float handoffStart = Mathf.Max(0f, travelDur - _handoffWindow);

        float t = 0f;
        Vector2 prevPos = p0;

        float rollSeed = UnityEngine.Random.Range(-999f, 999f);

        while (t < travelDur)
        {
            if (runId != _runId) yield break;

            t += Time.unscaledDeltaTime;
            float raw = Mathf.Clamp01(t / travelDur);

            float u = (_travelCurve != null) ? _travelCurve.Evaluate(raw) : raw;
            Vector2 pos = Bezier(p0, p1, p2, p3, u);

            if (_snapNearEndPixels > 0.001f)
            {
                float remaining = (endPos - pos).magnitude;
                float close01 = Mathf.Clamp01(1f - (remaining / _snapNearEndPixels));
                pos = Vector2.Lerp(pos, endPos, close01);
            }

            ghostRt.anchoredPosition = pos;

            // Bank + roll.
            float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            Vector2 v = (pos - prevPos) / dt;
            prevPos = pos;

            float speed01 = Mathf.Clamp01(v.magnitude / 2400f);
            float bank = Mathf.Lerp(0f, _bankDegrees, speed01) * sideSign;

            float rollNoise = 0f;
            if (_rollJitterDegrees > 0.001f)
            {
                float rollT = (Time.unscaledTime + rollSeed) / Mathf.Max(0.001f, _rollJitterSpeed);
                rollNoise = (Mathf.PerlinNoise(rollT, 0.13f) - 0.5f) * 2f;
            }

            ghostRt.localRotation = Quaternion.Euler(0f, 0f, (-bank) + (rollNoise * _rollJitterDegrees));

            // Scale.
            float sc = Mathf.Lerp(_travelStartScale, _travelEndScale, Smooth01(raw));
            ghostRt.localScale = Vector3.one * sc;

            // Alpha tail.
            ghostCg.alpha = Mathf.Lerp(_ghostStartAlpha, _ghostEndAlpha, Mathf.Clamp01((raw - 0.70f) / 0.30f));

            // Handoff: crossfade + shake the SLOT DISPLAY.
            if (_hideSlotDuringTravel && t >= handoffStart)
            {
                float hp = Mathf.InverseLerp(handoffStart, travelDur, t);

                ghostCg.alpha = Mathf.Lerp(ghostCg.alpha, 0f, hp);

                float slotTargetAlpha = (slotAlphaBefore <= 0f) ? 1f : slotAlphaBefore;
                slotCg.alpha = Mathf.Lerp(0f, slotTargetAlpha, hp);

                ApplyLandingDuringHandoff(_slotDisplayTarget, slotScaleStart, slotRotStart, hp);
            }

            yield return null;
        }

        if (runId != _runId) yield break;

        // Final snap.
        if (_hideSlotDuringTravel)
            slotCg.alpha = (slotAlphaBefore <= 0f) ? 1f : slotAlphaBefore;

        CleanupGhost();

        // Final wobble on slot display.
        yield return CoLandingWobble(runId, _slotDisplayTarget, slotScaleStart, slotRotStart);
        if (runId != _runId) yield break;

        onArrive?.Invoke();
        _routine = null;
    }

    private void CreateGhost(Sprite sprite, RectTransform from, out RectTransform ghostRt, out CanvasGroup ghostCg)
    {
        CleanupGhost();

        _activeGhostGo = new GameObject("MaskGhostFX", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        ghostRt = _activeGhostGo.GetComponent<RectTransform>();
        ghostRt.SetParent(_fxParent, worldPositionStays: false);
        ghostRt.SetAsLastSibling();
        ghostRt.anchorMin = ghostRt.anchorMax = new Vector2(0.5f, 0.5f);
        ghostRt.pivot = new Vector2(0.5f, 0.5f);

        Image ghostImg = _activeGhostGo.GetComponent<Image>();
        ghostImg.sprite = sprite;
        ghostImg.preserveAspect = true;
        ghostImg.raycastTarget = false;
        ghostImg.color = Color.white;

        ghostCg = _activeGhostGo.GetComponent<CanvasGroup>();
        ghostCg.alpha = 0f;

        ghostRt.sizeDelta = from.rect.size;
        ghostRt.localScale = Vector3.one;
        ghostRt.localRotation = Quaternion.identity;
    }

    private void CleanupGhost()
    {
        if (_activeGhostGo != null)
            Destroy(_activeGhostGo);

        _activeGhostGo = null;
    }

    private IEnumerator CoPopIn(int runId, RectTransform ghostRt, CanvasGroup ghostCg)
    {
        float dur = Mathf.Max(0.0001f, _popDuration);
        float t = 0f;

        float rotJitter = UnityEngine.Random.Range(-_popRotateJitter, _popRotateJitter);

        while (t < dur)
        {
            if (runId != _runId) yield break;

            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / dur);

            ghostCg.alpha = Mathf.Lerp(0f, _ghostStartAlpha, EaseOutCubic(p));

            float shaped = (_popCurve != null) ? _popCurve.Evaluate(p) : p;
            float scaleUp = Mathf.Lerp(_popStartScale, _popPeakScale, shaped);
            float scaleDown = Mathf.Lerp(_popPeakScale, _travelStartScale, Smooth01(p));
            float sc = Mathf.Lerp(scaleUp, scaleDown, p);

            ghostRt.localScale = Vector3.one * sc;

            float z = Mathf.Lerp(rotJitter, 0f, Smooth01(p));
            ghostRt.localRotation = Quaternion.Euler(0f, 0f, z);

            yield return null;
        }

        ghostCg.alpha = _ghostStartAlpha;
        ghostRt.localScale = Vector3.one * _travelStartScale;
        ghostRt.localRotation = Quaternion.identity;
    }

    private void ApplyLandingDuringHandoff(RectTransform slotRt, Vector3 baseScale, Quaternion baseRot, float hp01)
    {
        float p = Smooth01(hp01);

        float damp = Mathf.Exp(-_landDamping * (p * 0.35f));
        float wave = Mathf.Sin((p * 0.35f) * _landFrequency) * damp;

        float settleScale = Mathf.Lerp(1f + (_landScaleAmount * 0.55f), 1f, p);
        float scaleWave = 1f + (wave * _landScaleAmount);
        float rotWave = wave * _landRotateDegrees;

        slotRt.localScale = baseScale * (settleScale * scaleWave);
        slotRt.localRotation = baseRot * Quaternion.Euler(0f, 0f, rotWave);
    }

    private IEnumerator CoLandingWobble(int runId, RectTransform slotRt, Vector3 baseScale, Quaternion baseRot)
    {
        if (slotRt == null)
            yield break;

        float dur = Mathf.Max(0.0001f, _landDuration);
        float t = 0f;

        while (t < dur)
        {
            if (runId != _runId) yield break;

            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / dur);

            float decay = Mathf.Exp(-_landDamping * p);
            float wave = Mathf.Sin(p * _landFrequency) * decay;

            float scaleWave = 1f + (wave * _landScaleAmount);
            float rotWave = wave * _landRotateDegrees;

            slotRt.localScale = baseScale * scaleWave;
            slotRt.localRotation = baseRot * Quaternion.Euler(0f, 0f, rotWave);

            yield return null;
        }

        slotRt.localScale = baseScale;
        slotRt.localRotation = baseRot;
    }

    #endregion

    #region Setup + Space Conversion

    private void EnsureSetup()
    {
        if (_rootCanvas == null)
            _rootCanvas = GetComponentInParent<Canvas>();

        if (_fxParent == null)
        {
            GameObject fx = new GameObject("FXOverlay", typeof(RectTransform));
            _fxParent = fx.GetComponent<RectTransform>();
            _fxParent.SetParent(_rootCanvas.transform, worldPositionStays: false);
            _fxParent.anchorMin = Vector2.zero;
            _fxParent.anchorMax = Vector2.one;
            _fxParent.offsetMin = Vector2.zero;
            _fxParent.offsetMax = Vector2.zero;
            _fxParent.SetAsLastSibling();
        }

        _fxParent.SetAsLastSibling();

        // Cache the slot baseline so Stop() can always restore visuals.
        if (!_hasCachedSlotBase && _slotDisplayTarget != null)
        {
            _slotBaseScale = _slotDisplayTarget.localScale;
            _slotBaseRot = _slotDisplayTarget.localRotation;
            _hasCachedSlotBase = true;
        }
    }


    private Vector2 WorldToFxLocal(RectTransform source)
    {
        Vector3 world = source.TransformPoint(source.rect.center);

        Camera cam = null;
        if (_rootCanvas != null && _rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = _rootCanvas.worldCamera;

        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, world);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _fxParent,
            screen,
            cam,
            out Vector2 local
        );

        return local;
    }

    private static CanvasGroup GetOrAddCanvasGroup(RectTransform rt)
    {
        CanvasGroup cg = rt.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = rt.gameObject.AddComponent<CanvasGroup>();
        return cg;
    }

    #endregion

    #region Math Helpers

    private void BuildArc(Vector2 startPos, Vector2 endPos, out Vector2 p0, out Vector2 p1, out Vector2 p2, out Vector2 p3, out float sideSign)
    {
        p0 = startPos;
        p3 = endPos;

        Vector2 to = endPos - startPos;
        float dist = Mathf.Max(1f, to.magnitude);
        Vector2 dir = to / dist;
        Vector2 perp = new Vector2(-dir.y, dir.x);

        sideSign = (UnityEngine.Random.value < 0.5f) ? -1f : 1f;

        float arcBase = _arcPixels + (dist * 0.03f * _arcByDistance);
        float arcRand = UnityEngine.Random.Range(-_arcRandomPixels, _arcRandomPixels);
        float arc = Mathf.Clamp(arcBase + arcRand, 0f, 220f) * sideSign;

        float sideJitter = UnityEngine.Random.Range(-_sideJitterPixels, _sideJitterPixels);

        float earlyPush = dist * UnityEngine.Random.Range(0.18f, 0.30f);
        float latePush = dist * UnityEngine.Random.Range(0.62f, 0.82f);

        p1 = p0 + dir * earlyPush + perp * (arc * UnityEngine.Random.Range(0.75f, 1.05f)) + (perp * sideJitter * 0.35f);
        p2 = p0 + dir * latePush + perp * (arc * UnityEngine.Random.Range(0.05f, 0.35f)) + (perp * sideJitter * 0.10f);
    }

    private static Vector2 Bezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float u = 1f - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector2 p = uuu * p0;
        p += (3f * uu * t) * p1;
        p += (3f * u * tt) * p2;
        p += ttt * p3;
        return p;
    }

    private static float Smooth01(float x) => x * x * (3f - 2f * x);
    private static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);

    private static AnimationCurve EaseInCubicCurve()
    {
        return new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0.2f),
            new Keyframe(1f, 1f, 2.5f, 0f)
        );
    }

    private static AnimationCurve EaseOutBackCurve()
    {
        return new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.55f, 1.08f),
            new Keyframe(1f, 1f)
        );
    }

    #endregion
}
