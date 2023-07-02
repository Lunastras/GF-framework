using UnityEngine.Audio;
using System;
using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    private static AudioManager Instance = null;

    [SerializeField]
    private AudioMixerGroup[] m_mixerGroups = null;

    [SerializeField]
    private string[] m_mixerGroupsExposedVolumeString = null;

    [SerializeField]
    private GameObject m_audioObjectPrefab = null;

    [SerializeField]
    private float m_maxVolumeDecibels = 0;

    [SerializeField]
    private float m_minVolumeDecibels = -80;

    private static readonly Vector3 ZERO3 = Vector3.zero;

    void Awake()
    {
        if (Instance)
            Destroy(Instance);

        Instance = this;
    }

    public static float ValueToVolume(float value)
    {
        return Mathf.Log10(Mathf.Clamp(value, 0.00001f, 1f)) * ((Instance.m_maxVolumeDecibels - Instance.m_minVolumeDecibels) * 0.25f + Instance.m_maxVolumeDecibels);
    }

    public static void SetMixerVolume(AudioMixerType type, float volume)
    {
        int index = (int)type;
        Instance.m_mixerGroups[index].audioMixer.SetFloat(Instance.m_mixerGroupsExposedVolumeString[index], ValueToVolume(volume));
    }

    public static void SetMixerVolumeRaw(AudioMixerType type, float volume)
    {
        int index = (int)type;
        bool worked = Instance.m_mixerGroups[index].audioMixer.SetFloat(Instance.m_mixerGroupsExposedVolumeString[index], volume);
    }

    public static float GetMixerVolumeRaw(AudioMixerType type)
    {
        int index = (int)type;
        Instance.m_mixerGroups[index].audioMixer.GetFloat(Instance.m_mixerGroupsExposedVolumeString[index], out float val);
        return val;
    }

    public static float GetMixerVolume(AudioMixerType type)
    {
        int index = (int)type; //todo
        Instance.m_mixerGroups[index].audioMixer.GetFloat(Instance.m_mixerGroupsExposedVolumeString[index], out float val);
        return val;
    }


    public static GfAudioSource GetAudioObject(Transform parent = null)
    {
        var src = GfPooling.PoolInstantiate(Instance.m_audioObjectPrefab).GetComponent<GfAudioSource>();
        src.SetParent(parent);
        return src;
    }

    public static AudioMixerGroup GetMixerGroup(uint index)
    {
        AudioMixerGroup ret = null;
        int length = Instance.m_mixerGroups.Length;
        if (length > 0)
            if (length > index)
                ret = Instance.m_mixerGroups[index];
            else
                ret = Instance.m_mixerGroups[0]; //if the type has no mixer group, give default mixer group

        return ret;
    }

    public static AudioMixerGroup GetMixerGroup(AudioMixerType index)
    {
        return GetMixerGroup((uint)index);
    }

    public static void PlayAudio(AudioSource audio, float delay = 0, float volume = -1, float pitch = -1, bool loop = false, AudioMixerType mixerType = AudioMixerType.FX_DEFAULT)
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

    public static GfAudioSource PlayAudio(AudioClip audio, Transform parent, float delay = 0, float volume = 1, float pitch = 1, bool loop = false, AudioMixerType mixerType = AudioMixerType.FX_DEFAULT, float spatialBlend = 1, float minDst = 1, float maxDst = 500)
    {
        return InternalPlayAudio(audio, parent, ZERO3, delay, volume, pitch, loop, GetMixerGroup(mixerType), spatialBlend, minDst, maxDst);
    }

    public static GfAudioSource PlayAudio(AudioClip audio, Transform parent, float delay = 0, float volume = 1, float pitch = 1, bool loop = false, AudioMixerGroup mixer = null, float spatialBlend = 1, float minDst = 1, float maxDst = 500)
    {
        return InternalPlayAudio(audio, parent, ZERO3, delay, volume, pitch, loop, mixer, spatialBlend, minDst, maxDst);
    }

    public static GfAudioSource PlayAudio(AudioClip audio, Vector3 position, float delay = 0, float volume = 1, float pitch = 1, bool loop = false, AudioMixerType mixerType = AudioMixerType.FX_DEFAULT, float spatialBlend = 1, float minDst = 1, float maxDst = 500)
    {
        return InternalPlayAudio(audio, null, position, delay, volume, pitch, loop, GetMixerGroup(mixerType), spatialBlend, minDst, maxDst);
    }

    public static GfAudioSource PlayAudio(AudioClip audio, Vector3 position, float delay = 0, float volume = 1, float pitch = 1, bool loop = false, AudioMixerGroup mixer = null, float spatialBlend = 1, float minDst = 1, float maxDst = 500)
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

    public static GfAudioSource PlayAudio(Sound audio, Transform parent, float delay = 0, float volume = 1, float pitch = 1)
    {
        double timeAsDouble = Time.timeAsDouble;
        if (audio.CanPlay(timeAsDouble))
        {
            audio.Played(timeAsDouble, delay);
            return InternalPlayAudio(audio.m_clip, parent, ZERO3, delay, volume * audio.GetVolume(), pitch * audio.GetPitch(), audio.m_loop, audio.GetAudioMixerGroup(), audio.m_spatialBlend, audio.m_minDistance, audio.m_maxDistance);
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
        source.loop = loop;

        if (delay > 0)
            source.PlayDelayed(delay);
        else
            source.Play();

        return gfAudioSource;
    }

}

public enum AudioMixerType
{
    MASTER,
    MUSIC_MASTER, MUSIC_MAIN, MUSIC_ACTION, MUSIC_AUX,
    FX_MASTER, FX_DEFAULT, FX_UI, FX_AUX

}