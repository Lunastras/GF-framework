using MEC;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Assertions.Must;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GfcCursor : BaseInput
{
    private static GfcCursor Instance;

    private Vector2 m_cursorPosition = Vector2.zero;

    [SerializeField] private RectTransform m_cursorTransform;

    //[SerializeField] private float m_sensitivity = 100;

    private static PointerEventData PointerEventData;

    private GameObject m_gameObjectToSelect;
    private bool m_inTheProcessOfSelectingGameObject = false;

    private bool m_inTheProcessOfDeselectingGameObject = false;

    private CoroutineHandle m_selectingGameObjectCoroutine = default;
    private CoroutineHandle m_deselectingGameObjectCoroutine = default;

    private readonly HashSet<GameObject> m_objectsToDeselect = new(4);

    public override Vector2 mousePosition
    {
        get { return this.m_cursorPosition; }
    }

    private static readonly List<RaycastResult> RaycastResults = new(8);

    //placeholder for when we will have custom cursor that can be controlled by mouse and controller
    public static Vector2 MousePosition { get { return Input.mousePosition; } }

    public static Vector3 MousePositionWithDepth(float aDepth = 1)
    {
        Vector3 pos = MousePosition;
        pos.z = aDepth;
        return pos;
    }

    new void Awake()
    {
        this.SetSingleton(ref Instance);
        PointerEventData = new PointerEventData(EventSystem.current);

        base.Awake();
        this.GetComponentIfNull(ref m_cursorTransform);
        Debug.Assert(m_cursorTransform);
    }

    /*
    void Update()
    {
        Vector2 movement = new(GfgInput.GetAxisRaw(GfgInputType.CAMERA_X), GfgInput.GetAxisRaw(GfgInputType.CAMERA_Y));
        m_cursorPosition += m_sensitivity * movement;
        m_cursorTransform.position = m_cursorPosition;
    }*/

    public static bool IsMouseOverUICollision(GameObject ui)
    {
        return EventSystem.current.IsPointerOverGameObject() && ui == GetUIObjectUnderPosition(GfcCursor.MousePosition).gameObject;
    }

    public static bool IsMouseOverUICollision(Vector3 mousePosition, GameObject ui)
    {
        return EventSystem.current.IsPointerOverGameObject() && ui == GetUIObjectUnderPosition(mousePosition).gameObject;
    }

    public static GfcCursorRayhit GetGameObjectUnderMouse(bool anIgnoreUi = false, int aLayerMask = int.MaxValue, QueryTriggerInteraction aQuerryTriggerInteraction = QueryTriggerInteraction.Ignore)
    {
        return GetGameObjectUnderPosition(MousePosition, anIgnoreUi, aLayerMask, aQuerryTriggerInteraction);
    }

    public static GfcCursorRayhit GetGameObjectUnderPosition(Vector3 aPosition, bool anIgnoreUi = false, int aLayerMask = int.MaxValue, QueryTriggerInteraction aQuerryTriggerInteraction = QueryTriggerInteraction.Ignore)
    {
        GfcCursorRayhit hit = default;

        GameObject hitObject = null;
        RaycastResult raycastResult = default;

        if (!anIgnoreUi)
        {
            raycastResult = GetUIObjectUnderPosition(aPosition, aLayerMask);
            hitObject = raycastResult.gameObject;
        }

        if (hitObject == null)
        {
            Camera cam = GfcCamera.MainCamera;
            Vector3 cameraPos = cam.transform.position;
            Vector3 dirToTarget = cam.ScreenToWorldPoint(MousePositionWithDepth()) - cameraPos;

            GfcTools.Normalize(ref dirToTarget);
            if (Physics.Raycast(cameraPos, dirToTarget, out RaycastHit hitInfo, float.MaxValue, aLayerMask, aQuerryTriggerInteraction))
            {
                hit.RaycastHit = hitInfo;
                hit.GameObject = hitInfo.collider.gameObject;
                hit.HitType = GfcCursorRayhitType.GAMEOBJECT;
            }
        }
        else
        {
            hit.RaycastResultUi = raycastResult;
            hit.GameObject = raycastResult.gameObject;
            hit.HitType = GfcCursorRayhitType.UI;
        }

        return hit;
    }

    public static RaycastResult GetUIObjectUnderMouse(int aLayerMask = int.MaxValue)
    {
        return GetUIObjectUnderPosition(GfcCursor.MousePosition, aLayerMask);
    }

    public static RaycastResult GetUIObjectUnderPosition(Vector3 aPosition, int aLayerMask = int.MaxValue)
    {
        PointerEventData.position = aPosition;
        EventSystem.current.RaycastAll(PointerEventData, RaycastResults);

        int count = RaycastResults.Count;
        int lowestIndex = -1;
        float lowestDepth = int.MaxValue;

        for (int i = 0; i < count; ++i)
        {
            float depth = RaycastResults[i].distance;
            if (depth < lowestDepth && GfcPhysics.LayerIsInMask(RaycastResults[i].gameObject.layer, aLayerMask))
            {
                lowestDepth = depth;
                lowestIndex = i;
            }
        }

        RaycastResult raycastResult = default;
        if (lowestIndex != -1)
            raycastResult = RaycastResults[lowestIndex];

        return raycastResult;
    }

    public static bool IsMouseOverUI()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }

    private IEnumerator<float> _SetSelectedGameObject()
    {
        m_inTheProcessOfSelectingGameObject = true;

        while (EventSystem.current.alreadySelecting)
            yield return Timing.WaitForOneFrame;

        EventSystem.current.SetSelectedGameObject(m_gameObjectToSelect);
        m_gameObjectToSelect = null;
        m_inTheProcessOfSelectingGameObject = false;
        m_selectingGameObjectCoroutine = default;
    }

    private IEnumerator<float> _RemoveSelectedGameObject()
    {
        m_inTheProcessOfSelectingGameObject = true;

        while (EventSystem.current.alreadySelecting)
            yield return Timing.WaitForOneFrame;

        if (m_objectsToDeselect.Contains(EventSystem.current.currentSelectedGameObject))
            EventSystem.current.SetSelectedGameObject(null);

        m_objectsToDeselect.Clear();
        m_inTheProcessOfDeselectingGameObject = false;
        m_deselectingGameObjectCoroutine = default;
    }

    public static CoroutineHandle SetSelectedGameObject(GameObject aGameObject)
    {
        if (Instance.m_objectsToDeselect.Contains(aGameObject))
            Instance.m_objectsToDeselect.Remove(aGameObject);

        Instance.m_gameObjectToSelect = aGameObject;
        if (!Instance.m_inTheProcessOfSelectingGameObject)
            Instance.m_selectingGameObjectCoroutine = Timing.RunCoroutine(Instance._SetSelectedGameObject());

        return Instance.m_selectingGameObjectCoroutine;
    }

    public static CoroutineHandle RemoveSelectedGameObject() { return SetSelectedGameObject(null); }

    public static CoroutineHandle RemoveSelectedGameObject(GameObject aGameObject)
    {
        if (Instance.m_gameObjectToSelect == aGameObject)
        {
            Instance.m_gameObjectToSelect = null;
        }
        else
        {
            Instance.m_objectsToDeselect.Add(aGameObject);
            if (!Instance.m_inTheProcessOfDeselectingGameObject)
                Instance.m_deselectingGameObjectCoroutine = Timing.RunCoroutine(Instance._RemoveSelectedGameObject());
        }


        return Instance.m_deselectingGameObjectCoroutine;
    }
}

public enum GfcCursorRayhitType
{
    NONE,
    UI,
    GAMEOBJECT,
}

public struct GfcCursorRayhit
{
    public RaycastResult RaycastResultUi;
    public RaycastHit RaycastHit;
    public GfcCursorRayhitType HitType;
    public GameObject GameObject;
}
