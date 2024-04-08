using UnityEngine.Audio;
using System;
using UnityEngine;
using UnityEngine.UI;
using static Unity.Mathematics.math;

[RequireComponent(typeof(AudioSource))]
public class GfcManagerAudio : MonoBehaviour
{
    private static GfcManagerAudio Instance = null;

    [SerializeField]
    private AudioMixerGroup[] m_mixerGroups = null;

    [SerializeField]
    private string[] m_mixerGroupsExposedVolumeString = null;

    [SerializeField]
    private string[] m_mixerGroupsExposedPitchString = null;

    [SerializeField]
    private GameObject m_audioObjectPrefab = null;

    [SerializeField]
    private float m_maxVolumeDecibels = 0;

    [SerializeField]
    private float m_minVolumeDecibels = -80;

    private static readonly Vector3 ZERO3 = Vector3.zero;

    [SerializeField]
    private AudioSource m_audioLoadAudioSource = null;

    //twelveth root of 2
    const float NOTE_PROGRESSION_COEF = 1.05946309436f;

    void Awake()
    {
        m_audioLoadAudioSource = GetComponent<AudioSource>();

        if (this != Instance)
            Destroy(Instance);

        Instance = this;
    }

    public static void LoadAudioClip(GfcSound sound)
    {
        if (Instance && Instance.m_audioLoadAudioSource)
        {
            Instance.m_audioLoadAudioSource.Stop();
            Instance.m_audioLoadAudioSource.volume = 0;
            Instance.m_audioLoadAudioSource.clip = sound.Clip;
            Instance.m_audioLoadAudioSource.Play();
        }
        else Debug.LogWarning("Clip cannot load before the audioManager initializes.");

    }

    public static float GetPitchFromNote(int octave, PianoNotes note)
    {
        octave -= 4; //middle C will have pitch 1
        float cPitchOfOctave = pow(2, octave); //get the c pitch of the given octave
        return cPitchOfOctave * pow(NOTE_PROGRESSION_COEF, (int)note); //go from the c note to the desired note
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

    public static void SetMixerPitch(AudioMixerType type, float volume)
    {
        int index = (int)type;
        bool worked = Instance.m_mixerGroups[index].audioMixer.SetFloat(Instance.m_mixerGroupsExposedPitchString[index], volume);
    }

    public static float GetMixerPitch(AudioMixerType type)
    {
        int index = (int)type;
        Instance.m_mixerGroups[index].audioMixer.GetFloat(Instance.m_mixerGroupsExposedPitchString[index], out float val);
        return val;
    }

    public static GfcAudioSource GetAudioObject(Transform parent = null)
    {
        GfcAudioSource src = GfcPooling.PoolInstantiate(Instance.m_audioObjectPrefab).GetComponent<GfcAudioSource>();
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

    public static GfcAudioSource PlayAudio(AudioClip audio, Transform parent, float delay = 0, float volume = 1, float pitch = 1, bool loop = false, AudioMixerType mixerType = AudioMixerType.FX_DEFAULT, float spatialBlend = 1, float minDst = 1, float maxDst = 500)
    {
        return InternalPlayAudio(audio, parent, ZERO3, delay, volume, pitch, loop, GetMixerGroup(mixerType), spatialBlend, minDst, maxDst);
    }

    public static GfcAudioSource PlayAudio(AudioClip audio, Transform parent, float delay = 0, float volume = 1, float pitch = 1, bool loop = false, AudioMixerGroup mixer = null, float spatialBlend = 1, float minDst = 1, float maxDst = 500)
    {
        return InternalPlayAudio(audio, parent, ZERO3, delay, volume, pitch, loop, mixer, spatialBlend, minDst, maxDst);
    }

    public static GfcAudioSource PlayAudio(AudioClip audio, Vector3 position, float delay = 0, float volume = 1, float pitch = 1, bool loop = false, AudioMixerType mixerType = AudioMixerType.FX_DEFAULT, float spatialBlend = 1, float minDst = 1, float maxDst = 500)
    {
        return InternalPlayAudio(audio, null, position, delay, volume, pitch, loop, GetMixerGroup(mixerType), spatialBlend, minDst, maxDst);
    }

    public static GfcAudioSource PlayAudio(AudioClip audio, Vector3 position, float delay = 0, float volume = 1, float pitch = 1, bool loop = false, AudioMixerGroup mixer = null, float spatialBlend = 1, float minDst = 1, float maxDst = 500)
    {
        return InternalPlayAudio(audio, null, position, delay, volume, pitch, loop, mixer, spatialBlend, minDst, maxDst);
    }

    public static GfcAudioSource PlayAudio(GfcSound audio, Vector3 position, float delay = 0, float volume = 1, float pitch = 1)
    {
        return InternalPlayAudio(audio.Clip, null, position, delay, volume * audio.GetVolume(), pitch * audio.GetPitch(), audio.Loop, audio.GetAudioMixerGroup(), audio.SpatialBlend, audio.MinDistance, audio.MaxDistance);
    }

    public static GfcAudioSource PlayAudio(GfcSound audio, Transform parent, float delay = 0, float volume = 1, float pitch = 1)
    {
        return InternalPlayAudio(audio.Clip, parent, ZERO3, delay, volume * audio.GetVolume(), pitch * audio.GetPitch(), audio.Loop, audio.GetAudioMixerGroup(), audio.SpatialBlend, audio.MinDistance, audio.MaxDistance);
    }

    private static GfcAudioSource InternalPlayAudio(AudioClip audio, Transform parent, Vector3 position, float delay = 0, float volume = 1, float pitch = 1, bool loop = false, AudioMixerGroup mixer = null, float spatialBlend = 1, float minDst = 1, float maxDst = 500)
    {
        GfcAudioSource gfAudioSource = GetAudioObject();
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