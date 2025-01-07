using UnityEngine;
using UnityEngine.UI;

public class GfxButton2D : GfxButton
{
    public Transform VisualsParent;

    public Graphic[] GraphicsPanel;

    public Graphic[] GraphicsContent;

    public AnimationCurve TransitionCurve = new(new Keyframe(0, 0), new Keyframe(1, 1));

    public bool LocalTransformRelativeToOriginal = true;

    private Color[] m_colorAtBeginningOfTransition = null;

    private Color[] m_originalColors;

    private Vector3 m_scaleAtStartOfTransition = default;

    private Vector3 m_localPositionAtStartOfTransition = default;

    private Vector3 m_originalLocalPosition = default;

    private Vector3 m_originalLocalScale = new(1, 1, 1);

    private bool m_initialized2DButton = false;

    public override void Initialize()
    {
        if (!m_initialized2DButton)
        {
            if (VisualsParent == null)
            {
                Debug.Assert(transform.childCount == 1, "The VisualsParent is not assigned in the editor, but the transform of the button doesn't have a single child for its default parent. Please assign the VisualParent in the editor.");
                VisualsParent = transform.GetChild(0);
            }

            m_originalLocalPosition = VisualsParent.localPosition;
            m_originalLocalScale = VisualsParent.localScale;

            int countColors = GraphicsPanel.Length + GraphicsContent.Length;

            m_originalColors = new Color[countColors];
            m_colorAtBeginningOfTransition = new Color[countColors];

            UpdateOriginalColors();

            m_initialized2DButton = true;
        }

        base.Initialize();
    }

    public void UpdateOriginalColors()
    {
        int currentIndex = 0;

        foreach (Graphic graphic in GraphicsPanel)
            m_originalColors[currentIndex++] = graphic.color;

        foreach (Graphic graphic in GraphicsContent)
            m_originalColors[currentIndex++] = graphic.color;
    }

    protected override void SetTransitionLerpState(float aTransitionPoint, GfxButtonHightlightState aDesiredState)
    {
        if (TransitionCurve != null)
            aTransitionPoint = TransitionCurve.Evaluate(aTransitionPoint);

        int colorIndex = 0;

        foreach (Graphic graphic in GraphicsPanel)
        {
            Color color = GfxUiTools.BlendColors(m_originalColors[colorIndex], aDesiredState.ColorPanel, aDesiredState.ColorPanelBlendMode);
            color.a *= aDesiredState.Opacity;

            graphic.color = Color.Lerp(m_colorAtBeginningOfTransition[colorIndex++], color, aTransitionPoint);
        }

        foreach (Graphic graphic in GraphicsContent)
        {
            Color color = GfxUiTools.BlendColors(m_originalColors[colorIndex], aDesiredState.ColorContent, aDesiredState.ColorContentBlendMode);
            color.a *= aDesiredState.Opacity;

            graphic.color = Color.Lerp(m_colorAtBeginningOfTransition[colorIndex++], color, aTransitionPoint);
        }

        VisualsParent.localScale = Vector3.Lerp(m_scaleAtStartOfTransition, aDesiredState.Scale.Mult(LocalTransformRelativeToOriginal ? m_originalLocalScale : new Vector3(1, 1, 1)), aTransitionPoint);
        VisualsParent.localPosition = Vector3.Lerp(m_localPositionAtStartOfTransition, aDesiredState.PositionOffset + (LocalTransformRelativeToOriginal ? m_originalLocalPosition : Vector3.zero), aTransitionPoint);
        //todo aDesiredState.PositionOffset does not work for some reason
    }

    protected override void OnStartTransition()
    {
        Debug.Assert(m_colorAtBeginningOfTransition != null);
        int colourIndex = 0;
        foreach (Graphic graphic in GraphicsPanel)
            m_colorAtBeginningOfTransition[colourIndex++] = graphic.color;

        foreach (Graphic graphic in GraphicsContent)
            m_colorAtBeginningOfTransition[colourIndex++] = graphic.color;

        m_scaleAtStartOfTransition = VisualsParent.localScale;
        m_localPositionAtStartOfTransition = VisualsParent.localPosition;
    }

    public void SetOriginalColorOfGraphic(Graphic aGraphic, Color aColor, ColorBlendMode aBlendMode = ColorBlendMode.REPLACE)
    {
        int colourIndex = 0;
        bool foundTheGraphic = false;

        foreach (Graphic graphic in GraphicsPanel)
        {
            foundTheGraphic = graphic == aGraphic;
            if (foundTheGraphic) break;
            colourIndex++;
        }

        if (!foundTheGraphic)
            foreach (Graphic graphic in GraphicsContent)
            {
                foundTheGraphic = graphic == aGraphic;
                if (foundTheGraphic) break;
                colourIndex++;
            }

        if (foundTheGraphic)
        {
            m_originalColors[colourIndex].BlendSelf(aColor, aBlendMode);
            StartTransitionToNewState();
        }
    }

    public void SetPanelColor(Color aColor, ColorBlendMode aBlendMode = ColorBlendMode.REPLACE)
    {
        for (int i = 0; i < GraphicsPanel.Length; ++i)
            m_originalColors[i].BlendSelf(aColor, aBlendMode);

        StartTransitionToNewState();
    }

    public void SetContentColor(Color aColor, ColorBlendMode aBlendMode = ColorBlendMode.REPLACE)
    {
        for (int i = GraphicsPanel.Length; i < m_originalColors.Length; ++i)
            m_originalColors[i].BlendSelf(aColor, aBlendMode);

        StartTransitionToNewState();
    }
}