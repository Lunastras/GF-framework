using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GfcAudioSource : MonoBehaviour
{
    [SerializeField]
    private Transform m_parent;
    private AudioSource m_audioSource;

    private Transform m_transform;

    public bool m_destroyWhenFinished = false;

    public GfcSound m_originalSound = null;

    // Start is called before the first frame update

    void Awake()
    {
        m_transform = transform;
        m_audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (m_parent) m_transform.position = m_parent.position;
        if (m_destroyWhenFinished && !m_audioSource.isPlaying) GfcPooling.DestroyInsert(gameObject);
    }

    public AudioSource GetAudioSource()
    {
        return m_audioSource ? m_audioSource : GetComponent<AudioSource>();
    }

    public Transform GetParent()
    {
        return m_parent;
    }

    public void SetParent(Transform aParent)
    {
        m_parent = aParent;
        if (aParent)
            transform.position = m_parent.position;

    }

    public void OnDisable()
    {
        if (null != m_originalSound)
            m_originalSound.ClipFinished();
    }

    public void OnDestroy()
    {
        if (null != m_originalSound)
            m_originalSound.ClipFinished();
    }
}
