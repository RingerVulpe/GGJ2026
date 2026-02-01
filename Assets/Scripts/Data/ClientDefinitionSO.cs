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

[CreateAssetMenu(menuName = "Project/Clients/Client Definition", fileName = "ClientDefinition")]
public class ClientDefinitionSO : ScriptableObject
{
    public string id;
    public string displayName;

    [Header("Visuals")]
    public Sprite portraitSprite;

    [Header("Request Bubble (shown to player)")]
    [TextArea(2, 6)] public string requestText;

    [Header("Hidden Requested Mask (used for judging)")]
    public MaskDefinitionSO requestedMask;

    [Header("Responses (shown to player)")]
    [TextArea(2, 4)] public string responseIncorrect;
    [TextArea(2, 4)] public string responsePartial;
    [TextArea(2, 4)] public string responseCorrect;
}
