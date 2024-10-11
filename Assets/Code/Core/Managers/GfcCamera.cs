using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class GfcCamera : MonoBehaviour
{
    public static Camera MainCamera { get; private set; }

    public Camera Camera { get { return m_camera; } protected set { m_camera = value; } }

    protected Camera m_camera;

    private static List<Camera> Cameras = new(8);

    protected void OnEnable()
    {
        Debug.Assert(this.GetComponent(ref m_camera));
        MainCamera = m_camera;
        Cameras.Add(m_camera);

        if (MainCamera)
            Debug.Log("Found camera for " + GetSignature());
        else
            Debug.LogError("PULA CAMERA FOR " + GetSignature());
    }

    private string GetSignature() { return " " + gameObject.name + " " + Time.frameCount + " " + GetType(); }

    protected void OnDisable()
    {
        int thisCameraIndex = -1;
        Camera anActiveCamera = null; //keep this camera if there are none

        for (int i = 0; i < Cameras.Count; i++)
        {
            if (Cameras[i] != null)
            {
                if (Cameras[i] == m_camera)
                    thisCameraIndex = i;
                else if (Cameras[i].gameObject.activeInHierarchy)
                    anActiveCamera = Cameras[i];
            }
            else
            {
                Cameras[i] = Cameras[^1];
                Cameras.RemoveAt(Cameras.Count - 1);
                i--;//check this index again to make sure the last element wasn't null
            }
        }

        if (MainCamera == m_camera)
            MainCamera = anActiveCamera;

        if (thisCameraIndex >= 0)
            Cameras.RemoveAtSwapBack(thisCameraIndex);

        Debug.Log("Will disable now... " + GetSignature());
        Debug.Assert(MainCamera, Time.frameCount);
    }
}