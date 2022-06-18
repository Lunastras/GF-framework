using UnityEngine.Audio;
using System;
using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{

    public static AudioManager instance;

    public AudioMixerGroup mixerGroup;

    public float effectsVolume;
    public float musicVolume;

    //private SaveData saveData;

    public Sound[] sounds;

    void Awake()
    {

    }

    private void Start()
    {
        musicVolume = 0.2f;
        effectsVolume = 0.1f;
        //SetMusicVolume();
        //SetEffectsVolume();
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

    public void StopAudio()
    {
    }

    public void DefSettings()
    {
        SetVolume(1);
        SetPitch(1);
    }

    /*public void SetMusicVolume()
	{
		if(currentlyPlayingMusic != null)
		{
			currentlyPlayingMusic.source.volume = currentlyPlayingMusic.volume * (currentlyPlayingMusic.isMusic ? musicVolume : effectsVolume);
		} else
		{
			Debug.Log("nothing is playing dfq");
		}
		//saveData.musicVolume = musicVolume;
	}
	public void SetEffectsVolume()
	{
		//saveData.effectsVolume = effectsVolume;
		Play("Dunno");
	}*/

}