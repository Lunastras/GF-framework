using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GfxFpsTracker : MonoBehaviour
{
    [SerializeField]
    private Text textFps = null;

    [SerializeField]
    private float m_refreshRateInterval = 0.1f;

    private uint m_updateCalls;
    private uint m_totalFrames;
    private float m_timeUntilRefresh;
    // Start is called before the first frame update
    void Start()
    {
        this.GetComponentIfNull(ref textFps);
    }

    //Displays the average FPS between the refreshes

    // Update is called once per frame
    void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;
        uint currentFps = (uint)(1.0f / deltaTime);

        m_totalFrames += currentFps;
        m_updateCalls++;
        m_timeUntilRefresh -= deltaTime;

        if (m_timeUntilRefresh <= 0)
        {
            textFps.text = (m_totalFrames / m_updateCalls).ToString();
            m_timeUntilRefresh = m_refreshRateInterval;
            m_updateCalls = m_totalFrames = 0;
        }
    }
}
