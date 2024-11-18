using UnityEngine;

public class LegsTest : MonoBehaviour
{

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q)) Debug.Log("Normal Log");
        if (Input.GetKeyDown(KeyCode.W)) Debug.LogError("Error Log");
        if (Input.GetKeyDown(KeyCode.E)) Debug.LogWarning("Warning Log");
    }
}