using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FpsTracker : MonoBehaviour
{
    [SerializeField]
    private Text textFps = null;

    [SerializeField]
    private int refreshesPerSecond = 5;

    private uint updateCalls;
    private uint totalFrames;
    private float intervalBetweenRefresh;
    private float lastRefreshTime;
    // Start is called before the first frame update
    void Start()
    {
        if (null == textFps)
        {
            textFps = GetComponent<Text>();
        }

        intervalBetweenRefresh = 1.0f / (float)refreshesPerSecond;
        lastRefreshTime = Time.unscaledTime;
    }

    //Displays the average FPS between the refreshes

    // Update is called once per frame
    void Update()
    {
        float currentTime = Time.unscaledTime;
        float timeSinceLastRefresh = currentTime - lastRefreshTime;
        uint currentFps = (uint)(1.0f / Time.deltaTime);
        totalFrames += currentFps;
        updateCalls++;

        if (timeSinceLastRefresh > intervalBetweenRefresh)
        {
            textFps.text = (totalFrames / updateCalls).ToString();
            lastRefreshTime = currentTime;
            updateCalls = totalFrames = 0;
        }
    }
}
