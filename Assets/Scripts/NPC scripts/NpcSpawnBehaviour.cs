using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class NpcSpawnBehaviour : MonoBehaviour
{
    [SerializeField]
    protected AudioSource m_audioSource = null;

    void Start()
    {
        m_audioSource.volume = 0.5f;
        m_audioSource = GetComponent<AudioSource>();
        m_audioSource.Play();
    }

    public void SetPitch(float pitch)
    {
        m_audioSource.pitch = pitch;
    }

    public float GetPitch()
    {
        return m_audioSource.pitch;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!m_audioSource.isPlaying) GfcPooling.DestroyInsert(gameObject);
    }
}
