using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GfcAudioSource : MonoBehaviour
{
    [SerializeField] private Transform m_parent;

    private AudioSource m_audioSource;

    private Transform m_transform;

    public bool DestroyWhenFinished = false;

    public GfcSound OriginalSound = null;

    void Awake()
    {
        m_transform = transform;
        m_audioSource = GetComponent<AudioSource>();
    }

    void LateUpdate()
    {
        if (m_parent) m_transform.position = m_parent.position;
        if (DestroyWhenFinished && !m_audioSource.isPlaying) GfcPooling.DestroyInsert(gameObject);
    }

    public AudioSource GetAudioSource() { return m_audioSource ? m_audioSource : GetComponent<AudioSource>(); }

    public Transform GetParent() { return m_parent; }

    public void SetParent(Transform aParent)
    {
        m_parent = aParent;
        if (aParent)
            transform.position = m_parent.position;
    }

    public void Stop() { GetAudioSource()?.Stop(); }

    public void OnDisable()
    {
        if (null != OriginalSound)
            OriginalSound.ClipFinished();
    }

    public void OnDestroy()
    {
        if (null != OriginalSound)
            OriginalSound.ClipFinished();
    }
}