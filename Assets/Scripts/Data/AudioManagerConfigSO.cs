using UnityEngine;

[CreateAssetMenu(menuName = "Project/Audio/Audio Manager Config")]
public class AudioManagerConfigSO : ScriptableObject
{
    public AudioChannelConfig music;
    public AudioChannelConfig sfx;
    public AudioChannelConfig vo;
}
