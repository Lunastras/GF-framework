using UnityEngine;
using UnityEngine.Audio;
using static Unity.Mathematics.math;

[RequireComponent(typeof(AudioSource))]
public class GfcManagerAudio : MonoBehaviour
{
    private static GfcManagerAudio Instance = null;
    [SerializeField] private EnumSingletons<GfcSound, GfcSoundPreset> m_soundPresets;
    [SerializeField] private AudioMixerTypeInstance[] m_mixerGroups = null;
    [SerializeField] private GameObject m_audioObjectPrefab = null;
    [SerializeField] private float m_maxVolumeDecibels = 0;
    [SerializeField] private float m_minVolumeDecibels = -80;
    private AudioSource m_audioLoadAudioSource = null;

    //twelveth root of 2
    const float NOTE_PROGRESSION_COEF = 1.05946309436f;
    const string VOLUME_STRING = "Volume";
    const string PITCH_STRING = "Pitch";
    private static readonly Vector3 ZERO3 = Vector3.zero;

    void Awake()
    {
        if (this != Instance)
            Destroy(Instance);
        Instance = this;

        m_soundPresets.Initialize(GfcSoundPreset.COUNT);
        m_audioLoadAudioSource = GetComponent<AudioSource>();
        for (int i = 0; i < m_mixerGroups.Length; ++i)
            if ((int)m_mixerGroups[i].AudioMixerType != i)
                Debug.LogError("The audio mixer group of type " + m_mixerGroups[i].AudioMixerType + " is at index " + i + " instead of index " + (int)m_mixerGroups[i].AudioMixerType);
    }

    private static GfcStringBuffer GetExposedParameterString(AudioMixerType aMixerType, string aParameterString)
    {
        GfcStringBuffer buffer = GfcPooling.GfcStringBuffer;
        buffer.Append(aParameterString);
        buffer.Append(" (of ");
        buffer.Append(Instance.m_mixerGroups[(int)aMixerType].AudioMixerGroup.name);
        buffer.Append(')');
        return buffer;
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

    public static GfcSound GetSoundPresets(GfcSoundPreset aSoundPreset) { return aSoundPreset == GfcSoundPreset.NONE ? null : Instance.m_soundPresets[aSoundPreset]; }

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

    public static bool SetMixerVolumeRaw(AudioMixerType aMixerType, float volume)
    {
        int index = (int)aMixerType;
        GfcStringBuffer parameterName = GetExposedParameterString(aMixerType, VOLUME_STRING);
        var ret = Instance.m_mixerGroups[index].AudioMixerGroup.audioMixer.SetFloat(parameterName, volume);
        parameterName.Clear();
        return ret;
    }

    public static bool SetMixerVolume(AudioMixerType aMixerType, float volume) { return SetMixerVolumeRaw(aMixerType, ValueToVolume(volume)); }

    public static float GetMixerVolumeRaw(AudioMixerType aMixerType)
    {
        int index = (int)aMixerType;
        GfcStringBuffer parameterName = GetExposedParameterString(aMixerType, VOLUME_STRING);
        Instance.m_mixerGroups[index].AudioMixerGroup.audioMixer.GetFloat(parameterName, out float val);
        parameterName.Clear();
        return val;
    }

    public static bool SetMixerPitch(AudioMixerType aMixerType, float volume)
    {
        int index = (int)aMixerType;
        GfcStringBuffer parameterName = GetExposedParameterString(aMixerType, PITCH_STRING);
        var ret = Instance.m_mixerGroups[index].AudioMixerGroup.audioMixer.SetFloat(parameterName, volume);
        parameterName.Clear();
        return ret;
    }

    public static float GetMixerPitch(AudioMixerType aMixerType)
    {
        int index = (int)aMixerType;
        GfcStringBuffer parameterName = GetExposedParameterString(aMixerType, PITCH_STRING);
        Instance.m_mixerGroups[index].AudioMixerGroup.audioMixer.GetFloat(parameterName, out float val);
        parameterName.Clear();
        return val;
    }

    public static GfcAudioSource GetAudioObject(Transform aParent = null)
    {
        GfcAudioSource src = GfcPooling.PoolInstantiate(Instance.m_audioObjectPrefab).GetComponent<GfcAudioSource>();
        src.SetParent(aParent);
        return src;
    }

    public static AudioMixerGroup GetMixerGroup(uint aIndex)
    {
        AudioMixerGroup ret = null;
        int length = Instance.m_mixerGroups.Length;
        if (length > 0)
            if (length > aIndex)
                ret = Instance.m_mixerGroups[aIndex].AudioMixerGroup;
            else
                ret = Instance.m_mixerGroups[0].AudioMixerGroup; //if the type has no mixer group, give default mixer group

        return ret;
    }

    public static AudioMixerGroup GetMixerGroup(AudioMixerType aMixerType)
    {
        return GetMixerGroup((uint)aMixerType);
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
        gfAudioSource.DestroyWhenFinished = true;

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

    MUSIC_MASTER
    , MUSIC_MAIN
    , MUSIC_SECONDARY
    , MUSIC_AMBIENT

    , FX_MASTER
    , FX_DEFAULT
    , FX_VOICES
    , FX_UI,

    MUSIC_AUX,
    FX_AUX,
}

public enum PianoNotes
{
    C, C_SHARP, D, D_SHARP, E, F, F_SHARP, G, G_SHARP, A, A_SHARP, B
}

[System.Serializable]
public struct AudioMixerTypeInstance
{
    public AudioMixerType AudioMixerType;
    public AudioMixerGroup AudioMixerGroup;
}

public enum GfcSoundPreset
{
    NONE = -1,
    SUBMIT,
    SUBMIT_ALT,
    SUBMIT_SUPER,
    BACK,
    BACK_ALT,
    SELECT,
    SELECT_ALT,
    PIN,
    UNPIN,
    SWOOSH_ENTER,
    SWOOSH_EXIT,
    SKIP,
    COUNT,
}