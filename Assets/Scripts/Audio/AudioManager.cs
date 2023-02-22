using UnityEngine.Audio;
using System;
using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    [SerializeField]
    private static AudioManager m_instance;
    [SerializeField]
    private AudioMixerGroup[] m_mixerGroups;

    [SerializeField]
    private GameObject m_audioObjectPrefab;

    private static readonly Vector3 ZERO3;

    void Awake()
    {
        if (m_instance)
            Destroy(m_instance);

        m_instance = this;
    }

    private void Start()
    {
        
    }

    public void Play(string sound)
    {

    }



    public void Stop(string sound)
    {

    }

    public void PlayAmbient(string sound)
    {

    }

    public void SetPitch(float pitch)
    {

    }

    public void SetVolume(float vol)
    {

    }

    public void SetDarkAmbientVolume(float vol)
    {

    }

    public static GfAudioSource GetAudioObject(Transform parent = null)
    {
        var src = GfPooling.PoolInstantiate(m_instance.m_audioObjectPrefab).GetComponent<GfAudioSource>();
        src.SetParent(parent);
        return src;
    }

    public static AudioMixerGroup GetMixerGroup(uint index)
    {
        AudioMixerGroup ret = null;
        int length = m_instance.m_mixerGroups.Length;
        if (length > 0)
            if (length > index)
                ret = m_instance.m_mixerGroups[index];
            else
                ret = m_instance.m_mixerGroups[0]; //if the type has no mixer group, give default mixer group

        return ret;
    }

    public static AudioMixerGroup GetMixerGroup(AudioMixerType index)
    {
        return GetMixerGroup((uint)index);
    }

    public static void PlayAudio(AudioSource audio, float delay = 0, float volume = -1, float pitch = -1, bool loop = false, AudioMixerType mixerType = AudioMixerType.DEFAULT)
    {
        PlayAudio(audio, delay, volume, pitch, loop, GetMixerGroup(mixerType));
    }

    public static void PlayAudio(AudioSource audio, float delay = 0, float volume = -1, float pitch = -1, bool loop = false, AudioMixerGroup mixer = null)
    {
        audio.volume = volume;
        audio.pitch = pitch;
        audio.loop = loop;

        if (mixer)
            audio.outputAudioMixerGroup = mixer;

        if (delay > 0)
            audio.PlayDelayed(delay);
        else
            audio.Play();
    }

    public static GfAudioSource PlayAudio(AudioClip audio, Transform parent, float delay = 0, float volume = 1, float pitch = 1, bool loop = false, AudioMixerType mixerType = AudioMixerType.DEFAULT, float spatialBlend = 1, float minDst = 1, float maxDst = 500)
    {
        return InternalPlayAudio(audio, parent, ZERO3, delay, volume, pitch, loop, GetMixerGroup(mixerType), spatialBlend, minDst, maxDst);
    }

    public static GfAudioSource PlayAudio(AudioClip audio, Transform parent, float delay = 0, float volume = 1, float pitch = 1, bool loop = false, AudioMixerGroup mixer = null, float spatialBlend = 1, float minDst = 1, float maxDst = 500)
    {
        return InternalPlayAudio(audio, parent, ZERO3, delay, volume, pitch, loop, mixer, spatialBlend, minDst, maxDst);
    }

    public static GfAudioSource PlayAudio(AudioClip audio, Vector3 position, float delay = 0, float volume = 1, float pitch = 1, bool loop = false, AudioMixerType mixerType = AudioMixerType.DEFAULT, float spatialBlend = 1, float minDst = 1, float maxDst = 500)
    {
        return InternalPlayAudio(audio, null, position, delay, volume, pitch, loop, GetMixerGroup(mixerType), spatialBlend, minDst, maxDst);
    }

    public static GfAudioSource PlayAudio(AudioClip audio, Vector3 position, ulong delay = 0, float volume = 1, float pitch = 1, bool loop = false, AudioMixerGroup mixer = null, float spatialBlend = 1, float minDst = 1, float maxDst = 500)
    {
        return InternalPlayAudio(audio, null, position, delay, volume, pitch, loop, mixer, spatialBlend, minDst, maxDst);
    }

    public static GfAudioSource PlayAudio(Sound audio, Vector3 position, float delay = 0, float volume = 1, float pitch = 1)
    {
        double timeAsDouble = Time.timeAsDouble;
        if (audio.CanPlay(timeAsDouble))
        {
            audio.Played(timeAsDouble, delay);
            return InternalPlayAudio(audio.m_clip, null, position, delay, volume * audio.GetVolume(), pitch * audio.GetPitch(), audio.m_loop, audio.GetAudioMixerGroup(), audio.m_spatialBlend, audio.m_minDistance, audio.m_maxDistance);
        }

        return null;
    }

    public static GfAudioSource PlayAudio(Sound audio, Transform parent, ulong delay = 0, float volume = 1, float pitch = 1, AudioMixerGroup mixer = null)
    {
        double timeAsDouble = Time.timeAsDouble;
        if (audio.CanPlay(timeAsDouble))
        {
            audio.Played(timeAsDouble, delay);
            return InternalPlayAudio(audio.m_clip, parent, ZERO3, delay, volume * audio.GetVolume(), pitch * audio.GetPitch(), audio.m_loop, mixer, audio.m_spatialBlend, audio.m_minDistance, audio.m_maxDistance);
        }

        return null;
    }

    private static GfAudioSource InternalPlayAudio(AudioClip audio, Transform parent, Vector3 position, float delay = 0, float volume = 1, float pitch = 1, bool loop = false, AudioMixerGroup mixer = null, float spatialBlend = 1, float minDst = 1, float maxDst = 500)
    {
        GfAudioSource gfAudioSource = GetAudioObject();
        gfAudioSource.m_destroyWhenFinished = true;

        AudioSource source = gfAudioSource.GetAudioSource();
        gfAudioSource.SetParent(parent);
        if (null == parent)
            gfAudioSource.transform.position = position;

        source.clip = audio;
        source.outputAudioMixerGroup = mixer;
        source.pitch = pitch;
        source.volume = volume;
        source.spatialBlend = spatialBlend;
        source.maxDistance = minDst;
        source.minDistance = maxDst;

        if (delay > 0)
            source.PlayDelayed(delay);
        else
            source.Play();

        return gfAudioSource;
    }

}

public enum AudioMixerType
{
    DEFAULT,
    MUSIC
}