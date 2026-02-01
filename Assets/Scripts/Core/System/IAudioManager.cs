public interface IAudioManager
{
    void PlayTitleMusic();
    void PlayGameMusic();
    void StopMusic();

    void PlayClick();

    void PlayClientArrive();
    void PlayClientLeave();

    void PlayMaskAppear();
    void PlayMaskGive();

    void PlayClientRequest(UnityEngine.AudioClip clip);
    void PlayClientResponse(UnityEngine.AudioClip clip);
    void PlayClientExit(UnityEngine.AudioClip clip);
}
