using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MEC;
using TMPro;
using UnityEngine;


[Serializable]
public class GfxRichTextWriter
{
    public float LetterDelaySeconds = 0.1f;

    public bool IgnoreWhitespace = true;

    public TextMeshProUGUI m_textMeshPro;

    public AnimationCurve m_transitionCurve = new();

    private string m_string;

    private GfcStringBuffer m_stringBuffer = new(64);

    private GfcStringBuffer m_alphaStringBuffer = new(2);

    private const string TRANSPARENT_START = "<alpha=#00>";
    private const string TRANSPARENT_START_NO_VALUE = "<alpha=#>";

    private const string COLOR_TAG = "<color=";
    private const string COLOR_TAG_CLOSE = "</color>";

    private int m_currentCharacterIndex = 0;

    private CoroutineHandle m_writeTextHandle = default;
    private CoroutineHandle m_textTransitionHandle = default;

    private bool m_writeTextCoroutineRunning = false;

    private bool m_textTransitionCoroutineRunning = false;

    public GfcStringBuffer GetCurrentString() { return m_stringBuffer; }

    public void SetString(string aString) { m_string = aString; }

    public string GetString() { return m_string; }

    public bool WritingText() { return m_writeTextCoroutineRunning || m_textTransitionCoroutineRunning; }
    public void IncrementAll()
    {
        IncrementCharacterIndex(true, m_string.Length);
        m_stringBuffer.Clear();
        m_stringBuffer.Concatenate(m_string);

        if (m_writeTextCoroutineRunning)
        {
            m_writeTextCoroutineRunning = false;
            Timing.KillCoroutines(m_writeTextHandle);
            m_writeTextHandle = default;
        }
    }

    private IEnumerator<float> _WriteText()
    {
        m_writeTextCoroutineRunning = true;
        while (m_currentCharacterIndex < m_string.Length)
            yield return Timing.WaitUntilDone(IncrementCharacterIndex());

        m_writeTextHandle = default;
        m_writeTextCoroutineRunning = false;
    }

    public CoroutineHandle WriteText(bool aForceRestart = false)
    {
        if (aForceRestart || !m_writeTextCoroutineRunning)
        {
            if (m_writeTextCoroutineRunning)
            {
                m_writeTextCoroutineRunning = false;
                Timing.KillCoroutines(m_writeTextHandle);
            }

            m_stringBuffer.Clear();
            m_currentCharacterIndex = 0;
            m_writeTextCoroutineRunning = true;

            m_textMeshPro.text = null;
            m_writeTextHandle = Timing.RunCoroutine(_WriteText());
        }

        return m_writeTextHandle;
    }

    public CoroutineHandle IncrementCharacterIndex(bool aExecuteTransition = true, int aIncrement = 1)
    {
        if (m_textTransitionCoroutineRunning)
        {
            Timing.KillCoroutines(m_textTransitionHandle);
            m_textTransitionHandle = default;
        }

        int initialIndex = m_currentCharacterIndex;

        m_stringBuffer.Clear();
        m_stringBuffer.Concatenate(m_string);

        IncrementAndSkipTags(m_stringBuffer, aIncrement, ref m_currentCharacterIndex);

        int auxPosition = m_currentCharacterIndex;
        m_stringBuffer.Insert(ref auxPosition, TRANSPARENT_START);
        IncrementAndSkipTags(m_stringBuffer, m_string.Length, auxPosition, TRANSPARENT_START);

        m_textTransitionCoroutineRunning = aExecuteTransition;

        if (aExecuteTransition)
            m_textTransitionHandle = Timing.RunCoroutine(_IncrementIndex(initialIndex));

        return m_textTransitionHandle;
    }

    private static void SetHexValue(GfcStringBuffer aBuffer, int aIndex, int aHexValue)
    {
        if (aHexValue < 10)
            aBuffer.Write(aIndex, aHexValue);
        else
            aBuffer.Write(aIndex, (char)('A' + (aHexValue - 10)));
    }

    private IEnumerator<float> _IncrementIndex(int aTransitionStartIndex)
    {

        float invDuration = LetterDelaySeconds < 0.00001f ? float.MaxValue : 1.0f / LetterDelaySeconds;
        float timeFactor = 0;

        m_alphaStringBuffer.Clear();
        m_alphaStringBuffer.Concatenate(TRANSPARENT_START);
        m_stringBuffer.Insert(aTransitionStartIndex, TRANSPARENT_START);

        const float EPSILON_ONE = 0.99999f;
        while (timeFactor < EPSILON_ONE)
        {

            //var textInfo = 
            timeFactor.MinSelf(1);

            int byteValue = (int)(m_transitionCurve.Evaluate(timeFactor) * 256.0f);
            SetHexValue(m_alphaStringBuffer, 8, byteValue >> 4); //divide by 16
            SetHexValue(m_alphaStringBuffer, 9, byteValue % 16);

            m_stringBuffer.Write(aTransitionStartIndex, m_alphaStringBuffer);

            m_textMeshPro.text = null;
            m_textMeshPro.text = m_stringBuffer;

            yield return Timing.WaitForOneFrame;
            timeFactor += invDuration * Time.deltaTime;
        }

        //delme
        if (false)
        {
            m_textMeshPro.text = null;
            m_textMeshPro.text = m_stringBuffer;

            yield return Timing.WaitForSeconds(LetterDelaySeconds);
        }

        m_textTransitionCoroutineRunning = false;
        m_textTransitionHandle = default;
    }

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

    private void IncrementAndSkipTags(GfcStringBuffer aBuffer, int aIncrement, int aCurrentCharacterIndex, string aStringToAddAfterTags = null)
    {
        int tagRowCount = 0;
        IncrementAndSkipTags(aBuffer, aIncrement, ref aCurrentCharacterIndex, ref tagRowCount, aStringToAddAfterTags);
    }

    private void IncrementAndSkipTags(GfcStringBuffer aBuffer, int aIncrement, ref int aCurrentCharacterIndex, string aStringToAddAfterTags = null)
    {
        int tagRowCount = 0;
        IncrementAndSkipTags(aBuffer, aIncrement, ref aCurrentCharacterIndex, ref tagRowCount, aStringToAddAfterTags);
    }

    private bool ValidCharacter(char aChar)
    {
        return !IgnoreWhitespace || (aChar != ' ' && aChar != '\n' && aChar != '\t'); //not an exhaustive list
    }

    private void IncrementAndSkipTags(GfcStringBuffer aBuffer, int aIncrement, ref int aCurrentCharacterIndex, ref int aTagRowCount, string aStringToAddAfterTags = null)
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
            }

            for (int i = aCurrentCharacterIndex; i < newIndex; ++i)
                if (!ValidCharacter(aBuffer[i]))
                    ignoredTextLength++;


            aCurrentCharacterIndex = newIndex;
            IncrementAndSkipTags(aBuffer, ignoredTextLength + desiredIndex - newIndex, ref aCurrentCharacterIndex, ref aTagRowCount, aStringToAddAfterTags);
        }
    }
}
