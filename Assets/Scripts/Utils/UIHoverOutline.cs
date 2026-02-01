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
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UIHoverOutline : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("Outline")]
    [SerializeField] private Color _outlineColor = Color.white;
    [SerializeField, Range(0f, 8f)] private float _outlineSize = 1.2f;

    [Header("Hover Wobble (one-time on enter)")]
    [SerializeField] private bool _useWobble = true;
    [SerializeField] private Vector2 _angleRange = new Vector2(4f, 9f);
    [SerializeField] private Vector2 _frequencyRange = new Vector2(10f, 18f);
    [SerializeField] private Vector2 _durationRange = new Vector2(0.35f, 0.6f);
    [SerializeField, Range(0f, 8f)] private float _kickDegrees = 2.5f;
    [SerializeField, Range(0.02f, 0.2f)] private float _wobbleSmoothTime = 0.045f;
    [SerializeField] private bool _randomDirection = true;

    [Header("Hover Scale")]
    [SerializeField] private bool _useScale = true;
    [SerializeField, Range(1f, 1.25f)] private float _hoverScale = 1.06f;
    [SerializeField, Range(0.02f, 0.25f)] private float _scaleSmoothTime = 0.08f;

    [Header("Click Juice")]
    [SerializeField] private bool _useClickJuice = true;
    [SerializeField, Range(0.85f, 1f)] private float _pressScale = 0.94f;
    [SerializeField, Range(1f, 1.35f)] private float _releaseOvershoot = 1.10f;
    [SerializeField, Range(0.02f, 0.18f)] private float _pressInTime = 0.05f;
    [SerializeField, Range(0.04f, 0.25f)] private float _releaseOutTime = 0.10f;

    [SerializeField, Range(0f, 10f)] private float _clickTiltDegrees = 3.5f;
    [SerializeField, Range(0.02f, 0.25f)] private float _clickTiltSmoothTime = 0.06f;

    [SerializeField, Range(0f, 6f)] private float _outlineClickBoost = 1.2f;
    [SerializeField, Range(0.02f, 0.25f)] private float _outlineBoostTime = 0.08f;

    private Image _image;
    private RectTransform _rectTransform;
    private Material _runtimeMaterial;

    private bool _isHovered;
    private bool _isPressed;

    private Vector3 _baseScale;
    private Vector3 _scaleVel;

    private float _currentZ;
    private float _zVel;

    // Wobble params (randomized on enter)
    private float _wobbleTime;
    private float _wobbleDuration;
    private float _wobbleAngle;
    private float _wobbleFrequency;
    private float _wobblePhase;
    private float _directionSign;

    // Click params
    private float _pressBlend;         
    private float _pressVel;
    private float _releaseTimer;        
    private float _outlineBoostTimer;   
    private float _clickDirSign;       

    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineSizeId = Shader.PropertyToID("_OutlineSize");

    private void Awake()
    {
        _image = GetComponent<Image>();
        _rectTransform = (RectTransform)transform;

        _runtimeMaterial = Instantiate(_image.material);
        _image.material = _runtimeMaterial;
        _image.SetMaterialDirty();
        _image.SetVerticesDirty();


        _baseScale = _rectTransform.localScale;

        SetHighlighted(false);
        SetRotationZ(0f);
    }

    private void OnDisable()
    {
        _isHovered = false;
        _isPressed = false;

        _wobbleTime = 0f;
        _releaseTimer = 0f;
        _outlineBoostTimer = 0f;
        _pressBlend = 0f;

        SetHighlighted(false);
        SetRotationZ(0f);

        if (_rectTransform != null)
            _rectTransform.localScale = _baseScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        SetHighlighted(true);

        if (_useWobble)
            StartWobble();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;

        _isPressed = false;
        SetHighlighted(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_useClickJuice)
            return;

        _isPressed = true;

        // random left/right click tilt direction
        _clickDirSign = (Random.value < 0.5f) ? -1f : 1f;

        // flash outline
        _outlineBoostTimer = _outlineBoostTime;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_useClickJuice)
            return;

        _isPressed = false;

        // start release “punch” window
        _releaseTimer = _releaseOutTime;

        // flash outline again on release
        _outlineBoostTimer = _outlineBoostTime;
    }

    private void Update() //Im using update, and I dont care. Its GGJ not a god damn AAA title. Deal with it.
    {
        float dt = Time.unscaledDeltaTime;

        //Press smoothing (so press feels snappy but not instant-pop) 
        float pressTarget = (_useClickJuice && _isPressed) ? 1f : 0f;
        _pressBlend = Mathf.SmoothDamp(_pressBlend, pressTarget, ref _pressVel, _pressInTime, Mathf.Infinity, dt);

        //Hover wobble target rotation (one-time on enter) 
        float hoverZTarget = 0f;

        if (_useWobble && _isHovered)
        {
            _wobbleTime += dt;

            float t = Mathf.Clamp01(_wobbleTime / Mathf.Max(0.0001f, _wobbleDuration));
            float decay = 1f - t;
            decay *= decay;

            if (decay > 0.0001f)
            {
                float s1 = Mathf.Sin((_wobbleTime * _wobbleFrequency) + _wobblePhase);
                float s2 = Mathf.Sin((_wobbleTime * (_wobbleFrequency * 0.62f)) + (_wobblePhase * 1.7f));
                float wobble = (s1 * 0.75f + s2 * 0.25f);

                float kick = 0f;
                if (_wobbleTime < 0.08f)
                {
                    float k = Mathf.Clamp01(_wobbleTime / 0.08f);
                    kick = Mathf.Lerp(_kickDegrees, 0f, k);
                }

                hoverZTarget = (wobble * _wobbleAngle * decay * _directionSign) + (kick * _directionSign);
            }
            else
            {
                hoverZTarget = 0f;
            }
        }

        //Click tilt (only during release punch window)
        float clickZTarget = 0f;

        if (_useClickJuice && _releaseTimer > 0f)
        {
            _releaseTimer -= dt;

            float p = 1f - Mathf.Clamp01(_releaseTimer / Mathf.Max(0.0001f, _releaseOutTime));
            float punch = Mathf.Sin(p * Mathf.PI); // 0 -> 1 -> 0

            clickZTarget = _clickTiltDegrees * _clickDirSign * punch;
        }

        // Combine rotation targets
        float zTarget = hoverZTarget + clickZTarget;

        _currentZ = Mathf.SmoothDamp(_currentZ, zTarget, ref _zVel, _clickTiltSmoothTime, Mathf.Infinity, dt);
        SetRotationZ(_currentZ);

        // ----- Scale target (hover + press + release punch) -----
        if (_useScale)
        {
            // Base hover target
            float hoverMul = _isHovered ? _hoverScale : 1f;
            Vector3 targetScale = _baseScale * hoverMul;

            // Blend into pressed scale smoothly
            if (_useClickJuice)
            {
                Vector3 pressedScale = _baseScale * _pressScale;
                targetScale = Vector3.Lerp(targetScale, pressedScale, _pressBlend);
            }

            // Release overshoot punch (adds a multiplier briefly)
            if (_useClickJuice && _releaseTimer > 0f)
            {
                float p = 1f - Mathf.Clamp01(_releaseTimer / Mathf.Max(0.0001f, _releaseOutTime));
                float punch = Mathf.Sin(p * Mathf.PI); // 0 -> 1 -> 0
                float extraMul = Mathf.Lerp(1f, _releaseOvershoot, punch);

                targetScale *= extraMul;
            }

            _rectTransform.localScale = Vector3.SmoothDamp(
                _rectTransform.localScale,
                targetScale,
                ref _scaleVel,
                _scaleSmoothTime,
                Mathf.Infinity,
                dt
            );
        }

        //Outline size (hover/press) + click flash boost
        bool wantsOutline = _isHovered || (_useClickJuice && _isPressed);

        float finalSize = 0f;

        if (wantsOutline)
        {
            finalSize = _outlineSize;

            if (_useClickJuice && _outlineBoostTimer > 0f)
            {
                _outlineBoostTimer -= dt;

                float b = Mathf.Clamp01(_outlineBoostTimer / Mathf.Max(0.0001f, _outlineBoostTime));
                finalSize += _outlineClickBoost * b;
            }
        }

        ApplyOutline(finalSize);


    }

    private void StartWobble()
    {
        _wobbleTime = 0f;

        _wobbleAngle = Random.Range(_angleRange.x, _angleRange.y);
        _wobbleFrequency = Random.Range(_frequencyRange.x, _frequencyRange.y);
        _wobbleDuration = Random.Range(_durationRange.x, _durationRange.y);
        _wobblePhase = Random.Range(0f, Mathf.PI * 2f);

        _directionSign = 1f;
        if (_randomDirection)
            _directionSign = (Random.value < 0.5f) ? -1f : 1f;
    }

    private void SetHighlighted(bool isHighlighted)
    {
        ApplyOutline(isHighlighted ? _outlineSize : 0f);
    }


    private void SetRotationZ(float zDegrees)
    {
        _rectTransform.localRotation = Quaternion.Euler(0f, 0f, zDegrees);
    }

    private void ApplyOutline(float outlineSize)
    {
        if (_image == null)
            return;

        // The actual material used by UGUI after masking/stencil modifications
        Material renderMat = _image.materialForRendering;
        if (renderMat == null)
            return;

        renderMat.SetColor(OutlineColorId, _outlineColor);
        renderMat.SetFloat(OutlineSizeId, outlineSize);
    }

}
