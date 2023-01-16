using UnityEngine.Audio;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Sound
{
    public AudioClip clip;
    public bool isMusic;

    [Range(0f, 1f)]
    public float volume = .5f;
    [Range(0f, 1f)]
    public float volumeVariance = 0f;

    [Range(.1f, 3f)]
    public float pitch = 1f;
    [Range(0f, 1f)]
    public float pitchVariance = 0;

    public bool loop = false;

    public AudioMixerGroup mixerGroup;

    private float timeOfCoolDownOver = 0;

    public float Length()
    {
        if (null != clip)
            return clip.length;
        else
            return 0;
    }

    //very arbitrary number, but it makes the touhou sounds closer to the original games
    private const float coolDownTime = 0.08f;

    public void Play(AudioSource source, float volume = 1, float pitch = 1)
    {
        if (source.clip != clip)
        {
            source.clip = clip;
        }

        float currentTime = Time.unscaledTime;

        if (currentTime >= timeOfCoolDownOver)
        {
            timeOfCoolDownOver = currentTime += coolDownTime;

            source.volume = this.volume * volume;
            source.pitch = this.pitch * (1f + UnityEngine.Random.Range(-pitchVariance / 2f, pitchVariance / 2f));
            source.pitch *= pitch;

            source.Play();
        }
    }

}