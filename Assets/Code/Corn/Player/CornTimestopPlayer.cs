using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CornTimestopPlayer : MonoBehaviour
{
    [SerializeField] private GfxSliderFollower m_sliderFollower;

    [SerializeField] private GfgPlayerController m_playerController;

    [SerializeField] private GfMovementGeneric m_movement;

    [SerializeField] private GfgInputType m_timeStopInput;

    [SerializeField] private float m_depleteTime = 5;

    private GfgInputTracker m_inputTrackerTimeStop;

    private float m_currentEnergyLevel = 1;

    [HideInInspector] public bool m_canTimeStop = true;

    private bool m_inTimeStop = false;

    // Start is called before the first frame update
    void Start()
    {
        m_inputTrackerTimeStop = new(m_timeStopInput);
        this.GetComponent(ref m_playerController);
        this.GetComponent(ref m_movement);
        m_sliderFollower.Target = transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (m_inputTrackerTimeStop.PressedSinceLastCheck(false) && m_currentEnergyLevel > 0)
        {
            //initiate time stop
            m_inTimeStop = true;
        }
        else if (m_inputTrackerTimeStop.ReleasedSinceLastCheck(false) || m_currentEnergyLevel <= 0)
        {

            //stop time stop;
            m_inTimeStop = false;
        }

        if (m_inTimeStop)
        {
            m_currentEnergyLevel -= Time.deltaTime / m_depleteTime;
            m_sliderFollower.Slider.value = m_currentEnergyLevel;
        }
        else
        {
            if (m_movement.GetGrounded())
                m_currentEnergyLevel = 1;
        }

        m_sliderFollower.Visible = m_inTimeStop;
        m_playerController.IgnoreRunner = m_inTimeStop;
    }
}
