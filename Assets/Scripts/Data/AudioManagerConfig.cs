using System;

[Serializable]
public class AudioManagerConfig
{
    public AudioChannelConfig music = new AudioChannelConfig
    {
        volume = 0.7f,
        priority = 64,
        fadeOutTime = 0.25f,
        fadeInTime = 0.35f
    };

    public AudioChannelConfig sfx = new AudioChannelConfig
    {
        volume = 1.0f,
        priority = 128,
        fadeOutTime = 0f,   // SFX never fade OUT
        fadeInTime = 0f
    };

    public AudioChannelConfig vo = new AudioChannelConfig
    {
        volume = 1.0f,
        priority = 96,
        fadeOutTime = 0.12f,
        fadeInTime = 0.05f
    };
}
