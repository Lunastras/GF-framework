using MEC;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Assertions.Must;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using uei = UnityEngine.Internal;

public class GfUiTools : MonoBehaviour
{
    [SerializeField] protected GameObject m_panelPrefab;

    [SerializeField] private TextMeshProUGUI m_bottomNotificationText = null;

    [SerializeField] private Image m_colourOverlay = null;

    [SerializeField] private CanvasGroup m_colourOverlayCanvsGroup = null;

    [SerializeField] private CanvasGroup m_blackBarsCanvsGroup = null;

    [SerializeField] private RectTransform m_blackBarUpper = null;

    [SerializeField] private RectTransform m_blackBarLower = null;

    [SerializeField] private Sprite m_iconCharm;

    [SerializeField] private Sprite m_iconWeapon;

    [SerializeField] private Color m_notificationColorDefault;

    [SerializeField] private Color m_notificationColorError;

    [SerializeField] private Color m_notificationColorWarning;

    [SerializeField] private GfxPanelCreateData m_defaultPanelCreateData;

    private static GfUiTools Instance;
    private static List<RaycastResult> RaycastResults = new(1);
    private static PointerEventData PointerEventData;

    private GfxPanel m_displayedNotificationPanel = null;

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

    public static bool NotifyIsActive() { return false; }

    public static void NotifyMessage(string aMessage)
    {
        //todo
    }

    public static void NotifyMessage(List<string> someMessages)
    {
        //todo
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

    public static GfxPanelCreateData GetDefaultPanelCreateData() { return Instance.m_defaultPanelCreateData; }

    public static Sprite GetIconCharm() { return Instance.m_iconCharm; }

    public static Sprite GetIconWeapon() { return Instance.m_iconWeapon; }

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

    public static void WriteDisableReason(GfxPanel aPanel)
    {
        string reason = aPanel.GetDisabledReason();
        if (!reason.IsEmpty())
        {
            WriteBottomNotification(reason, BottomNotificationType.ERROR);
            Instance.m_displayedNotificationPanel = aPanel;
        }
    }

    public static void EraseDisableReason(GfxPanel aPanel)
    {
        if (Instance.m_displayedNotificationPanel == aPanel)
            WriteBottomNotification(null, BottomNotificationType.ERROR);
    }

    public static void WriteBottomNotification(string aText, BottomNotificationType aNotificationType = BottomNotificationType.NORMAL)
    {
        //todo make nice transition
        Instance.m_displayedNotificationPanel = null;
        Instance.m_bottomNotificationText.text = aText;

        Color textColor;
        switch (aNotificationType)
        {
            case (BottomNotificationType.ERROR):
                textColor = Instance.m_notificationColorError;
                break;

            case (BottomNotificationType.WARNING):
                textColor = Instance.m_notificationColorWarning;
                break;

            default:
                textColor = Color.white;
                break;
        }

        Instance.m_bottomNotificationText.color = textColor;
    }

    public static Color BlendColors(Color aFirstColor, Color aSecondColor, ColorBlendMode aBlendMode)
    {
        Color retCol;
        switch (aBlendMode)
        {
            case ColorBlendMode.INTERPOLATE_USING_ALPHA:
                float secondAlpha = 1 - aSecondColor.a;
                retCol.r = aFirstColor.r * secondAlpha + aSecondColor.r * aSecondColor.a;
                retCol.g = aFirstColor.g * secondAlpha + aSecondColor.g * aSecondColor.a;
                retCol.b = aFirstColor.b * secondAlpha + aSecondColor.b * aSecondColor.a;
                retCol.a = aFirstColor.a;
                break;

            case ColorBlendMode.MULTIPLY:
                retCol = aFirstColor * aSecondColor;
                break;

            case ColorBlendMode.DIVIDE:
                retCol = aFirstColor;
                retCol.r /= aSecondColor.r * aSecondColor.a;
                retCol.g /= aSecondColor.g * aSecondColor.a;
                retCol.b /= aSecondColor.b * aSecondColor.a;
                break;

            case ColorBlendMode.ADD:
                retCol = aFirstColor;
                retCol.r += aSecondColor.r * aSecondColor.a;
                retCol.g += aSecondColor.g * aSecondColor.a;
                retCol.b += aSecondColor.b * aSecondColor.a;
                break;

            case ColorBlendMode.SUBSTRACT:
                retCol = aFirstColor;
                retCol.r -= aSecondColor.r * aSecondColor.a;
                retCol.g -= aSecondColor.g * aSecondColor.a;
                retCol.b -= aSecondColor.b * aSecondColor.a;
                break;

            default: //ColorBlendMode.REPLACE
                retCol = aSecondColor;
                break;

        }

        return retCol;
    }

    // Gradually changes a vector towards a desired goal over time.
    public static Color SmoothDamp(Color current, Color target, ref Color currentVelocity, float smoothTime, float maxSpeed = float.MaxValue, float deltaTime = -1)
    {
        if (deltaTime < 0) deltaTime = Time.deltaTime;

        float output_r;
        float output_g;
        float output_b;
        float output_a;

        // Based on Game Programming Gems 4 Chapter 1.10
        smoothTime = Mathf.Max(0.0001F, smoothTime);
        float omega = 2F / smoothTime;

        float x = omega * deltaTime;
        float exp = 1F / (1F + x + 0.48F * x * x + 0.235F * x * x * x);

        float change_r = current.r - target.r;
        float change_g = current.g - target.g;
        float change_b = current.b - target.b;
        float change_a = current.a - target.a;
        Color originalTo = target;

        // Clamp maximum speed
        float maxChange = maxSpeed * smoothTime;

        float maxChangeSq = maxChange * maxChange;
        float sqrmag = change_r * change_r + change_g * change_g + change_b * change_b + change_a * change_a;
        if (sqrmag > maxChangeSq)
        {
            var correction = MathF.Sqrt(sqrmag) * maxChange;
            change_r *= correction;
            change_g *= correction;
            change_b *= correction;
            change_a *= correction;
        }

        target.r = current.r - change_r;
        target.g = current.g - change_g;
        target.b = current.b - change_b;
        target.a = current.a - change_a;

        float temp_r = (currentVelocity.r + omega * change_r) * deltaTime;
        float temp_g = (currentVelocity.g + omega * change_g) * deltaTime;
        float temp_b = (currentVelocity.b + omega * change_b) * deltaTime;
        float temp_a = (currentVelocity.a + omega * change_b) * deltaTime;

        currentVelocity.r = (currentVelocity.r - omega * temp_r) * exp;
        currentVelocity.g = (currentVelocity.g - omega * temp_g) * exp;
        currentVelocity.b = (currentVelocity.b - omega * temp_b) * exp;
        currentVelocity.a = (currentVelocity.a - omega * temp_a) * exp;

        output_r = target.r + (change_r + temp_r) * exp;
        output_g = target.g + (change_g + temp_g) * exp;
        output_b = target.b + (change_b + temp_b) * exp;
        output_a = target.a + (change_a + temp_a) * exp;

        // Prevent overshooting
        float origMinusCurrent_r = originalTo.r - current.r;
        float origMinusCurrent_g = originalTo.g - current.g;
        float origMinusCurrent_b = originalTo.b - current.b;
        float origMinusCurrent_a = originalTo.b - current.b;

        float outMinusOrig_r = output_r - originalTo.r;
        float outMinusOrig_g = output_g - originalTo.g;
        float outMinusOrig_b = output_b - originalTo.b;
        float outMinusOrig_a = output_a - originalTo.a;

        if (origMinusCurrent_r * outMinusOrig_r + origMinusCurrent_g * outMinusOrig_g + origMinusCurrent_b * outMinusOrig_b + origMinusCurrent_a * outMinusOrig_a > 0)
        {
            output_r = originalTo.r;
            output_g = originalTo.g;
            output_b = originalTo.b;
            output_a = originalTo.b;

            currentVelocity.r = (output_r - originalTo.r) / deltaTime;
            currentVelocity.g = (output_g - originalTo.g) / deltaTime;
            currentVelocity.b = (output_b - originalTo.b) / deltaTime;
            currentVelocity.a = (output_a - originalTo.a) / deltaTime;
        }

        return new Color(output_r, output_g, output_b, output_a);
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

    public static GfxPanelCreateData GetDefaultPanelCreateData(MovementDisableOptions aMovementDisableOptions)
    {
        GfxPanelCreateData createData = Instance.m_defaultPanelCreateData;

        switch (aMovementDisableOptions)
        {
            case MovementDisableOptions.REMOVE_VERTICAL:
                createData.DefaultHighlightState.PositionOffset.y = 0;
                createData.PinnedHighlightState.PositionOffset.y = 0;
                createData.SelectHighlightState.PositionOffset.y = 0;
                createData.DisabledHighlightState.PositionOffset.y = 0;
                createData.SubmitHighlightState.PositionOffset.y = 0;
                break;

            case MovementDisableOptions.REMOVE_HORIZONTAL:
                createData.DefaultHighlightState.PositionOffset.x = 0;
                createData.PinnedHighlightState.PositionOffset.x = 0;
                createData.SelectHighlightState.PositionOffset.x = 0;
                createData.DisabledHighlightState.PositionOffset.x = 0;
                createData.SubmitHighlightState.PositionOffset.x = 0;
                break;

            case MovementDisableOptions.REMOVE_ALL:
                createData.DefaultHighlightState.PositionOffset = new();
                createData.PinnedHighlightState.PositionOffset = new();
                createData.SelectHighlightState.PositionOffset = new();
                createData.DisabledHighlightState.PositionOffset = new();
                createData.SubmitHighlightState.PositionOffset = new();
                break;
        }

        return createData;
    }

    public static GfxPanel CreatePanelWithCache(RectTransform aParent, GfxPanelCreateData aPanelData, List<GfxPanel> somePanelsCache, bool aSnapPanelsToDesiredState = false, bool aForceUpdateCache = true, AlignmentHorizontal aAlignmentHorizontal = AlignmentHorizontal.MIDDLE, AlignmentVertical aAlignmentVertical = AlignmentVertical.MIDDLE, bool aDynamicIndex = false)
    {
        aPanelData.Parent = aParent;
        return CreatePanelWithCache(aPanelData, somePanelsCache, aSnapPanelsToDesiredState, aForceUpdateCache, aAlignmentHorizontal, aAlignmentVertical, aDynamicIndex);
    }

    public static GfxPanel CreatePanelWithCache(GfxPanelCreateData aPanelData, List<GfxPanel> somePanelsCache, bool aSnapPanelsToDesiredState = false, bool aForceUpdateCache = true, AlignmentHorizontal aAlignmentHorizontal = AlignmentHorizontal.MIDDLE, AlignmentVertical aAlignmentVertical = AlignmentVertical.MIDDLE, bool aDynamicIndex = false)
    {
        if (somePanelsCache == null)
            somePanelsCache = new(4);

        if (aForceUpdateCache || somePanelsCache.Count <= aPanelData.Index || somePanelsCache[aPanelData.Index] == null)
        {
            GfxPanel panel;
            bool setPanelData = aForceUpdateCache;

            if (somePanelsCache.Count > aPanelData.Index)
            {
                panel = somePanelsCache[aPanelData.Index];
                if (panel == null)
                {
                    panel = CreatePanelUninitialized();
                    aSnapPanelsToDesiredState = true;
                    somePanelsCache[aPanelData.Index] = panel;
                    setPanelData = true;
                }
            }
            else
            {
                if (somePanelsCache.Count != aPanelData.Index && !aDynamicIndex)
                    Debug.LogError("The count of the list right now is: " + somePanelsCache.Count + " but we are already at index " + aPanelData.Index + ", the previous elements in the list should already be instantiated. If this is intentional, set 'aDynamicIndex' to true");

                panel = CreatePanelUninitialized();
                aSnapPanelsToDesiredState = true;
                somePanelsCache.Add(panel);
                setPanelData = true;
            }

            if (setPanelData)
            {
                CalculateAlignmentOffset(ref aPanelData, new(1, 1), aAlignmentHorizontal, aAlignmentVertical);
                panel.SetCreateData(aPanelData, aSnapPanelsToDesiredState);
            }
        }

        return somePanelsCache[aPanelData.Index];
    }

    public static GfxPanel CreatePanelIfNull(GfxPanelCreateData aPanelData, ref GfxPanel aPanel, bool aSnapPanelsToDesiredState = false, bool aForceUpdateCache = true, AlignmentHorizontal aAlignmentHorizontal = AlignmentHorizontal.MIDDLE, AlignmentVertical aAlignmentVertical = AlignmentVertical.MIDDLE)
    {
        if (aForceUpdateCache || aPanel == null)
        {
            bool setPanelData = aForceUpdateCache;
            if (aPanel == null)
            {
                setPanelData = true;
                aSnapPanelsToDesiredState = true;
                aPanel = CreatePanelUninitialized();
            }

            if (setPanelData)
            {
                CalculateAlignmentOffset(ref aPanelData, new(1, 1), aAlignmentHorizontal, aAlignmentVertical);
                aPanel.SetCreateData(aPanelData, aSnapPanelsToDesiredState);
            }
        }

        return aPanel;
    }

    public static GfxPanel CreatePanelUninitialized()
    {
        return GfcPooling.PoolInstantiate(Instance.m_panelPrefab).GetComponent<GfxPanel>();
    }

    public static GfxPanel CreatePanel(GfxPanelCreateData aPanelData, AlignmentHorizontal aAlignmentHorizontal = AlignmentHorizontal.MIDDLE, AlignmentVertical aAlignmentVertical = AlignmentVertical.MIDDLE)
    {
        GameObject spawnedObject = GfcPooling.PoolInstantiate(Instance.m_panelPrefab, aPanelData.Parent);
        GfxPanel itemPanel = spawnedObject.GetComponent<GfxPanel>();
        CalculateAlignmentOffset(ref aPanelData, new(1, 1), aAlignmentHorizontal, aAlignmentVertical);
        itemPanel.SetCreateData(aPanelData, true);
        return itemPanel;
    }

    public static void CreatePanelList(RectTransform aParent, GfxPanelCreateData aPanelData, List<GfxPanel> somePanelsCache, Axis aDrawAxis, int aPanelCount, bool aSnapPanelsToDesiredState = false, bool aForceUpdateCache = true, AlignmentHorizontal aAlignmentHorizontal = AlignmentHorizontal.MIDDLE, AlignmentVertical aAlignmentVertical = AlignmentVertical.MIDDLE)
    {
        aPanelData.Parent = aParent;
        CreatePanelList(aPanelData, somePanelsCache, aDrawAxis, aPanelCount, aSnapPanelsToDesiredState, aForceUpdateCache, aAlignmentHorizontal, aAlignmentVertical);
    }

    public static void CreatePanelMatrix(RectTransform aParent, GfxPanelCreateData aPanelData, List<GfxPanel> somePanelsCache, Vector2Int aPanelCount, bool aSnapPanelsToDesiredState = false, bool aForceUpdateCache = true, AlignmentHorizontal aAlignmentHorizontal = AlignmentHorizontal.MIDDLE, AlignmentVertical aAlignmentVertical = AlignmentVertical.MIDDLE)
    {
        aPanelData.Parent = aParent;
        CreatePanelMatrix(aPanelData, somePanelsCache, aPanelCount, aSnapPanelsToDesiredState, aForceUpdateCache, aAlignmentHorizontal, aAlignmentVertical);
    }

    private static void CalculateAlignmentOffset(ref GfxPanelCreateData aPanelData, Vector2Int aPanelCount, AlignmentHorizontal aAlignmentHorizontal, AlignmentVertical aAlignmentVertical)
    {
        for (int axis = 0; axis < 2; axis++)
        {
            float listAxisLength = aPanelCount[axis] * (aPanelData.PanelSize[axis] + aPanelData.DistanceFromLastPanel[axis]) - aPanelData.DistanceFromLastPanel[axis];

            int spawnAxisCoefSign = Math.Sign(aPanelData.SpawnAxisCoef[axis]);
            int axisAlignment = axis == (int)Axis.HORIZONTAL ? (int)aAlignmentHorizontal : (int)aAlignmentVertical;
            if (axisAlignment == 0) //MIDDLE
            {
                aPanelData.PositionOffset[axis] += 0.5f * spawnAxisCoefSign * (aPanelData.PanelSize[axis] - listAxisLength);
            }
            else //TOP/BOTTOM, LEFT/RIGHT
            {
                aPanelData.PositionOffset[axis] += 0.5f * spawnAxisCoefSign * aPanelData.PanelSize[axis];

                int sign = axisAlignment == (int)AlignmentHorizontal.LEFT ? 1 : -1; // can be compared with (int)AlignmentVertical.BOTTOM, it is required for these two to have the same value
                if (spawnAxisCoefSign == sign)
                    aPanelData.PositionOffset[axis] -= sign * listAxisLength;
            }
        }
    }

    public static void CreatePanelList(GfxPanelCreateData aPanelData, List<GfxPanel> somePanelsCache, Axis aDrawAxis, int aPanelCount, bool aSnapPanelsToDesiredState = false, bool aForceUpdateCache = true, AlignmentHorizontal aAlignmentHorizontal = AlignmentHorizontal.MIDDLE, AlignmentVertical aAlignmentVertical = AlignmentVertical.MIDDLE)
    {
        if (aForceUpdateCache || somePanelsCache == null || somePanelsCache.Count < aPanelCount)
        {
            CalculateAlignmentOffset(ref aPanelData, aDrawAxis == (int)Axis.HORIZONTAL ? new(aPanelCount, 1) : new(1, aPanelCount), aAlignmentHorizontal, aAlignmentVertical);

            aPanelData.IndecesColumnRow[Math.Abs((int)aDrawAxis - 1)] = 0;
            for (int i = 0; i < aPanelCount; ++i)
            {
                aPanelData.IndecesColumnRow[(int)aDrawAxis] = i;
                CreatePanelWithCache(aPanelData, somePanelsCache, aSnapPanelsToDesiredState, aForceUpdateCache);
                aPanelData.Index++;
            }
        }
    }

    public static void CreatePanelMatrix(GfxPanelCreateData aPanelData, List<GfxPanel> somePanelsCache, Vector2Int aPanelCount, bool aSnapPanelsToDesiredState = false, bool aForceUpdateCache = true, AlignmentHorizontal aAlignmentHorizontal = AlignmentHorizontal.MIDDLE, AlignmentVertical aAlignmentVertical = AlignmentVertical.MIDDLE)
    {
        if (aForceUpdateCache || somePanelsCache == null || somePanelsCache.Count < aPanelCount.x * aPanelCount.y)
        {
            CalculateAlignmentOffset(ref aPanelData, aPanelCount, aAlignmentHorizontal, aAlignmentVertical);

            for (int i = 0; i < aPanelCount.y; ++i)
            {
                aPanelData.IndecesColumnRow.y = i;
                for (int j = 0; j < aPanelCount.x; ++j)
                {
                    aPanelData.IndecesColumnRow.x = j;
                    CreatePanelWithCache(aPanelData, somePanelsCache, aSnapPanelsToDesiredState, aForceUpdateCache);
                    aPanelData.Index++;
                }
            }
        }
    }
}

public enum ColorBlendMode
{
    REPLACE,
    INTERPOLATE_USING_ALPHA,
    MULTIPLY,
    DIVIDE,
    ADD,
    SUBSTRACT
}

public enum BottomNotificationType
{
    NORMAL,
    ERROR,
    WARNING
}

public enum Axis
{
    HORIZONTAL,
    VERTICAL
}

public enum AlignmentHorizontal
{
    MIDDLE,
    LEFT,
    RIGHT,
}

public enum AlignmentVertical
{
    MIDDLE,
    BOTTOM,
    TOP,
}

public enum MovementDisableOptions
{
    REMOVE_HORIZONTAL,
    REMOVE_VERTICAL,
    REMOVE_ALL,
}