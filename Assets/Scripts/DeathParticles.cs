using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathParticles : MonoBehaviour
{
    protected ParticleSystem m_particleSystem;

    private static DeathParticles m_instance;

    private static Transform m_instanceTrans;

    // Start is called before the first frame update
    void Start()
    {
        if (m_instance) Destroy(m_instance);
        m_instance = this;
        m_instanceTrans = transform;

        m_particleSystem = GetComponent<ParticleSystem>();
    }

    public static void PlaySystem(Vector3 position)
    {
        m_instanceTrans.position = position;
        m_instance.m_particleSystem.Play(true);
    }
}
