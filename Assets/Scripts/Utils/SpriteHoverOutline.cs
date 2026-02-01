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

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteHoverOutline : MonoBehaviour
{
    [Header("Outline")]
    [SerializeField] private bool _startHighlighted = false;
    [SerializeField] private Color _outlineColor = Color.white;
    [SerializeField, Range(0f, 8f)] private float _outlineSize = 1f;

    private SpriteRenderer _sr;
    private MaterialPropertyBlock _mpb;

    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineSizeId = Shader.PropertyToID("_OutlineSize");

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _mpb = new MaterialPropertyBlock();
        SetHighlighted(_startHighlighted);
    }

    private void OnMouseEnter() => SetHighlighted(true);
    private void OnMouseExit() => SetHighlighted(false);

    public void SetHighlighted(bool isHighlighted)
    {
        _sr.GetPropertyBlock(_mpb);

        if (isHighlighted)
        {
            _mpb.SetColor(OutlineColorId, _outlineColor);
            _mpb.SetFloat(OutlineSizeId, _outlineSize);
        }
        else
        {
            // Disable outline by setting size to 0 (fast + simple)
            _mpb.SetFloat(OutlineSizeId, 0f);
        }

        _sr.SetPropertyBlock(_mpb);
    }
}
