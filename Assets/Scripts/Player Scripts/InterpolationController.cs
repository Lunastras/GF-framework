using UnityEngine;
using System.Collections;

public class InterpolationController : MonoBehaviour
{
    private static float[] m_lastFixedUpdateTimes;
    private static int m_newTimeIndex;

    private static int _lastFrameCalculated;

    private static float m_interpolationFactor;
    public static float InterpolationFactor
    {
        get 
        {
            int currentFrame = Time.frameCount;
            float interpolationFactor = m_interpolationFactor;

            if(currentFrame != _lastFrameCalculated)
            {
                _lastFrameCalculated = currentFrame;

                float newerTime = m_lastFixedUpdateTimes[m_newTimeIndex];
                float olderTime = m_lastFixedUpdateTimes[OldTimeIndex()];

                if (newerTime != olderTime)
                {
                    m_interpolationFactor = (Time.time - newerTime) / (newerTime - olderTime);
                }
                else
                {
                    m_interpolationFactor = 1;
                }
            }

            return interpolationFactor;
        }
    }


    public void Start()
    {
        m_lastFixedUpdateTimes = new float[2];
        m_newTimeIndex = 0;
    }

    public void FixedUpdate()
    {
        m_newTimeIndex = OldTimeIndex();
        m_lastFixedUpdateTimes[m_newTimeIndex] = Time.fixedTime;
    }

    private static int OldTimeIndex()
    {
        return (m_newTimeIndex == 0 ? 1 : 0);
    }
}