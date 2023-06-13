using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System;
public class GfUITools : MonoBehaviour
{
    private static GfUITools Instance;
    private static List<RaycastResult> m_raycastResults = new(1);
    private static PointerEventData m_pointerEventData;


    // Start is called before the first frame update
    void Awake()
    {
        if (Instance) Destroy(Instance);
        Instance = this;

        m_pointerEventData = new PointerEventData(EventSystem.current);
    }

    public static bool IsMouseOverUICollision(GameObject ui)
    {
        return EventSystem.current.IsPointerOverGameObject() && ui == GetUIObjectUnderMouse(Input.mousePosition);
    }

    public static bool IsMouseOverUICollision(Vector3 mousePosition, GameObject ui)
    {
        return EventSystem.current.IsPointerOverGameObject() && ui == GetUIObjectUnderMouse(mousePosition);
    }

    public static GameObject GetUIObjectUnderMouse()
    {
        return GetUIObjectUnderMouse(Input.mousePosition);
    }

    public static GameObject GetUIObjectUnderMouse(Vector3 mousePosition)
    {
        m_pointerEventData.position = mousePosition;
        EventSystem.current.RaycastAll(m_pointerEventData, m_raycastResults);

        int count = m_raycastResults.Count;
        int lowestIndex = -1;
        float lowestDepth = int.MaxValue;

        for (int i = 0; i < count; ++i)
        {
            float depth = m_raycastResults[i].distance;
            if (depth < lowestDepth)
            {
                lowestDepth = depth;
                lowestIndex = i;
            }
        }

        GameObject obj = null;
        if (lowestIndex != -1)
            obj = m_raycastResults[lowestIndex].gameObject;

        return obj;
    }

    public static bool IsMouseOverUI()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }
}
