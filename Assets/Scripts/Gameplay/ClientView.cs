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

public class ClientView : MonoBehaviour
{
    [Header("Definition")]
    public ClientDefinitionSO clientDefinition;

    [Header("Portrait")]
    [SerializeField] private Image _portraitImage;

    [Header("Mask Snap")]
    public RectTransform maskSnap;

    private GameObject _spawnedMaskGo;

    public RectTransform rectTransform { get; private set; }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    #region Public Methods

    public void SetClientDefinition(ClientDefinitionSO definition)
    {
        clientDefinition = definition;

        if (_portraitImage != null && clientDefinition != null)
            _portraitImage.sprite = clientDefinition.portraitSprite;
    }

    public void ClearMask()
    {
        if (_spawnedMaskGo != null)
        {
            Destroy(_spawnedMaskGo);
            _spawnedMaskGo = null;
        }
    }

    public void AttachMask(Sprite maskSprite)
    {
        ClearMask();

        if (maskSnap == null || maskSprite == null)
            return;

        var maskGo = new GameObject("AttachedMask", typeof(RectTransform), typeof(Image));
        var rect = maskGo.GetComponent<RectTransform>();
        rect.SetParent(maskSnap, false);

        // Make it live in the exact same rect space as the snap
        rect.anchorMin = maskSnap.anchorMin;
        rect.anchorMax = maskSnap.anchorMax;
        rect.pivot = maskSnap.pivot;

        // If the snap is stretched, offsets matter. If it's not stretched, sizeDelta matters.
        rect.anchoredPosition = maskSnap.anchoredPosition;
        rect.sizeDelta = maskSnap.sizeDelta;

        rect.offsetMin = maskSnap.offsetMin;
        rect.offsetMax = maskSnap.offsetMax;

        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;

        var image = maskGo.GetComponent<Image>();
        image.raycastTarget = false;
        image.sprite = maskSprite;
        image.preserveAspect = true;

        rect.localScale = Vector3.one * 7f;

        _spawnedMaskGo = maskGo;
    }


    #endregion
}
