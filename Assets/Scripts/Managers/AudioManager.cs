using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour, IAudioManager
{
    public static IAudioManager Instance // dont ask. :)
    {
        get { return ServiceLocator.Get<IAudioManager>(); }
    }

    private AudioSource _musicSource;
    private AudioSource _sfxSource;
    private AudioSource _voSource;

    private AudioLibrary _library;
    private AudioManagerConfig _config;

    private Coroutine _musicFadeRoutine;
    private Coroutine _voFadeRoutine;

    private bool _isInitialized;

    #region Init

    public void Initialize(AudioManagerConfig config, AudioLibrary library)
    {
        if (_isInitialized)
            return;

        _library = library;
        _config = config ?? new AudioManagerConfig();

        _musicSource = CreateSource("MusicSource", _config.music);
        _sfxSource = CreateSource("SFXSource", _config.sfx);
        _voSource = CreateSource("VOSource", _config.vo);
        PlayTitleMusic();
        _isInitialized = true;
        Debug.Log("[AudioManager] Initialized (soft-fade enabled).");
    }

    private AudioSource CreateSource(string name, AudioChannelConfig cfg)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);

        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.volume = cfg.volume;
        src.priority = cfg.priority;
        src.spatialBlend = 0f; // FORCE 2D

        return src;
    }

    private bool CanPlay() => _isInitialized && _library != null;

    #endregion

    #region Music

    public void PlayTitleMusic() => PlayMusic(_library.titleMusic);
    public void PlayGameMusic() => PlayMusic(_library.gameMusic);

    private void PlayMusic(AudioClip clip)
    {
        if (!CanPlay() || clip == null)
            return;

        if (_musicSource.clip == clip && _musicSource.isPlaying)
            return;

        if (_musicFadeRoutine != null)
            StopCoroutine(_musicFadeRoutine);

        _musicFadeRoutine = StartCoroutine(FadeAndSwap(
            _musicSource,
            clip,
            _config.music.fadeOutTime,
            _config.music.fadeInTime,
            loop: true
        ));
    }

    public void StopMusic()
    {
        if (!CanPlay())
            return;

        if (_musicFadeRoutine != null)
            StopCoroutine(_musicFadeRoutine);

        _musicFadeRoutine = StartCoroutine(FadeOutOnly(
            _musicSource,
            _config.music.fadeOutTime
        ));
    }

    #endregion

    #region SFX (never cut, never faded)

    public void PlayClick() => PlaySfx(_library.click);
    public void PlayBookWoosh() => PlaySfx(_library.bookWoosh);
    public void PlayClientArrive() => PlaySfx(_library.clientArrive);
    public void PlayClientLeave() => PlaySfx(_library.clientLeave);
    public void PlayMaskAppear() => PlaySfx(_library.maskAppear);
    public void PlayMaskGive() => PlaySfx(_library.maskGive);

    private void PlaySfx(AudioClip clip)
    {
        if (!CanPlay() || clip == null)
            return;

        // Audible but still natural variation
        const float pitchVariance = 0.10f; // +/- 10%
        float originalPitch = _sfxSource.pitch;

        _sfxSource.pitch = Random.Range(
            1f - pitchVariance,
            1f + pitchVariance
        );

        _sfxSource.PlayOneShot(clip, _config.sfx.volume);

        // Reset immediately so future calls start clean
        _sfxSource.pitch = originalPitch;
    }



    #endregion

    #region VO (soft replace)

    public void PlayClientRequest(AudioClip clip) => PlayVo(clip);
    public void PlayClientResponse(AudioClip clip) => PlayVo(clip);
    public void PlayClientExit(AudioClip clip) => PlayVo(clip);

    private void PlayVo(AudioClip clip)
    {
        if (!CanPlay() || clip == null)
            return;

        if (_voFadeRoutine != null)
            StopCoroutine(_voFadeRoutine);

        _voFadeRoutine = StartCoroutine(FadeAndSwap(
            _voSource,
            clip,
            _config.vo.fadeOutTime,
            _config.vo.fadeInTime,
            loop: false
        ));
    }

    #endregion

    #region Fade Utilities

    private IEnumerator FadeAndSwap(
        AudioSource source,
        AudioClip newClip,
        float fadeOut,
        float fadeIn,
        bool loop)
    {
        float startVol = source.volume;

        // Fade out
        if (source.isPlaying && fadeOut > 0f)
        {
            float t = 0f;
            while (t < fadeOut)
            {
                t += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVol, 0f, t / fadeOut);
                yield return null;
            }
        }

        source.Stop();
        source.clip = newClip;
        source.loop = loop;
        source.volume = 0f;
        source.Play();

        // Fade in
        if (fadeIn > 0f)
        {
            float t = 0f;
            while (t < fadeIn)
            {
                t += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(0f, startVol, t / fadeIn);
                yield return null;
            }
        }

        source.volume = startVol;
    }

    private IEnumerator FadeOutOnly(AudioSource source, float fadeOut)
    {
        if (!source.isPlaying)
            yield break;

        float startVol = source.volume;
        float t = 0f;

        while (t < fadeOut)
        {
            t += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(startVol, 0f, t / fadeOut);
            yield return null;
        }

        source.Stop();
        source.clip = null;
        source.volume = startVol;
    }

    #endregion
}
