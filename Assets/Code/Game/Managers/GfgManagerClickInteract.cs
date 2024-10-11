using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfgManagerClickInteract : MonoBehaviour
{
    private static GfgManagerClickInteract Instance;

    private GfcInputTracker m_clickTracker = new(GfcInputType.SUBMIT);

    // Start is called before the first frame update
    void Awake()
    {
        this.SetSingleton(ref Instance);
    }

    private void Update()
    {
        if (m_clickTracker.PressedSinceLastCheck())
        {

        }
    }
}
