using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using MEC;
using TMPro;
using UnityEngine;
using System.Diagnostics;

[Serializable]
public class GfxRichTextWriter
{
    public TextMeshProUGUI TextMeshPro;

    public float LetterDelaySeconds;

    public float LetterTranslateSeconds;

    public bool IgnoreWhitespace;

    //During tests I realised the text is redrawn after I make it transparent when I mess with the scale.
    //setting this to FALSE helped, but the added overhead is questionable, so I prefer keeping to to TRUE
    public bool EarlyTransitionBreak = true;

    //public bool StaticVertexData = true;

    public TextWriterTranslation TranslationFadeIn;

    public TextWriterTranslation TranslationFadeOut;

    private bool m_writeAllOnce = false;

    private float m_secondsSinceWriteAll = 0;

    private float m_writeAllSpeedMultiplier = 0;

    private float m_writeAllSpeedMultiplierInv = 0;

    private GfcCoroutineHandle m_textTransitionHandle = default;

    private GfcCoroutineHandle m_textEraseHandle = default;

    const int VERT_COUNT = 4;

    private List<VertexData> m_vertexData = new(32);

    public GfxRichTextWriter() { }
    public GfxRichTextWriter(GfxRichTextWriter aObjectToCopy)
    {
        TextMeshPro = aObjectToCopy.TextMeshPro;
        LetterDelaySeconds = aObjectToCopy.LetterDelaySeconds;
        LetterTranslateSeconds = aObjectToCopy.LetterTranslateSeconds;
        IgnoreWhitespace = aObjectToCopy.IgnoreWhitespace;
        TranslationFadeIn = aObjectToCopy.TranslationFadeIn;
        TranslationFadeOut = aObjectToCopy.TranslationFadeOut;
    }

    public void SetString(string aString, bool aWriteTextInstant = true)
    {
        TextMeshPro.SetText(aString);
        if (aWriteTextInstant)
            WriteAllNoTransition(true, true);
    }

    public string GetString() { return TextMeshPro.text; }

    public int TextLength { get { return TextMeshPro.textInfo.characterCount; } }

    public bool WritingText()
    {
        return m_textEraseHandle.CoroutineIsRunning || m_textTransitionHandle.CoroutineIsRunning;
    }

    private bool ValidCharacter(char aChar)
    {
        return !IgnoreWhitespace || (aChar != ' ' && aChar != '\n' && aChar != '\t'); //not an exhaustive list
    }

    public CoroutineHandle FinishTranslation(float aSpeedMultiplier = 1)
    {
        if (!m_writeAllOnce && m_textTransitionHandle.CoroutineIsRunning)
        {
            m_writeAllSpeedMultiplier = aSpeedMultiplier;
            m_writeAllSpeedMultiplierInv = aSpeedMultiplier < 0.00001f ? float.MaxValue : 1.0f / aSpeedMultiplier;
            m_secondsSinceWriteAll = 0;
        }

        m_writeAllOnce = m_textTransitionHandle.CoroutineIsRunning;
        return m_textTransitionHandle;
    }

    public CoroutineHandle WriteText(string aString, float aSpeedMultiplier = 1) { SetString(aString, true); return WriteSubString(0, TextLength, true, aSpeedMultiplier, false); }

    public CoroutineHandle WriteText(float aSpeedMultiplier = 1) { return WriteSubString(0, TextLength, true, aSpeedMultiplier); }

    public void RemoveText()
    {
        m_textEraseHandle.KillCoroutine();
        m_textTransitionHandle.KillCoroutine();

        SetString(null);
    }

    public CoroutineHandle EraseText(float aSpeedMultiplier = 1)
    {
        m_textEraseHandle.RunCoroutineIfNotRunning(_EraseText(aSpeedMultiplier));
        return m_textEraseHandle;
    }

    private IEnumerator<float> _EraseText(float aSpeedMultiplier = 1)
    {
        if (m_textTransitionHandle.CoroutineIsRunning)
            yield return Timing.WaitUntilDone(m_textTransitionHandle);

        yield return Timing.WaitUntilDone(WriteSubString(0, TextLength, false, aSpeedMultiplier), false);

        m_textEraseHandle.Finished();
    }

    public void WriteAllNoTransition(bool aIgnoreActiveState = false, bool aForceTextReparsing = false)
    {
        m_textEraseHandle.KillCoroutine();
        m_textTransitionHandle.KillCoroutine();
        TextMeshPro.ForceMeshUpdate(aIgnoreActiveState, aForceTextReparsing);
    }

    //todo, doesn't do what it says it does
    private CoroutineHandle WriteSubString(int aStartIndex, int aFinalIndex, bool aFadeIn, float aSpeedMultiplier = 1, bool aUpdateTmp = true)
    {
        m_textEraseHandle.KillCoroutine();
        m_textTransitionHandle.KillCoroutine();

        if (aUpdateTmp)
            WriteAllNoTransition();

        m_writeAllOnce = false;
        m_secondsSinceWriteAll = 0;
        m_writeAllSpeedMultiplier = 1;
        m_writeAllSpeedMultiplierInv = 1;

        if (aStartIndex != aFinalIndex)
            m_textTransitionHandle.RunCoroutineIfNotRunning(_ExecuteCharacterTransition(aStartIndex, aFinalIndex, aFadeIn, aSpeedMultiplier));

        return m_textTransitionHandle;
    }

    public virtual void ApplyStaticEffects(int aStartIndex, int aFinalIndex, int aIndexSign, TMP_TextInfo aTextInfo)
    {
        TMP_CharacterInfo[] charsInfo = aTextInfo.characterInfo;
    }

    private TMP_VertexDataUpdateFlags ApplyTransitionEffect(bool aFadeIn, float aTimeCoef, int aValidCharacterIndex, TMP_TextInfo aTextInfo, TMP_CharacterInfo aCharacterInfo, VertexData aOriginalVertexData)
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

    private IEnumerator<float> _ExecuteCharacterTransition(int aStartIndex, int aFinalIndex, bool aFadeIn, float aSpeedMultiplier = 1)
    {
        float secondsSinceStart = 0;
        float speedMultiplierInv = aSpeedMultiplier < 0.00001f ? float.MaxValue : 1.0f / aSpeedMultiplier;
        float invLetterTranslateSeconds = aSpeedMultiplier * (LetterTranslateSeconds < 0.00001f ? float.MaxValue : 1.0f / LetterTranslateSeconds);

        var textInfo = TextMeshPro.textInfo;
        var charactersInfo = textInfo.characterInfo;
        int characterCount = textInfo.characterCount;

        int indexSign = (aFinalIndex - aStartIndex).Sign();

        aFinalIndex.MinSelf(characterCount - 1);

        UpdateOriginalVertexDataBuffer(aStartIndex, aFinalIndex, indexSign, textInfo);

        //todo make the routine work with aStartIndex bigger than aFinalIndex and write backwards
        TMP_VertexDataUpdateFlags vertexUpdateFlag = TMP_VertexDataUpdateFlags.None;

        //make ccharacters invisible in the beginning
        if (aFadeIn)
        {
            vertexUpdateFlag |= TMP_VertexDataUpdateFlags.Colors32;

            for (int i = aStartIndex; i * indexSign <= aFinalIndex * indexSign; i += indexSign)
            {
                if (ValidCharacter(charactersInfo[i].character))
                {
                    int vertexIndexStart = charactersInfo[i].vertexIndex;
                    Color32[] vertColors = textInfo.meshInfo[charactersInfo[i].materialReferenceIndex].colors32;

                    for (int j = 0; j < VERT_COUNT; ++j)
                        vertColors[vertexIndexStart + j].a = 0;
                }
            }
        }

        int validIndexOfLastFinished = -1;
        int lastFinishedIndex = aStartIndex - indexSign;

        while (lastFinishedIndex < aFinalIndex)
        {
            int validCharactersCount = 0;

            for (int i = lastFinishedIndex + indexSign; i * indexSign <= aFinalIndex * indexSign; i += indexSign)
            {
                TMP_CharacterInfo charInfo = charactersInfo[i];
                if (ValidCharacter(charInfo.character))  // Skips spaces
                {
                    validCharactersCount++;
                    int validCharacterIndex = validIndexOfLastFinished + validCharactersCount;
                    float auxSecondsSinceStart = secondsSinceStart - validCharacterIndex * LetterDelaySeconds * speedMultiplierInv * m_writeAllSpeedMultiplierInv;
                    auxSecondsSinceStart.MaxSelf(m_secondsSinceWriteAll);

                    if (auxSecondsSinceStart <= 0 && EarlyTransitionBreak)
                        break;

                    float timeCoef = (auxSecondsSinceStart * invLetterTranslateSeconds * m_writeAllSpeedMultiplier).Min(1);
                    float timeCoefAux = aFadeIn ? timeCoef : 1.0f - timeCoef;

                    vertexUpdateFlag |= ApplyTransitionEffect(aFadeIn, timeCoefAux, validCharacterIndex, textInfo, charInfo, m_vertexData[validCharacterIndex]);

                    if (timeCoef >= 1)
                    {
                        validCharactersCount = 0;
                        validIndexOfLastFinished = validCharacterIndex;

                        int lowerBound = aFinalIndex.Min(aStartIndex);
                        int upperBound = aFinalIndex.Max(aStartIndex);

                        while (i > lowerBound && i < upperBound && !ValidCharacter(charactersInfo[i + indexSign].character))
                            i += indexSign;

                        lastFinishedIndex = i;
                    }
                }
            }

            TextMeshPro.UpdateVertexData(vertexUpdateFlag);
            vertexUpdateFlag = TMP_VertexDataUpdateFlags.None;

            float timeOfWaitStart = Time.time;
            yield return Timing.WaitForOneFrame;

            float elapsedSeconds = Time.time - timeOfWaitStart;
            if (m_writeAllOnce) m_secondsSinceWriteAll += elapsedSeconds;
            secondsSinceStart += elapsedSeconds;
        }

        m_textTransitionHandle.Finished();
    }

    //update the the buffer data with the new original vertex data used for the transition lerping
    private void UpdateOriginalVertexDataBuffer(int aStartIndex, int aFinalIndex, int aIndexSign, TMP_TextInfo aTextInfo)
    {
        TMP_CharacterInfo[] charsInfo = aTextInfo.characterInfo;

        ApplyStaticEffects(aStartIndex, aFinalIndex, aIndexSign, aTextInfo);

        m_vertexData.Clear();

        for (int i = aStartIndex; i * aIndexSign <= aFinalIndex * aIndexSign; i += aIndexSign)
        {
            if (ValidCharacter(charsInfo[i].character))
            {
                int vertexIndexStart = charsInfo[i].vertexIndex;
                int materialIndex = charsInfo[i].materialReferenceIndex;

                Color32[] vertColors = aTextInfo.meshInfo[materialIndex].colors32;
                Vector3[] vertPositions = aTextInfo.meshInfo[materialIndex].vertices;

                Vector3 center = Vector3.zero;

                Vector2 vert0To3 = vertPositions[vertexIndexStart + 0] + vertPositions[vertexIndexStart + 3];
                float rotationZDeg = Mathf.Rad2Deg * MathF.Atan2(vert0To3.y, vert0To3.x);

                Quaternion UndoLocalRotation = Quaternion.AngleAxis(-rotationZDeg, Vector3.forward);

                VertexData data = default;
                for (int j = 0; j < VERT_COUNT; ++j)
                {
                    int vertexIndex = vertexIndexStart + j;
                    data.Colors[j] = vertColors[vertexIndex];
                    data.WorldPositions[j] = vertPositions[vertexIndex];
                    GfcTools.Add(ref center, vertPositions[vertexIndex]);
                }

                GfcTools.Mult(ref center, 0.25f);

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
}

[Serializable]
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

internal struct VertexDataBasic
{
    public Vector4<Color32> Colors;
    public Vector4<Vector3> WorldPositions;
}

internal struct Vector4<T>
{
    public T x;
    public T y;
    public T z;
    public T w;

    public T this[int index]
    {
        get
        {
            return index switch
            {
                0 => x,
                1 => y,
                2 => z,
                3 => w,
                _ => throw new IndexOutOfRangeException("Invalid Vector3 index(" + index + ")!"),
            };
        }
        set
        {
            switch (index)
            {
                case 0:
                    x = value;
                    break;
                case 1:
                    y = value;
                    break;
                case 2:
                    z = value;
                    break;
                case 3:
                    w = value;
                    break;
                default:
                    throw new IndexOutOfRangeException("Invalid Vector3 index(" + index + ")!");
            }
        }
    }
}