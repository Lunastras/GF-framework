using UnityEngine.Audio;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Sound
{
    public AudioClip m_clip;
    public AudioMixerType m_mixerType;

    [Range(0f, 1f)]
    public float m_volume = .5f;
    [Range(0f, 1f)]
    public float m_volumeVariance = 0f;

    [Range(.1f, 3f)]
    public float m_pitch = 1f;
    [Range(0f, 1f)]
    public float m_pitchVariance = 0;
    public float m_coolDownTime = 0.08f;
    public bool m_loop = false;
    private double m_timeOfCoolDownOver = 0;
    public float m_minDistance = 1.0f;
    public float m_maxDistance = 500f;

    [Range(0f, 1f)]
    public float m_spatialBlend = 0f;

    [SerializeField]
    public uint m_maxInstances = 12;

    public int m_currentPlayingInstances = 0;

    private AudioSource m_lastAudioSource;

    public float Length()
    {
        if (null != m_clip)
            return m_clip.length;
        else
            return 0;
    }

    public void ClipFinished()
    {
        m_currentPlayingInstances--;
    }

    public void Played(double timeAsDouble, float delay)
    {
        m_timeOfCoolDownOver = timeAsDouble + m_coolDownTime + delay;
    }

    public void Played(float delay)
    {
        Played(Time.timeAsDouble, delay);
    }

    //Plays the sound at volume 0 in order to load the sound in advance
    //By default, unity does not load sounds until they are played, which produces stutters that can take up to 2 seconds
    //we use this to produce the stutter while loading the level in order to avoid it during the level
    public void LoadAudioClip()
    {
        AudioManager.LoadAudioClip(this);
    }

    public GfAudioSource Play(float delay = 0, float volume = 1, float pitch = 1)
    {
        return Play(Vector3.zero, delay, volume, pitch);
    }

    public GfAudioSource Play(Vector3 position, float delay = 0, float volume = 1, float pitch = 1)
    {
        if (m_currentPlayingInstances < m_maxInstances)
        {
            m_currentPlayingInstances++;
            return AudioManager.PlayAudio(this, position, delay, volume, pitch);
        }
        else
            return null;
    }

    public GfAudioSource Play(Transform parent, float delay = 0, float volume = 1, float pitch = 1)
    {
        if (m_currentPlayingInstances < m_maxInstances)
        {
            m_currentPlayingInstances++;
            return AudioManager.PlayAudio(this, parent, delay, volume, pitch);
        }
        else
            return null;
    }

    public void Play(AudioSource source, float delay = 0, float volume = 1, float pitch = 1)
    {
        double currentTime;

        if (source && m_clip != null && (currentTime = Time.timeAsDouble) >= m_timeOfCoolDownOver)
        {
            m_timeOfCoolDownOver = currentTime + m_coolDownTime + delay;

            m_lastAudioSource = source;
            source.volume = m_volume * GetVolume();
            source.pitch = pitch * GetPitch();
            source.outputAudioMixerGroup = GetAudioMixerGroup();
            source.clip = m_clip;
            source.loop = m_loop;
            source.spatialBlend = m_spatialBlend;
            source.minDistance = m_minDistance;
            source.maxDistance = m_maxDistance;

            if (delay > 0)
                source.PlayDelayed(delay);
            else
                source.Play();
        }
    }


    public float GetPitch()
    {
        return m_pitch * (1f + UnityEngine.Random.Range(-m_pitchVariance / 2f, m_pitchVariance / 2f));
    }

    public float GetVolume()
    {
        return m_volume * (1f + UnityEngine.Random.Range(-m_volumeVariance / 2f, m_volumeVariance / 2f));
    }

    public bool CanPlay(double timeAsDouble) { return timeAsDouble >= m_timeOfCoolDownOver && m_clip; }

    public bool CanPlay() { return CanPlay(Time.timeAsDouble); }

    public AudioMixerGroup GetAudioMixerGroup()
    {
        return AudioManager.GetMixerGroup(m_mixerType);
    }

    public void SetMixerVolume(float volume)
    {
        AudioManager.SetMixerVolume(m_mixerType, volume);
    }

    public void SetMixerVolumeRaw(float volume)
    {
        AudioManager.SetMixerVolumeRaw(m_mixerType, volume);
    }

    public float GetMixerVolumeRaw()
    {
        return AudioManager.GetMixerVolumeRaw(m_mixerType);
    }

    public void SetMixerPitch(float pitch)
    {
        AudioManager.SetMixerPitch(m_mixerType, pitch);
    }

    public float GetMixerPitch()
    {
        return AudioManager.GetMixerPitch(m_mixerType);
    }
}