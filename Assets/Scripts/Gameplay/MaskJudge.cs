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
public static class MaskJudge
{
    public static int GetMatchCount(ClientDefinitionSO client, MaskDefinitionSO chosenMask)
    {
        if (client == null || chosenMask == null || client.requestedMask == null)
            return 0;

        var requested = client.requestedMask;

        int matches = 0;

        if (requested.style != null && requested.style == chosenMask.style)
            matches++;

        if (requested.color != null && requested.color == chosenMask.color)
            matches++;

        if (requested.shape != null && requested.shape == chosenMask.shape)
            matches++;

        return matches;
    }

    public static string GetResponseText(ClientDefinitionSO client, int matchCount)
    {
        if (client == null)
            return string.Empty;

        if (matchCount >= 3)
            return client.responseCorrect;

        if (matchCount >= 1)
            return client.responsePartial;

        return client.responseIncorrect;
    }
}
