using UnityEngine.Audio;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class GfcSound
{
    public AudioClip Clip;
    public AudioMixerType MixerType;
    public bool Loop = false;

    [Range(0f, 1f)] public float Volume = .5f;
    [Range(0f, 1f)] public float VolumeVariance = 0f;
    [Range(.1f, 3f)] public float Pitch = 1f;
    [Range(0f, 1f)] public float PitchVariance = 0;
    [Range(0f, 1f)] public float SpatialBlend = 0f;
    public float CoolDownTime = 0.08f;
    public float MinDistance = 1.0f;
    public float MaxDistance = 500f;
    public uint MaxInstances = 12;

    private int CurrentPlayingInstances = 0;
    private float TimeOfCoolDownOver = 0;

    public GfcAudioSource Play(Vector3 aPosition, float aDelay = 0, float aVolume = 1, float aPitch = 1)
    {
        float currentTime;

        if (Clip && CurrentPlayingInstances < MaxInstances && (currentTime = Time.time + aDelay) >= TimeOfCoolDownOver)
        {
            TimeOfCoolDownOver = currentTime + CoolDownTime + aDelay;
            CurrentPlayingInstances++;
            return GfcManagerAudio.PlayAudio(this, aPosition, aDelay, aVolume, aPitch);
        }
        else
            return null;
    }

    public GfcAudioSource Play(Transform aParent, float aDelay = 0, float aVolume = 1, float aPitch = 1)
    {
        float currentTime;

        if (Clip && CurrentPlayingInstances < MaxInstances && (currentTime = Time.time + aDelay) >= TimeOfCoolDownOver)
        {
            TimeOfCoolDownOver = currentTime + CoolDownTime + aDelay;
            CurrentPlayingInstances++;
            return GfcManagerAudio.PlayAudio(this, aParent, aDelay, aVolume, aPitch);
        }
        else
            return null;
    }

    public void Play(AudioSource aSource, float aDelay = 0, float aVolume = 1, float aPitch = 1)
    {
        float currentTime;

        if (aSource.isActiveAndEnabled && Clip && (currentTime = Time.time + aDelay) >= TimeOfCoolDownOver)
        {
            TimeOfCoolDownOver = currentTime + CoolDownTime + aDelay;

            aSource.volume = aVolume * GetVolume();
            aSource.pitch = aPitch * GetPitch();
            aSource.outputAudioMixerGroup = GetAudioMixerGroup();
            aSource.clip = Clip;
            aSource.loop = Loop;
            aSource.spatialBlend = SpatialBlend;
            aSource.minDistance = MinDistance;
            aSource.maxDistance = MaxDistance;

            if (aDelay > 0)
                aSource.PlayDelayed(aDelay);
            else
                aSource.Play();
        }
    }

    //Plays the sound at volume 0 in order to load the sound in advance
    //By default, unity does not load sounds until they are played, which produces stutters that can take up to 2 seconds
    //we use this to produce the stutter while loading the level in order to avoid it during gameplay
    public void LoadAudioClip() { GfcManagerAudio.LoadAudioClip(this); }
    public float Length() { return Clip ? Clip.length : 0; }
    public void ClipFinished() { CurrentPlayingInstances--; }
    public float GetPitch() { return Pitch * (1f + UnityEngine.Random.Range(-PitchVariance * 0.5f, PitchVariance * 0.5f)); }
    public float GetVolume() { return Volume * (1f + UnityEngine.Random.Range(-VolumeVariance * 0.5f, VolumeVariance * 0.5f)); }
    public bool CanPlay() { return CanPlay(Time.time); }
    public bool CanPlay(float aTimeToPlay) { return aTimeToPlay >= TimeOfCoolDownOver; }
    public AudioMixerGroup GetAudioMixerGroup() { return GfcManagerAudio.GetMixerGroup(MixerType); }
    public void SetMixerVolume(float aVolume) { GfcManagerAudio.SetMixerVolume(MixerType, aVolume); }
    public void SetMixerVolumeRaw(float aVolume) { GfcManagerAudio.SetMixerVolumeRaw(MixerType, aVolume); }
    public float GetMixerVolumeRaw() { return GfcManagerAudio.GetMixerVolumeRaw(MixerType); }
    public void SetMixerPitch(float aPitch) { GfcManagerAudio.SetMixerPitch(MixerType, aPitch); }
    public float GetMixerPitch() { return GfcManagerAudio.GetMixerPitch(MixerType); }
    //todo, make a system that reuses the same audio gameobject in the audio manager for a given GfcSound
    public GfcAudioSource PlaySingleInstance(float aVolume = 1, float aPitch = 1) { return Play(Vector3.zero, 0, aVolume, aPitch); }
    public GfcAudioSource Play(float aDelay = 0, float aVolume = 1, float aPitch = 1) { return Play(Vector3.zero, aDelay, aVolume, aPitch); }
    public static GfcSound GetSoundPreset(GfcSoundPreset aSoundPreset) { return GfcManagerAudio.GetSoundPresets(aSoundPreset); }
}