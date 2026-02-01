using System;
using UnityEngine;

[Serializable]
public class AudioChannelConfig
{
    public float volume = 1f;
    public int priority = 128;

    [Tooltip("Seconds to fade out current audio")]
    public float fadeOutTime = 0.08f;

    [Tooltip("Seconds to fade in new audio")]
    public float fadeInTime = 0.08f;
}
