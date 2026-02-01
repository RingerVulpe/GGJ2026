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

[CreateAssetMenu(menuName = "Project/Masks/Mask Library", fileName = "MaskLibrary")]
public class MaskLibrarySO : ScriptableObject
{
    [Tooltip("These are the masks that can appear in the selection display.")]
    public MaskDefinitionSO[] masks;
}
