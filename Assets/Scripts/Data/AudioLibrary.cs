using UnityEngine;

public class AudioLibrary : MonoBehaviour
{
    public static AudioLibrary Instance { get; private set; }

    [Header("Music")]
    public AudioClip titleMusic;
    public AudioClip gameMusic;

    [Header("UI SFX")]
    public AudioClip click;

    [Header("Client SFX")]
    public AudioClip clientArrive;
    public AudioClip clientLeave;

    [Header("Mask SFX")]
    public AudioClip maskAppear;
    public AudioClip maskGive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
