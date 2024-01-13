using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System;
using MEC;

public class GfUiTools : MonoBehaviour
{
    [SerializeField]
    private Image m_colourOverlay = null;

    [SerializeField]
    private CanvasGroup m_colourOverlayCanvsGroup = null;

    [SerializeField]
    private CanvasGroup m_blackBarsCanvsGroup = null;

    [SerializeField]
    private RectTransform m_blackBarUpper = null;

    [SerializeField]
    private RectTransform m_blackBarLower = null;

    private static GfUiTools Instance;
    private static List<RaycastResult> RaycastResults = new(1);
    private static PointerEventData PointerEventData;


    // Start is called before the first frame update
    void Awake()
    {
        if (Instance) Destroy(Instance);
        Instance = this;

        PointerEventData = new PointerEventData(EventSystem.current);

        if (m_colourOverlayCanvsGroup)
            m_colourOverlayCanvsGroup.alpha = 1;

        if (m_colourOverlay)
        {
            m_colourOverlay.gameObject.SetActive(true);
            m_colourOverlay.CrossFadeAlpha(0, 0, true);
        }

        if (m_blackBarsCanvsGroup)
            SetBlackBars(false);
    }

    void OnDestroy()
    {
        Instance = null;
    }

    void Start()
    {
    }

    public static void SetBlackBars(bool turnOn, float delay = 0, bool constantOpacity = false, bool constantAnchors = false, bool ignoreTimeScale = false)
    {
        if (delay == 0 || true)
        {
            Instance.m_blackBarsCanvsGroup.alpha = turnOn ? 1 : 0;
            float lowerMaxYAnchor = turnOn ? 0.125f : 0;
            float upperMinYAnchor = turnOn ? 0.875f : 1;

            Instance.m_blackBarLower.anchorMax = new Vector2(1, lowerMaxYAnchor);
            Instance.m_blackBarUpper.anchorMin = new Vector2(0, upperMinYAnchor);
        }
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

    public static void CrossFadeAlpha(float alpha, float durationSeconds, bool ignoreTimeScale = false)
    {
        Instance.m_colourOverlay.CrossFadeAlpha(alpha, durationSeconds, ignoreTimeScale);
    }

    public static void CrossFadeColor(Color color, float durationSeconds, bool ignoreTimeScale = false, bool useAlpha = true, bool useRgb = true)
    {
        Instance.m_colourOverlay.CrossFadeColor(color, durationSeconds, ignoreTimeScale, useAlpha, useRgb);
    }

    public static void SetOverlayColor(Color color)
    {
        Instance.m_colourOverlay.color = color;
    }

    public static void SetOverlayAlpha(float alpha)
    {
        Color color = Instance.m_colourOverlay.color;
        color.a = alpha;
        Instance.m_colourOverlay.color = color;
    }

    public static GameObject GetUIObjectUnderMouse(Vector3 mousePosition)
    {
        PointerEventData.position = mousePosition;
        EventSystem.current.RaycastAll(PointerEventData, RaycastResults);

        int count = RaycastResults.Count;
        int lowestIndex = -1;
        float lowestDepth = int.MaxValue;

        for (int i = 0; i < count; ++i)
        {
            float depth = RaycastResults[i].distance;
            if (depth < lowestDepth)
            {
                lowestDepth = depth;
                lowestIndex = i;
            }
        }

        GameObject obj = null;
        if (lowestIndex != -1)
            obj = RaycastResults[lowestIndex].gameObject;

        return obj;
    }

    public static bool IsMouseOverUI()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }

    public static void CrossFadeAlphaGroup(CanvasGroup group, float targetAlpha, float smoothTime, bool ignoreTimeScale)
    {
        Timing.RunCoroutine(_CrossFadeAlphaGroup(group, targetAlpha, smoothTime, group.alpha, ignoreTimeScale));
    }

    private static IEnumerator<float> _CrossFadeAlphaGroup(CanvasGroup group, float targetAlpha, float smoothTime, float currentAlpha, bool ignoreTimeScale)
    {
        float refSmooth = 0;
        group.alpha = currentAlpha;
        float deltaTime;

        while (group && MathF.Abs(currentAlpha - targetAlpha) > 0.001f && group.alpha == currentAlpha) //stop coroutine if alpha is modified by somebody else
        {
            deltaTime = Time.deltaTime;
            if (ignoreTimeScale)
                deltaTime = Time.unscaledDeltaTime;

            currentAlpha = Mathf.SmoothDamp(currentAlpha, targetAlpha, ref refSmooth, smoothTime, int.MaxValue, deltaTime);
            group.alpha = currentAlpha;
            yield return Timing.WaitForOneFrame;
        }

        if (group && group.alpha == currentAlpha)
            group.alpha = targetAlpha;
    }

}
