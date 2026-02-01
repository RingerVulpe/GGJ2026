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

[CreateAssetMenu(menuName = "Project/Clients/Client Queue", fileName = "ClientQueue")]
public class ClientQueueSO : ScriptableObject
{
    [Tooltip("Clients are processed in this order. When finished, the run ends.")]
    public ClientDefinitionSO[] clients;
}
