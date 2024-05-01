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

public class GfxNotifyPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_notifyBoxText;
    [SerializeField] private TextMeshProUGUI m_continueText;
    [SerializeField] private RectTransform m_textRectTransform;
    [SerializeField] private AnimationCurve m_animationCurve;
    [SerializeField] private float m_transitionTime;
    [SerializeField] private Vector2 m_localPositionStart;
    [SerializeField] private Vector2 m_localPositionEnd;
    [SerializeField] private Vector2 m_localScaleStart;
    [SerializeField] private Vector2 m_localScaleEnd;

    [SerializeField] private GfxRichTextWriter m_textWriter = new();

    private CanvasGroup m_textCanvasGroup;
    private RectTransform m_rectTransform;
    private CanvasGroup m_canvasGroup;
    private CoroutineHandle m_currentCoroutine = default;
    private bool m_coroutineIsRunning = false;
    private List<string> m_messagesBuffer = new(8);


    [HideInInspector] public Vector3 DesiredLocalPosition;

    private readonly Vector3 UNIFORM_SCALE = new(1, 1, 1);

    private void Awake()
    {
        m_rectTransform = GetComponent<RectTransform>();
        m_canvasGroup = GetComponent<CanvasGroup>();
        m_textCanvasGroup = m_textRectTransform.GetComponent<CanvasGroup>();
        DesiredLocalPosition = m_rectTransform.localPosition;
        m_rectTransform.localScale = UNIFORM_SCALE;
        m_canvasGroup.alpha = 0;
    }

    public CoroutineHandle DrawMessages(IEnumerable<string> someMessages)
    {
        m_messagesBuffer.Add(someMessages);

        if (!m_coroutineIsRunning)
            m_currentCoroutine = Timing.RunCoroutine(_DrawMessages());

        return m_currentCoroutine;
    }

    public bool IsShowingMessages() { return m_coroutineIsRunning; }

    public CoroutineHandle GetCoroutineHandle() { return m_currentCoroutine; }

    private IEnumerator<float> _AnimateContinueGraphics()
    {
        m_continueText.enabled = true;

        while (m_coroutineIsRunning)
        {
            yield return Timing.WaitForSeconds(0.5f);
            m_continueText.enabled = !m_continueText.enabled;
        }
    }

    private IEnumerator<float> _DrawMessages()
    {
        m_coroutineIsRunning = true;
        float invTransitionTime = m_transitionTime > 0.00001f ? 1.0f / m_transitionTime : 0;
        GfcInputTracker submitInput = new(GfcInputType.SUBMIT);
        CoroutineHandle continueAnimationHandle = Timing.RunCoroutine(_AnimateContinueGraphics());

        for (int i = 0; i < m_messagesBuffer.Count; ++i)
        {
            float desiredAlpha = 1;
            Vector2 desiredLocalPosition = DesiredLocalPosition;
            Vector2 desiredScale = UNIFORM_SCALE;

            float currentAlpha = 0;
            Vector2 currentLocalPosition = m_localPositionStart;
            Vector2 currentScale = m_localScaleStart;

            m_notifyBoxText.text = "";
            m_textWriter.m_textMeshPro = m_notifyBoxText;
            m_textWriter.SetString(m_messagesBuffer[i]);

            for (int transitionIndex = 0; transitionIndex < 2; ++transitionIndex)
            {
                float transitionProgress = 0;

                const float EPSILON_ONE = 0.9999f;
                while (transitionProgress < EPSILON_ONE)
                {
                    float curveProgress = m_animationCurve.Evaluate(transitionProgress);

                    m_textCanvasGroup.alpha = m_canvasGroup.alpha = currentAlpha.Lerp(desiredAlpha, curveProgress);
                    m_textRectTransform.localScale = m_rectTransform.localScale = currentScale.Lerp(desiredScale, curveProgress);
                    m_rectTransform.localPosition = currentLocalPosition.Lerp(desiredLocalPosition, curveProgress);

                    yield return Timing.WaitForOneFrame;

                    transitionProgress += Time.deltaTime * invTransitionTime;
                    if (transitionProgress >= EPSILON_ONE) transitionProgress = 1;
                }

                if (transitionIndex == 0)
                {
                    m_textWriter.WriteText(m_notifyBoxText);
                    while (m_textWriter.WritingText())
                    {
                        if (submitInput.PressedSinceLastCheck())
                            m_textWriter.IncrementAll();

                        yield return Timing.WaitForOneFrame;
                    }
                }

                desiredAlpha = 0;
                desiredLocalPosition = m_localPositionEnd;
                desiredScale = m_localScaleEnd;

                currentAlpha = m_canvasGroup.alpha;
                currentLocalPosition = m_rectTransform.localPosition;
                currentScale = m_rectTransform.localScale;

                while (transitionIndex == 0 && !submitInput.PressedSinceLastCheck())
                    yield return Timing.WaitForOneFrame;
            }
        }

        Timing.KillCoroutines(continueAnimationHandle);
        m_messagesBuffer.Clear();
        m_coroutineIsRunning = false;
        m_currentCoroutine = default;
    }
}