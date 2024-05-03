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
    public TextMeshProUGUI m_textMeshPro;

    public float LetterDelaySeconds;

    public float LetterTranslateSeconds;

    public bool IgnoreWhitespace;

    public TextWriterTranslation TranslationFadeIn;

    public TextWriterTranslation TranslationFadeOut;

    private bool m_writeAllOnce = false;

    private float m_secondsSinceWriteAll = 0;

    private float m_writeAllSpeedMultiplier = 0;

    private float m_writeAllSpeedMultiplierInv = 0;

    private GfcCoroutineHandle m_textTransitionHandle = default;

    private GfcCoroutineHandle m_textEraseHandle = default;

    const int VERT_COUNT = 4;

    private List<VertexData> m_vertexData = new(1);

    public void SetString(string aString, bool aWriteTextInstant = true)
    {
        m_textMeshPro.text = aString;
        if (aWriteTextInstant)
            WriteAllNoTransition();
    }

    public string GetString() { return m_textMeshPro.text; }

    public int TextLength { get { return m_textMeshPro.textInfo.characterCount; } }

    public bool WritingText() { return m_textEraseHandle.CoroutineIsRunning || m_textTransitionHandle.CoroutineIsRunning; }

    private bool ValidCharacter(char aChar)
    {
        return !IgnoreWhitespace || (aChar != ' ' && aChar != '\n' && aChar != '\t'); //not an exhaustive list
    }

    public CoroutineHandle FinishText(float aSpeedMultiplier = 1)
    {
        if (!m_writeAllOnce && m_textTransitionHandle.CoroutineIsRunning)
        {
            m_writeAllSpeedMultiplier = aSpeedMultiplier;
            m_writeAllSpeedMultiplierInv = aSpeedMultiplier < 0.00001f ? float.MaxValue : 1.0f / aSpeedMultiplier;
            m_secondsSinceWriteAll = 0;

            UnityEngine.Debug.Log("Finish text called");
        }

        m_writeAllOnce = m_textTransitionHandle.CoroutineIsRunning;
        return m_textTransitionHandle;
    }

    public CoroutineHandle WriteText(float aSpeedMultiplier = 1) { return WriteSubString(0, TextLength, true, aSpeedMultiplier); }

    public CoroutineHandle EraseText(float aSpeedMultiplier = 1, bool aSkipTransition = false)
    {
        if (aSkipTransition)
        {
            m_textTransitionHandle.KillCoroutine();
            SetString(null);
        }
        else
        {
            m_textEraseHandle.RunCoroutineIfNotRunning(_EraseText(aSpeedMultiplier));
        }

        return m_textEraseHandle;
    }

    private IEnumerator<float> _EraseText(float aSpeedMultiplier = 1)
    {
        if (m_textTransitionHandle.CoroutineIsRunning)
            yield return Timing.WaitUntilDone(m_textTransitionHandle);

        WriteSubString(0, TextLength, false, aSpeedMultiplier);
        yield return Timing.WaitUntilDone(m_textTransitionHandle);

        m_textEraseHandle.Finished();
    }

    public void WriteAllNoTransition(bool aIgnoreActiveState = false, bool aForceTextReparsing = false) { m_textMeshPro.ForceMeshUpdate(aIgnoreActiveState, aForceTextReparsing); }

    //todo, doesn't do what it says it does
    private CoroutineHandle WriteSubString(int aStartIndex, int aFinalIndex, bool aFadeIn, float aSpeedMultiplier = 1, bool aUpdateTmp = true)
    {
        m_textTransitionHandle.KillCoroutine();

        m_textEraseHandle.KillCoroutine();

        if (aUpdateTmp)
            WriteAllNoTransition();

        m_writeAllOnce = false;
        m_secondsSinceWriteAll = 0;
        m_writeAllSpeedMultiplier = 1;
        m_writeAllSpeedMultiplierInv = 1;

        m_textTransitionHandle.RunCoroutineIfNotRunning(_ExecuteCharacterTransition(aStartIndex, aFinalIndex, aFadeIn, aSpeedMultiplier));

        return m_textTransitionHandle;
    }

    private TMP_VertexDataUpdateFlags ApplyEffect(bool aFadeIn, float aTimeCoef, int aValidCharacterIndex, TMP_TextInfo aTextInfo, TMP_CharacterInfo aCharacterInfo, VertexData aOriginalVertexData)
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

        Vector3 center = Vector3.zero;
        for (int j = 0; j < VERT_COUNT; ++j)
            GfcTools.Add(ref center, aOriginalVertexData.Positions[j]);
        GfcTools.Mult(ref center, 0.25f);

        Vector2 vert0To3 = vertPositions[vertexIndexStart + 0] + vertPositions[vertexIndexStart + 3];
        float rotationZDeg = Mathf.Rad2Deg * MathF.Atan2(vert0To3.y, vert0To3.x);
        Quaternion applyRotation = Quaternion.AngleAxis(rotationZDeg, Vector3.forward);
        Quaternion undeRotation = Quaternion.AngleAxis(-rotationZDeg, Vector3.forward);
        Quaternion localRotation = Quaternion.AngleAxis(Mathf.LerpAngle(translation.LocalRotationZ, 0, lerpProgress), Vector3.forward);

        for (int j = 0; j < VERT_COUNT; ++j)
        {
            int vertexIndex = vertexIndexStart + j;
            Vector3 currentPosition = aOriginalVertexData.Positions[j];

            //world to local
            GfcTools.Minus(ref currentPosition, center);
            currentPosition = undeRotation * currentPosition;

            //apply local transform
            currentPosition = localRotation * currentPosition;
            GfcTools.Add(ref currentPosition, localPositionOffset);
            GfcTools.Mult(ref currentPosition, currentLocalScale);

            //local to world
            currentPosition = applyRotation * currentPosition;
            GfcTools.Add(ref currentPosition, center);

            //apply global transform
            GfcTools.Add(ref currentPosition, globalPositionOffset);
            GfcTools.Mult(ref currentPosition, currentGlobalScale);

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

        var textInfo = m_textMeshPro.textInfo;
        var charactersInfo = textInfo.characterInfo;
        int characterCount = textInfo.characterCount;

        int indexSign = (aFinalIndex - aStartIndex).Sign();

        aFinalIndex.MinSelf(characterCount - 1);

        m_vertexData.Clear();

        //todo make the routine work with aStartIndex bigger than aFinalIndex and write backwards
        TMP_VertexDataUpdateFlags vertexUpdateFlag = TMP_VertexDataUpdateFlags.Colors32;

        for (int i = aStartIndex; i * indexSign <= aFinalIndex * indexSign; i += indexSign)
        {
            if (ValidCharacter(charactersInfo[i].character))
            {
                int vertexIndexStart = charactersInfo[i].vertexIndex;
                int materialIndex = charactersInfo[i].materialReferenceIndex;

                Color32[] vertColors = textInfo.meshInfo[materialIndex].colors32;
                Vector3[] vertPositions = textInfo.meshInfo[materialIndex].vertices;

                VertexData data = default;
                for (int j = 0; j < VERT_COUNT; ++j)
                {
                    int vertexIndex = vertexIndexStart + j;
                    data.Colors[j] = vertColors[vertexIndex];
                    data.Positions[j] = vertPositions[vertexIndex];

                    if (aFadeIn)
                        vertColors[vertexIndex].a = 0;
                }

                m_vertexData.Add(data);
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

                    if (auxSecondsSinceStart <= 0)
                        break;

                    float timeCoef = (auxSecondsSinceStart * invLetterTranslateSeconds * m_writeAllSpeedMultiplier).Min(1);
                    float timeCoefAux = aFadeIn ? timeCoef : 1.0f - timeCoef;

                    vertexUpdateFlag |= ApplyEffect(aFadeIn, timeCoefAux, validCharacterIndex, textInfo, charInfo, m_vertexData[validCharacterIndex]);

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

            m_textMeshPro.UpdateVertexData(vertexUpdateFlag);
            vertexUpdateFlag = TMP_VertexDataUpdateFlags.None;

            float timeOfWaitStart = Time.time;
            yield return Timing.WaitForOneFrame;

            float elapsedSeconds = Time.time - timeOfWaitStart;
            if (m_writeAllOnce) m_secondsSinceWriteAll += elapsedSeconds;
            secondsSinceStart += elapsedSeconds;
        }

        m_textTransitionHandle.Finished();
    }

    /*
   private static bool TagIsValid(string aString, int aStartIndex, int aEndIndex)
   {
       bool valid = aStartIndex >= 0 && aEndIndex >= 0
                   && aEndIndex > aStartIndex
                   && aEndIndex - aStartIndex >= 1;

       if (valid)
       {
           //tags do not contain spaces, we will abuse that to detect them fast
           int firstSpace = aString.IndexOf(' ', aStartIndex);
           valid &= firstSpace == -1 || firstSpace > aEndIndex;
       }

       //todo, check tags

       return valid;
   }

   private void IncrementAndSkipTags(GfcStringBuffer aBuffer, int aIncrement, ref int aCurrentCharacterIndex, ref int aTagsCharCount, string aStringToAddAfterTags = null)
   {
       int tagRowCount = 0;
       IncrementAndSkipTags(aBuffer, aIncrement, ref aCurrentCharacterIndex, ref tagRowCount, ref aTagsCharCount, aStringToAddAfterTags);
   }

   private void IncrementAndSkipTags(GfcStringBuffer aBuffer, int aIncrement, int aCurrentCharacterIndex, string aStringToAddAfterTags = null)
   {
       int tagRowCount = 0, tagLength = 0;
       IncrementAndSkipTags(aBuffer, aIncrement, ref aCurrentCharacterIndex, ref tagRowCount, ref tagLength, aStringToAddAfterTags);
   }

   private void IncrementAndSkipTags(GfcStringBuffer aBuffer, int aIncrement, ref int aCurrentCharacterIndex, ref int aTagRowCount, ref int aTagsCharCount, string aStringToAddAfterTags = null)
   {
       aIncrement.MinSelf(aBuffer.Length - aCurrentCharacterIndex);

       if (aIncrement > 0)
       {
           int desiredIndex = aCurrentCharacterIndex + aIncrement;

           //we use the assumption that m_currentCharacterIndex will never be inside of a tag
           //if this function works correctly, this assumption will always be true
           int firstRightArrow = aBuffer.StringBuffer.IndexOf('>', aCurrentCharacterIndex);
           int firstLeftArrow = aBuffer.StringBuffer.IndexOf('<', aCurrentCharacterIndex);

           bool tagFound = TagIsValid(aBuffer, firstLeftArrow, firstRightArrow);

           if (aTagRowCount > 0
               && !aStringToAddAfterTags.IsEmpty()
               && (!tagFound || aCurrentCharacterIndex < firstLeftArrow))
           {
               aBuffer.Insert(ref aCurrentCharacterIndex, aStringToAddAfterTags);

               desiredIndex += aStringToAddAfterTags.Length;
               firstRightArrow += aStringToAddAfterTags.Length;
               firstLeftArrow += aStringToAddAfterTags.Length;

               aTagRowCount = 0;
           }

           int newIndex = desiredIndex;
           int ignoredTextLength = 0;
           if (tagFound && desiredIndex >= firstLeftArrow)
           {
               aTagRowCount++;
               firstRightArrow++;
               //Debug.Log("I found a tag!!" + aTagRowCount + " " + aBuffer.StringBuffer.Substring(firstLeftArrow, firstRightArrow - firstLeftArrow));
               newIndex = firstRightArrow;
               ignoredTextLength = firstRightArrow - firstLeftArrow;
               aTagsCharCount += ignoredTextLength;
           }

           for (int i = aCurrentCharacterIndex; i < newIndex; ++i)
               if (!ValidCharacter(aBuffer[i]))
                   ignoredTextLength++;

           aCurrentCharacterIndex = newIndex;
           IncrementAndSkipTags(aBuffer, ignoredTextLength + desiredIndex - newIndex, ref aCurrentCharacterIndex, ref aTagRowCount, aStringToAddAfterTags);
       }
   }

   */
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
    public Vector4<Vector3> Positions;
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






