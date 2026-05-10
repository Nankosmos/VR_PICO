using UnityEngine;

public static class AudioPlayback
{
    private static AudioSource sfxSource;

    public static void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        AudioSource source = GetSfxSource();
        if (source == null) return;

        if (clip.loadState == AudioDataLoadState.Unloaded)
        {
            clip.LoadAudioData();
        }

        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            Debug.LogWarning("SFX clip is not ready to play: " + clip.name + " (" + clip.loadState + ")");
            return;
        }

        source.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private static AudioSource GetSfxSource()
    {
        if (sfxSource != null) return sfxSource;

        GameObject audioObject = new GameObject("RuntimeSfxPlayer");
        Object.DontDestroyOnLoad(audioObject);

        sfxSource = audioObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.volume = 1f;
        sfxSource.spatialBlend = 0f;
        sfxSource.priority = 64;

        return sfxSource;
    }
}
