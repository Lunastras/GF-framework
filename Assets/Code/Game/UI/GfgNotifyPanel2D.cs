using System.Collections.Generic;
using UnityEngine;
using MEC;
using TMPro;

public class GfgNotifyPanel2D : GfgNotifyPanelGeneric
{
    [SerializeField] protected TextMeshProUGUI m_continueText;

    [SerializeField] protected TransformTransitionInstance[] m_elementsTransitions;

    [SerializeField] protected TransformTransitionInstance[] m_elementsTransitionsNoName;

    private CanvasGroup[] m_elementsCanvasGroup;

    private readonly Vector3 UNIFORM_SCALE = new(1, 1, 1);

    private TransformData[] m_elementsOriginalLocalData;

    private void Awake()
    {
        m_elementsCanvasGroup = new CanvasGroup[m_elementsTransitions.Length];
        m_elementsOriginalLocalData = new TransformData[m_elementsTransitions.Length];

        for (int i = 0; i < m_elementsCanvasGroup.Length; ++i)
        {
            m_elementsTransitions[i].Transform.GetComponent(ref m_elementsCanvasGroup[i]);
            m_elementsOriginalLocalData[i] = m_elementsTransitions[i].Transform.GetTransformData(true);

            if (m_elementsCanvasGroup[i])
                m_elementsCanvasGroup[i].alpha = 0;

            Debug.Assert(!m_elementsTransitionsNoName.HasValueAt(i)
                        || m_elementsTransitionsNoName[i].Transform == null
                        || m_elementsTransitionsNoName[i].Transform == m_elementsTransitions[i].Transform
                        , "The transform values between the normal transitions and the no name transitions is not the same! The no name transition transform must be either null or equal to its main transition counterpart.");
        }
    }

    protected override void TransitionBox(float aTimeFactor, bool aFadeIn, bool aHasName)
    {
        for (int i = 0; i < m_elementsTransitions.Length; ++i)
        {
            TransformTransitionInstance transition = m_elementsTransitions[i];

            if (!aHasName && m_elementsTransitionsNoName.HasValueAt(i) && m_elementsTransitionsNoName[i].Transform != null)
            {
                transition = m_elementsTransitionsNoName[i];
            }

            float evaluatedCoef = transition.TransitionData.AnimationCurve.Evaluate(aTimeFactor);

            transition.SetStateEvaluated(evaluatedCoef, m_elementsOriginalLocalData[i], aFadeIn);

            if (m_elementsCanvasGroup[i])
                m_elementsCanvasGroup[i].alpha = evaluatedCoef;
        }
    }

    public override CoroutineHandle InitializeOptionButton(GfxTextMessage aTextMessage, GfxButton anInstantiatedButton, GfxNotifyOption aOption, int aIndex)
    {
        GfxPanel panel = anInstantiatedButton as GfxPanel;

        if (panel)
        {
            GfxPanelCreateData createData = GfxUiTools.GetDefaultPanelCreateData();
            createData.ButtonCreateData.Parent = m_optionsButtonsParent;
            createData.IndecesColumnRow = new(0, aIndex);

            panel.SetCreateData(createData, true);
            panel.SetTextOnly(aOption.OptionText);
        }
        else
        {
            Debug.LogError("The spawned option button does not have a GfxPanel component.");
        }

        return default;
    }

    protected override void OnTextWrite(GfxTextMessage aMessage, int aMessageIndex)
    {
    }

    protected override void SubmitOnText(int aMessageIndex, bool aHasName)
    {
        m_continueText.enabled = false;
    }

    protected override IEnumerator<float> _AnimateContinueGraphics()
    {
        m_continueText.enabled = true;

        while (m_currentNotifyCoroutine.CoroutineIsRunning)
        {
            yield return Timing.WaitForSeconds(0.5f);
            m_continueText.enabled = !m_continueText.enabled;
        }
    }
}

[System.Serializable]
public struct TransformTransitionData
{
    public AnimationCurve AnimationCurve;
    public Vector3 LocalPositionStart;
    public Vector3 LocalPositionEnd;
    public Vector3 LocalScaleStart;
    public Vector3 LocalScaleEnd;
}

[System.Serializable]
public struct TransformTransitionInstance
{
    public Transform Transform;
    public TransformTransitionData TransitionData;

    public readonly void SetState(float aTransitionCoef, TransformData aOriginalLocalData, bool aTransitionStart)
    {
        SetStateEvaluated(TransitionData.AnimationCurve.Evaluate(aTransitionCoef), aOriginalLocalData, aTransitionStart);
    }

    public readonly void SetStateEvaluated(float aTransitionCoef, TransformData aOriginalLocalData, bool aTransitionStart)
    {
        Vector3 desiredScale = TransitionData.LocalScaleStart;
        Vector3 desiredLocalPosition = TransitionData.LocalPositionStart;

        if (!aTransitionStart)
        {
            desiredScale = TransitionData.LocalScaleEnd;
            desiredLocalPosition = TransitionData.LocalPositionEnd;
        }

        Transform.localScale = desiredScale.Lerp(aOriginalLocalData.Scale, aTransitionCoef);
        Transform.localPosition = aOriginalLocalData.Position + desiredLocalPosition.Lerp(Vector3.zero, aTransitionCoef);
    }
}