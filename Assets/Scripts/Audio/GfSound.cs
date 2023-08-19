using UnityEngine.Audio;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class GfSound
{
    public AudioClip Clip;
    public AudioMixerType MixerType;

    [Range(0f, 1f)]
    public float Volume = .5f;
    [Range(0f, 1f)]
    public float VolumeVariance = 0f;

    [Range(.1f, 3f)]
    public float Pitch = 1f;
    [Range(0f, 1f)]
    public float PitchVariance = 0;
    public float CoolDownTime = 0.08f;
    public bool Loop = false;
    private float TimeOfCoolDownOver = 0;
    public float MinDistance = 1.0f;
    public float MaxDistance = 500f;

    [Range(0f, 1f)]
    public float SpatialBlend = 0f;

    [SerializeField]
    public uint MaxInstances = 12;

    private int CurrentPlayingInstances = 0;

    public float Length()
    {
        if (Clip)
            return Clip.length;
        else
            return 0;
    }

    public void ClipFinished()
    {
        CurrentPlayingInstances--;
    }

    //Plays the sound at volume 0 in order to load the sound in advance
    //By default, unity does not load sounds until they are played, which produces stutters that can take up to 2 seconds
    //we use this to produce the stutter while loading the level in order to avoid it during the level
    public void LoadAudioClip()
    {
        GfAudioManager.LoadAudioClip(this);
    }

    public GfAudioSource Play(float delay = 0, float volume = 1, float pitch = 1)
    {
        return Play(Vector3.zero, delay, volume, pitch);
    }

    public GfAudioSource Play(Vector3 position, float delay = 0, float volume = 1, float pitch = 1)
    {
        float currentTime;

        if (Clip && CurrentPlayingInstances < MaxInstances && (currentTime = Time.time + delay) >= TimeOfCoolDownOver)
        {
            TimeOfCoolDownOver = currentTime + CoolDownTime + delay;
            CurrentPlayingInstances++;
            return GfAudioManager.PlayAudio(this, position, delay, volume, pitch);
        }
        else
            return null;
    }

    public GfAudioSource Play(Transform parent, float delay = 0, float volume = 1, float pitch = 1)
    {
        float currentTime;

        if (Clip && CurrentPlayingInstances < MaxInstances && (currentTime = Time.time + delay) >= TimeOfCoolDownOver)
        {
            TimeOfCoolDownOver = currentTime + CoolDownTime + delay;
            CurrentPlayingInstances++;
            return GfAudioManager.PlayAudio(this, parent, delay, volume, pitch);
        }
        else
            return null;
    }

    public void Play(AudioSource source, float delay = 0, float volume = 1, float pitch = 1)
    {
        float currentTime;

        if (source.isActiveAndEnabled && Clip && (currentTime = Time.time + delay) >= TimeOfCoolDownOver)
        {
            TimeOfCoolDownOver = currentTime + CoolDownTime + delay;

            source.volume = volume * GetVolume();
            source.pitch = pitch * GetPitch();
            source.outputAudioMixerGroup = GetAudioMixerGroup();
            source.clip = Clip;
            source.loop = Loop;
            source.spatialBlend = SpatialBlend;
            source.minDistance = MinDistance;
            source.maxDistance = MaxDistance;

            if (delay > 0)
                source.PlayDelayed(delay);
            else
                source.Play();
        }
    }

    public float GetPitch()
    {
        return Pitch * (1f + UnityEngine.Random.Range(-PitchVariance * 0.5f, PitchVariance * 0.5f));
    }

    public float GetVolume()
    {
        return Volume * (1f + UnityEngine.Random.Range(-VolumeVariance * 0.5f, VolumeVariance * 0.5f));
    }

    public bool CanPlay(float timeToPlay) { return timeToPlay >= TimeOfCoolDownOver; }

    public bool CanPlay() { return CanPlay(Time.time); }

    public AudioMixerGroup GetAudioMixerGroup()
    {
        return GfAudioManager.GetMixerGroup(MixerType);
    }

    public void SetMixerVolume(float volume)
    {
        GfAudioManager.SetMixerVolume(MixerType, volume);
    }

    public void SetMixerVolumeRaw(float volume)
    {
        GfAudioManager.SetMixerVolumeRaw(MixerType, volume);
    }

    public float GetMixerVolumeRaw()
    {
        return GfAudioManager.GetMixerVolumeRaw(MixerType);
    }

    public void SetMixerPitch(float pitch)
    {
        GfAudioManager.SetMixerPitch(MixerType, pitch);
    }

    public float GetMixerPitch()
    {
        return GfAudioManager.GetMixerPitch(MixerType);
    }
}