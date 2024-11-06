using System.Collections.Generic;
using MEC;
using TMPro;
using UnityEngine;

[System.Serializable]
[RequireComponent(typeof(TextMeshProUGUI))]
public class GfxRichTextWriter : MonoBehaviour
{
    public TextMeshProUGUI TextMeshPro
    {
        get { return m_textMeshPro; }
        set
        {
            if (m_textMeshPro) m_textMeshPro.OnPreRenderText -= OnPreRenderText;
            m_textMeshPro = value;
            m_textMeshPro.OnPreRenderText += OnPreRenderText;
        }
    }

    public FloatAndInv LetterDelaySeconds;

    public FloatAndInv LetterTranslateSeconds;

    public FloatAndInv SpeedMultiplier = new(1);

    //public bool StaticVertexData = true;

    public TextWriterTranslation TranslationFadeIn;

    public TextWriterTranslation TranslationFadeOut;

    protected TextMeshProUGUI m_textMeshPro;

    private FloatAndInv m_writeAllSpeedMultiplier = new(0);

    private bool m_writeAllOnce = false;

    private float m_secondsSinceWriteAll = 0;

    private GfcCoroutineHandle m_textAnimationHandle = default;

    private GfcCoroutineHandle m_textEraseHandle = default;
    private TransitionRuntimeData m_animData;

    const int VERT_COUNT = 4;

    private List<VertexData> m_vertexData = new(32);

    private int m_lastFramePreRender = 0;

    public GfxRichTextWriter() { }

    private void Awake()
    {
        LetterDelaySeconds.Refresh();
        LetterTranslateSeconds.Refresh();
        SpeedMultiplier.Refresh();
        m_writeAllSpeedMultiplier.Refresh();
        TextMeshPro = GetComponent<TextMeshProUGUI>();
    }

    public void SetString(string aString, bool aWriteTextInstant = true)
    {
        m_textMeshPro.SetText(aString);
        if (aWriteTextInstant)
            WriteAllNoTransition(true, true);
    }

    public string GetString() { return m_textMeshPro.text; }

    public int TextLength { get { return m_textMeshPro.textInfo.characterCount; } }

    public bool WritingText()
    {
        bool ret = m_textEraseHandle.CoroutineIsRunning || m_textAnimationHandle.CoroutineIsRunning;
        //if (ret) Debug.Log("RICH WRITER: ERASER (" + m_textEraseHandle.CoroutineIsRunning + ") - ANIMATION(" + m_textAnimationHandle.CoroutineIsRunning + ")");
        return ret;
    }

    private bool ValidCharacter(char aChar) { return aChar != ' ' && aChar != '\n' && aChar != '\t'; }//not an exhaustive list

    public CoroutineHandle FinishTranslation(float aSpeedMultiplier = 1)
    {
        if (!m_writeAllOnce && m_textAnimationHandle.CoroutineIsRunning)
        {
            m_writeAllSpeedMultiplier.Value = aSpeedMultiplier;
            m_secondsSinceWriteAll = 0;
        }

        m_writeAllOnce = m_textAnimationHandle.CoroutineIsRunning;
        return m_textAnimationHandle;
    }

    public CoroutineHandle WriteText(string aString, float aSpeedMultiplier = 1) { SetString(aString, true); return WriteStringAnimation(true, aSpeedMultiplier); }

    public void RemoveText() { SetString(null); }

    public CoroutineHandle EraseText(float aSpeedMultiplier = 1) { return m_textEraseHandle.RunCoroutineIfNotRunning(_EraseText(aSpeedMultiplier)); }

    private IEnumerator<float> _EraseText(float aSpeedMultiplier = 1)
    {
        m_textAnimationHandle.KillCoroutine();

        yield return Timing.WaitUntilDone(WriteStringAnimation(false, aSpeedMultiplier));
        m_textMeshPro.text = "";

        m_textEraseHandle.Finished();

        //shouldn't be required but I found a weird bug where the animation handle is active after the erase handle stops.
        //If you want to fix that bug remove this line and figure out what's wrong lmao
        m_textAnimationHandle.KillCoroutine();
    }

    public void WriteAllNoTransition(bool aIgnoreActiveState = false, bool aForceTextReparsing = false)
    {
        m_textEraseHandle.KillCoroutine();
        m_textAnimationHandle.KillCoroutine();
        m_textMeshPro.ForceMeshUpdate(aIgnoreActiveState, aForceTextReparsing);
    }

    private CoroutineHandle WriteStringAnimation(bool aFadeIn, float aSpeedMultiplier = 1) { return m_textAnimationHandle.RunCoroutineIfNotRunning(_ExecuteCharacterTransition(aFadeIn, aSpeedMultiplier)); }

    public virtual void ApplyStaticEffects(TMP_TextInfo aTextInfo)
    {
        TMP_CharacterInfo[] charsInfo = aTextInfo.characterInfo;
    }

    private void ApplyTransitionEffectToText(bool anInitializeStep, bool anAllowEarlyBreak = false)
    {
        int validCharactersCount = 0;
        TMP_TextInfo aTextInfo = m_textMeshPro.textInfo;
        var charactersInfo = aTextInfo.characterInfo;
        int characterCount = aTextInfo.characterCount;

        TMP_VertexDataUpdateFlags vertexUpdateFlag = TMP_VertexDataUpdateFlags.None;

        if (anInitializeStep)
        {
            m_animData.LastFinishedIndex = -1;
            m_animData.ValidIndexOfLastFinishedChar = -1;

            UpdateOriginalVertexDataBuffer();
        }

        for (int i = m_animData.LastFinishedIndex + 1; i < characterCount; i++)
        {
            TMP_CharacterInfo charInfo = charactersInfo[i];
            if (ValidCharacter(charInfo.character))// Skips spaces
            {
                validCharactersCount++;
                int validCharacterIndex = m_animData.ValidIndexOfLastFinishedChar + validCharactersCount;
                float auxSecondsSinceStart = m_animData.SecondsSinceStart - validCharacterIndex * LetterDelaySeconds * SpeedMultiplier.Inverse * m_writeAllSpeedMultiplier.Inverse;
                auxSecondsSinceStart.MaxSelf(m_secondsSinceWriteAll);

                if (!anInitializeStep && anAllowEarlyBreak && auxSecondsSinceStart <= 0)
                    break;

                float timeCoef = (SpeedMultiplier * auxSecondsSinceStart * LetterTranslateSeconds.Inverse * m_writeAllSpeedMultiplier).Min(1);
                float timeCoefAux = m_animData.FadeIn ? timeCoef : 1.0f - timeCoef;

                vertexUpdateFlag |= ApplyTransitionEffectToChar(m_animData.FadeIn, timeCoefAux, validCharacterIndex, aTextInfo, charInfo, m_vertexData[validCharacterIndex]);

                if (timeCoef >= 1)
                {
                    m_animData.LastFinishedIndex = i;
                    validCharactersCount = 0;
                    m_animData.ValidIndexOfLastFinishedChar = validCharacterIndex;
                }
            }
        }

        m_textMeshPro.UpdateVertexData(vertexUpdateFlag);
    }

    private TMP_VertexDataUpdateFlags ApplyTransitionEffectToChar(bool aFadeIn, float aTimeCoef, int aValidCharacterIndex, TMP_TextInfo aTextInfo, TMP_CharacterInfo aCharacterInfo, VertexData aOriginalVertexData)
    {
        TextWriterTranslation translation = aFadeIn ? TranslationFadeIn : TranslationFadeOut;

        int vertexIndexStart = aCharacterInfo.vertexIndex;
        float coef = aValidCharacterIndex % 2 == 0 ? 1 : -1;
        coef = 1;

        float lerpProgress = translation.TransitionCurve.Evaluate(aTimeCoef).Clamp(0, 1);

        Vector3 globalPositionOffset = translation.GlobalPosition * (coef * (1.0f - lerpProgress));
        Vector3 localPositionOffset = translation.LocalPosition * (coef * (1.0f - lerpProgress));

        Vector3 currentLocalScale = translation.LocalScale.Lerp(new Vector3(1, 1, 1), lerpProgress);
        Vector3 currentGlobalScale = translation.GlobalScale.Lerp(new Vector3(1, 1, 1), lerpProgress);

        int materialIndex = aCharacterInfo.materialReferenceIndex;
        Color32[] vertColors = aTextInfo.meshInfo[materialIndex].colors32;
        Vector3[] vertPositions = aTextInfo.meshInfo[materialIndex].vertices;

        Quaternion localRotation = Quaternion.AngleAxis(translation.LocalRotationZ.Lerp(0, lerpProgress), Vector3.forward);

        for (int j = 0; j < VERT_COUNT; ++j)
        {
            int vertexIndex = vertexIndexStart + j;
            Vector3 currentPosition = aOriginalVertexData.ModelPositions[j];

            //apply local transform
            GfcTools.Mult(ref currentPosition, currentLocalScale);
            currentPosition = localRotation * currentPosition;
            GfcTools.Add(ref currentPosition, localPositionOffset);

            //model/local to world
            currentPosition = aOriginalVertexData.LocalRotation * currentPosition;
            GfcTools.Add(ref currentPosition, aOriginalVertexData.Center);

            //apply global transform
            GfcTools.Mult(ref currentPosition, currentGlobalScale);
            GfcTools.Add(ref currentPosition, globalPositionOffset);

            Color32 transitionColor = GfxUiTools.BlendColors(aOriginalVertexData.Colors[j], translation.Color, translation.ColorBlendMode);

            vertPositions[vertexIndex] = currentPosition;
            vertColors[vertexIndex] = Color32.Lerp(transitionColor, aOriginalVertexData.Colors[j], lerpProgress);
        }

        return TMP_VertexDataUpdateFlags.Colors32 | TMP_VertexDataUpdateFlags.Vertices;
    }

    private void OnPreRenderText(TMP_TextInfo aTmpInfo)
    {
        if (m_textAnimationHandle.CoroutineIsRunning)
        {
            ApplyTransitionEffectToText(true);
            m_lastFramePreRender = Time.frameCount;
        }
    }

    private IEnumerator<float> _ExecuteCharacterTransition(bool aFadeIn, float aSpeedMultiplier = 1)
    {
        SpeedMultiplier.Value = aSpeedMultiplier;

        if (TextLength == 0)
            yield break;

        m_writeAllOnce = false;
        m_secondsSinceWriteAll = 0;
        m_writeAllSpeedMultiplier.Value = 1;

        m_animData.LastFinishedIndex = -1;
        m_animData.ValidIndexOfLastFinishedChar = -1;

        m_animData.FadeIn = aFadeIn;
        m_animData.SecondsSinceStart = 0;

        bool firstIteration = true;

        int characterCount = TextLength;
        int lastIndex = characterCount - 1;

        while (m_animData.LastFinishedIndex < lastIndex)
        {
            if (m_lastFramePreRender != Time.frameCount)
                ApplyTransitionEffectToText(firstIteration, !firstIteration);

            float timeOfWaitStart = Time.time;
            yield return Timing.WaitForOneFrame;

            float elapsedSeconds = Time.time - timeOfWaitStart;
            if (m_writeAllOnce) m_secondsSinceWriteAll += elapsedSeconds;
            m_animData.SecondsSinceStart += elapsedSeconds;
            firstIteration = false;
        }

        m_textAnimationHandle.Finished();
    }

    //update the the buffer data with the new original vertex data used for the transition lerping
    private void UpdateOriginalVertexDataBuffer()
    {
        TMP_TextInfo aTextInfo = m_textMeshPro.textInfo;
        TMP_CharacterInfo[] charsInfo = aTextInfo.characterInfo;
        int length = TextLength;

        ApplyStaticEffects(aTextInfo);

        m_vertexData.Clear();

        for (int i = 0; i < length; i++)
        {
            if (ValidCharacter(charsInfo[i].character))
            {
                int vertexIndexStart = charsInfo[i].vertexIndex;
                int materialIndex = charsInfo[i].materialReferenceIndex;

                Color32[] vertColors = aTextInfo.meshInfo[materialIndex].colors32;
                Vector3[] vertPositions = aTextInfo.meshInfo[materialIndex].vertices;

                Vector3 center = Vector3.zero;

                Vector2 vert0To3 = vertPositions[vertexIndexStart + 0] + vertPositions[vertexIndexStart + 3];
                float rotationZDeg = Mathf.Rad2Deg * System.MathF.Atan2(vert0To3.y, vert0To3.x);

                Quaternion UndoLocalRotation = Quaternion.AngleAxis(-rotationZDeg, Vector3.forward);

                VertexData data = default;
                for (int j = 0; j < VERT_COUNT; ++j)
                {
                    int vertexIndex = vertexIndexStart + j;
                    data.Colors[j] = vertColors[vertexIndex];
                    data.WorldPositions[j] = vertPositions[vertexIndex];
                    GfcTools.Add(ref center, vertPositions[vertexIndex]);
                }

                GfcTools.Mult(ref center, 0.25f); //average of the positions, same as dividing by 4

                for (int j = 0; j < VERT_COUNT; ++j)
                {
                    int vertexIndex = vertexIndexStart + j;
                    Vector3 modelPosition = vertPositions[vertexIndex];

                    //world to model
                    GfcTools.Minus(ref modelPosition, center);
                    modelPosition = UndoLocalRotation * modelPosition;

                    data.ModelPositions[j] = modelPosition;
                }

                data.Center = center;
                data.LocalZRotationDeg = rotationZDeg;
                data.LocalRotation = Quaternion.AngleAxis(rotationZDeg, Vector3.forward);

                m_vertexData.Add(data);
            }
        }
    }

    protected void OnDestroy()
    {
        WriteAllNoTransition();
    }
}

[System.Serializable]
public struct TextWriterTranslation
{
    public Vector3 GlobalPosition;

    public Vector3 LocalPosition;

    public Vector3 GlobalScale;

    public Vector3 LocalScale;

    public AnimationCurve TransitionCurve;

    public Color32 Color;

    public ColorBlendMode ColorBlendMode;

    public float LocalRotationZ;
}

internal struct VertexData
{
    public Vector4<Color32> Colors;
    public Vector4<Vector3> WorldPositions;
    public Vector4<Vector3> ModelPositions;
    public Quaternion LocalRotation;
    public Vector3 Center;
    public float LocalZRotationDeg;
}

internal struct TransitionRuntimeData
{
    public float SecondsSinceStart;
    public int ValidIndexOfLastFinishedChar;
    public int LastFinishedIndex;
    public bool FadeIn;
}