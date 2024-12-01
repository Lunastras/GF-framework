using MEC;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Assertions.Must;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GfxUiTools : MonoBehaviour
{
    [SerializeField] protected GameObject m_panelPrefab;

    [SerializeField] private TextMeshProUGUI m_bottomNotificationText = null;

    [SerializeField] private GfxNotifyPanelTemplate m_notifyPanel = null;

    [SerializeField] private Image m_colourOverlay = null;

    [SerializeField] private CanvasGroup m_blackBarsCanvsGroup = null;

    [SerializeField] private RectTransform m_blackBarUpper = null;

    [SerializeField] private RectTransform m_blackBarLower = null;

    [SerializeField] private Sprite m_iconCharm;

    [SerializeField] private Sprite m_iconWeapon;

    [SerializeField] private Color m_notificationColorError;

    [SerializeField] private Color m_notificationColorWarning;

    [SerializeField] private GfxPanelCreateData m_defaultPanelCreateData;

    [SerializeField] private AnimationCurve m_defaultAnimationCurve;

    private static GfxUiTools Instance;

    private GfcInteractable m_currentBottomNotificationInteractable = null;

    // Start is called before the first frame update
    void Awake()
    {
        this.SetSingleton(ref Instance);

        if (m_colourOverlay)
        {
            m_colourOverlay.gameObject.SetActive(true);
            m_colourOverlay.CrossFadeAlphaGf(0, 0, true);
        }

        if (m_blackBarsCanvsGroup)
            SetBlackBars(false);
    }

    void OnDestroy()
    {
        Instance = null;
    }

    public static GfxNotifyPanelTemplate GetNotifyPanel() { return Instance.m_notifyPanel; }

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

    public static CoroutineHandle FadeOverlayAlpha(float alpha, float durationSeconds, bool ignoreTimeScale = false)
    {
        return FadeOverlayAlpha(alpha, durationSeconds, ignoreTimeScale, Instance.m_defaultAnimationCurve);
    }

    public static CoroutineHandle FadeOverlayAlpha(float alpha, float durationSeconds, bool ignoreTimeScale, AnimationCurve anAnimationCurve)
    {
        Instance.m_colourOverlay.raycastTarget = alpha > 0.01f;
        return Instance.m_colourOverlay.CrossFadeAlphaGf(alpha, durationSeconds, ignoreTimeScale, anAnimationCurve);
    }

    public static void SetOverlayAlpha(float alpha)
    {
        Color color = Instance.m_colourOverlay.color;
        color.a = alpha;
        Instance.m_colourOverlay.color = color;
    }

    public static void WriteDisabledReason(GfcInteractable anInteractable)
    {
        string reason = anInteractable.NotInteractableReason;
        if (!reason.IsEmpty())
        {
            WriteBottomNotification(reason, GfxBottomNotificationType.ERROR);
            Instance.m_currentBottomNotificationInteractable = anInteractable;
        }
    }

    public static void EraseDisableReason(GfcInteractable anInteractable)
    {
        if (Instance.m_currentBottomNotificationInteractable == anInteractable)
            WriteBottomNotification(null, GfxBottomNotificationType.ERROR);
    }

    public static void WriteBottomNotification(string aText, GfxBottomNotificationType aNotificationType = GfxBottomNotificationType.NORMAL)
    {
        //todo make nice transition
        Instance.m_currentBottomNotificationInteractable = null;
        Instance.m_bottomNotificationText.text = aText;

        Color textColor;
        switch (aNotificationType)
        {
            case GfxBottomNotificationType.ERROR:
                textColor = Instance.m_notificationColorError;
                break;

            case GfxBottomNotificationType.WARNING:
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
            case ColorBlendMode.LERP_USING_ALPHA:
                retCol = Color.Lerp(aFirstColor, aSecondColor, aSecondColor.a);
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

    public static CoroutineHandle CrossFadeAlpha(CanvasGroup aGroup, float aTargetAlpha, float aDurationSeconds, bool anIgnoreTimeScale = false, AnimationCurve anAnimationCurve = null)
    {
        if (aTargetAlpha != aGroup.alpha)
            return Timing.RunCoroutine(_CrossFadeAlphaGroup(aGroup, aTargetAlpha, aDurationSeconds, anIgnoreTimeScale, anAnimationCurve ?? Instance.m_defaultAnimationCurve));
        else
            return default;
    }

    private static IEnumerator<float> _CrossFadeAlphaGroup(CanvasGroup aGroup, float aTargetAlpha, float aDurationSeconds, bool anIgnoreTimeScale, AnimationCurve anAnimationCurve)
    {
        aGroup.alpha += 0.001f; //change it a bit to cancel out other coroutines

        float progress = 0;
        float anInitialAlpha = aGroup.alpha, lastAlpha = anInitialAlpha;

        if (aDurationSeconds > 0)
        {
            float invDuration = 1.0f / aDurationSeconds;

            while (progress < 1 && aGroup && aGroup.alpha == lastAlpha) //stop coroutine if alpha is modified by somebody else or if the image was deleted in the meantime
            {

                aGroup.alpha = anInitialAlpha.Lerp(aTargetAlpha, anAnimationCurve == null ? progress : anAnimationCurve.Evaluate(progress));
                lastAlpha = aGroup.alpha;

                yield return Timing.WaitForOneFrame;

                progress += invDuration * (anIgnoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime);
            }
        }

        progress = 1;
        if (aGroup && aGroup.alpha == lastAlpha)
            aGroup.alpha = aGroup.alpha.Lerp(aTargetAlpha, anAnimationCurve == null ? progress : anAnimationCurve.Evaluate(progress));
    }

    public static CoroutineHandle CrossFadeAlpha(Graphic aGraphic, float aTargetAlpha, float aDurationSeconds, bool anIgnoreTimeScale, AnimationCurve anAnimationCurve = null)
    {
        Color color = aGraphic.color;
        color.a = aTargetAlpha;
        return CrossFadeColor(aGraphic, color, aDurationSeconds, anIgnoreTimeScale, anAnimationCurve);
    }

    public static CoroutineHandle CrossFadeColor(Graphic aGraphic, Color aTargetColor, float aDurationSeconds, bool anIgnoreTimeScale, AnimationCurve anAnimationCurve = null)
    {
        if (aTargetColor != aGraphic.color)
        {
            return Timing.RunCoroutine(_CrossFadeColor(aGraphic, aTargetColor, aDurationSeconds, anIgnoreTimeScale, anAnimationCurve ?? Instance.m_defaultAnimationCurve));
        }
        else
            return default;
    }

    private static IEnumerator<float> _CrossFadeColor(Graphic aGraphic, Color aTargetColor, float aDurationSeconds, bool anIgnoreTimeScale, AnimationCurve anAnimationCurve)
    {
        Color color = aGraphic.color;
        color.a += 0.001f; //change it a bit to cancel out other coroutines
        aGraphic.color = color;

        float progress = 0;
        Color anInitialColor = aGraphic.color, lastColor = anInitialColor;

        if (aDurationSeconds > 0)
        {
            float invDuration = 1.0f / aDurationSeconds;
            float lerpFactor;

            while (progress < 1 && aGraphic && aGraphic.color == lastColor) //stop coroutine if alpha is modified by somebody else or if the image was deleted in the meantime
            {
                lerpFactor = anAnimationCurve == null ? progress : anAnimationCurve.Evaluate(progress);
                aGraphic.color = anInitialColor.Lerp(aTargetColor, lerpFactor);
                lastColor = aGraphic.color;

                yield return Timing.WaitForOneFrame;

                progress += invDuration * (anIgnoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime);
            }
        }

        progress = 1;
        if (aGraphic && aGraphic.color == lastColor)
            aGraphic.color = Color.Lerp(anInitialColor, aTargetColor, anAnimationCurve == null ? progress : anAnimationCurve.Evaluate(progress));
    }

    public static GfxPanelCreateData GetDefaultPanelCreateData(MovementDisableOptions aMovementDisableOptions)
    {
        GfxPanelCreateData createData = Instance.m_defaultPanelCreateData;

        switch (aMovementDisableOptions)
        {
            case MovementDisableOptions.REMOVE_VERTICAL:
                createData.ButtonCreateData.DefaultHighlightState.PositionOffset.y = 0;
                createData.ButtonCreateData.PinnedHighlightState.PositionOffset.y = 0;
                createData.ButtonCreateData.SelectHighlightState.PositionOffset.y = 0;
                createData.ButtonCreateData.DisabledHighlightState.PositionOffset.y = 0;
                createData.ButtonCreateData.SubmitHighlightState.PositionOffset.y = 0;
                break;

            case MovementDisableOptions.REMOVE_HORIZONTAL:
                createData.ButtonCreateData.DefaultHighlightState.PositionOffset.x = 0;
                createData.ButtonCreateData.PinnedHighlightState.PositionOffset.x = 0;
                createData.ButtonCreateData.SelectHighlightState.PositionOffset.x = 0;
                createData.ButtonCreateData.DisabledHighlightState.PositionOffset.x = 0;
                createData.ButtonCreateData.SubmitHighlightState.PositionOffset.x = 0;
                break;

            case MovementDisableOptions.REMOVE_ALL:
                createData.ButtonCreateData.DefaultHighlightState.PositionOffset = new();
                createData.ButtonCreateData.PinnedHighlightState.PositionOffset = new();
                createData.ButtonCreateData.SelectHighlightState.PositionOffset = new();
                createData.ButtonCreateData.DisabledHighlightState.PositionOffset = new();
                createData.ButtonCreateData.SubmitHighlightState.PositionOffset = new();
                break;
        }

        return createData;
    }

    public static GfxPanel CreatePanelWithCache(RectTransform aParent, GfxPanelCreateData aPanelData, List<GfxPanel> somePanelsCache, bool aSnapPanelsToDesiredState = false, bool aForceUpdateCache = true, AlignmentHorizontal aAlignmentHorizontal = AlignmentHorizontal.MIDDLE, AlignmentVertical aAlignmentVertical = AlignmentVertical.MIDDLE, bool aDynamicIndex = false)
    {
        aPanelData.ButtonCreateData.Parent = aParent;
        return CreatePanelWithCache(aPanelData, somePanelsCache, aSnapPanelsToDesiredState, aForceUpdateCache, aAlignmentHorizontal, aAlignmentVertical, aDynamicIndex);
    }

    public static GfxPanel CreatePanelWithCache(GfxPanelCreateData aPanelData, List<GfxPanel> somePanelsCache, bool aSnapPanelsToDesiredState = false, bool aForceUpdateCache = true, AlignmentHorizontal aAlignmentHorizontal = AlignmentHorizontal.MIDDLE, AlignmentVertical aAlignmentVertical = AlignmentVertical.MIDDLE, bool aDynamicIndex = false)
    {
        if (somePanelsCache == null)
            somePanelsCache = new(4);

        int index = aPanelData.ButtonCreateData.Index;
        if (aForceUpdateCache || somePanelsCache.Count <= index || somePanelsCache[index] == null)
        {
            GfxPanel panel;
            bool setPanelData = aForceUpdateCache;

            if (somePanelsCache.Count > index)
            {
                panel = somePanelsCache[index];
                if (panel == null)
                {
                    panel = CreatePanelUninitialized();
                    aSnapPanelsToDesiredState = true;
                    somePanelsCache[index] = panel;
                    setPanelData = true;
                }
            }
            else
            {
                if (somePanelsCache.Count != index && !aDynamicIndex)
                    Debug.LogError("The count of the list right now is: " + somePanelsCache.Count + " but we are already at index " + index + ", the previous elements in the list should already be instantiated. If this is intentional, set 'aDynamicIndex' to true");

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

        return somePanelsCache[index];
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
        GameObject spawnedObject = GfcPooling.PoolInstantiate(Instance.m_panelPrefab, aPanelData.ButtonCreateData.Parent);
        GfxPanel itemPanel = spawnedObject.GetComponent<GfxPanel>();
        CalculateAlignmentOffset(ref aPanelData, new(1, 1), aAlignmentHorizontal, aAlignmentVertical);
        itemPanel.SetCreateData(aPanelData, true);
        return itemPanel;
    }

    public static void CreatePanelList(RectTransform aParent, GfxPanelCreateData aPanelData, List<GfxPanel> somePanelsCache, Axis aDrawAxis, int aPanelCount, bool aSnapPanelsToDesiredState = false, bool aForceUpdateCache = true, AlignmentHorizontal aAlignmentHorizontal = AlignmentHorizontal.MIDDLE, AlignmentVertical aAlignmentVertical = AlignmentVertical.MIDDLE)
    {
        aPanelData.ButtonCreateData.Parent = aParent;
        CreatePanelList(aPanelData, somePanelsCache, aDrawAxis, aPanelCount, aSnapPanelsToDesiredState, aForceUpdateCache, aAlignmentHorizontal, aAlignmentVertical);
    }

    public static void CreatePanelMatrix(RectTransform aParent, GfxPanelCreateData aPanelData, List<GfxPanel> somePanelsCache, Vector2Int aPanelCount, bool aSnapPanelsToDesiredState = false, bool aForceUpdateCache = true, AlignmentHorizontal aAlignmentHorizontal = AlignmentHorizontal.MIDDLE, AlignmentVertical aAlignmentVertical = AlignmentVertical.MIDDLE)
    {
        aPanelData.ButtonCreateData.Parent = aParent;
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
                aPanelData.ButtonCreateData.Index++;
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
                    aPanelData.ButtonCreateData.Index++;
                }
            }
        }
    }
}

public static class GfxUiToolsStatic
{
    public static CoroutineHandle CrossFadeAlphaGf(this CanvasGroup aGroup, float aTargetAlpha, float aDurationSeconds, bool anIgnoreTimeScale = false, AnimationCurve anAnimationCurve = null)
    {
        return GfxUiTools.CrossFadeAlpha(aGroup, aTargetAlpha, aDurationSeconds, anIgnoreTimeScale, anAnimationCurve);
    }

    public static CoroutineHandle CrossFadeAlphaGf(this Graphic aGraphic, float aTargetAlpha, float aDurationSeconds, bool anIgnoreTimeScale, AnimationCurve anAnimationCurve = null)
    {
        return GfxUiTools.CrossFadeAlpha(aGraphic, aTargetAlpha, aDurationSeconds, anIgnoreTimeScale, anAnimationCurve);
    }

    public static CoroutineHandle CrossFadeColorGf(this Graphic aGraphic, Color aTargetColor, float aDurationSeconds, bool anIgnoreTimeScale, AnimationCurve anAnimationCurve = null)
    {
        return GfxUiTools.CrossFadeColor(aGraphic, aTargetColor, aDurationSeconds, anIgnoreTimeScale, anAnimationCurve);
    }

    public static Color Lerp(this Color aLeftCol, Color aRightCol, float aCoef) { return Color.Lerp(aLeftCol, aRightCol, aCoef); }

    public static void LerpSelf(this ref Color aLeftCol, Color aRightCol, float aCoef) { aLeftCol = Color.Lerp(aLeftCol, aRightCol, aCoef); }

    public static Color Blend(this Color aLeftCol, Color aRightCol, ColorBlendMode aBlendMode) { return GfxUiTools.BlendColors(aLeftCol, aRightCol, aBlendMode); }

    public static void BlendSelf(this ref Color aLeftCol, Color aRightCol, ColorBlendMode aBlendMode) { aLeftCol = GfxUiTools.BlendColors(aLeftCol, aRightCol, aBlendMode); }
}

public enum ColorBlendMode
{
    MULTIPLY,
    LERP_USING_ALPHA,
    REPLACE,
    DIVIDE,
    ADD,
    SUBSTRACT
}

public enum GfxBottomNotificationType
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