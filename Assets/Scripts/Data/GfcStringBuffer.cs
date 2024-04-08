using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;

using static Unity.Mathematics.math;

public class GfcStringBuffer
{
    protected string m_stringBuffer;

    protected int m_stringBufferCapacity;

    public string StringBuffer { get { return m_stringBuffer; } }

    public int Length { get { return m_stringBuffer.Length; } }

    public int Capacity { get { return m_stringBufferCapacity; } }

    public GfcStringBuffer()
    {
        m_stringBufferCapacity = 0;
        m_stringBuffer = new('F', m_stringBufferCapacity);
        Clear();
    }

    public GfcStringBuffer(int aLength)
    {
        m_stringBufferCapacity = aLength;
        m_stringBuffer = new('F', m_stringBufferCapacity);
        Clear();
    }

    public GfcStringBuffer(string aBufferString)
    {
        m_stringBufferCapacity = aBufferString.Length;
        m_stringBuffer = aBufferString;
    }

    public unsafe char this[int key]
    {
        get => m_stringBuffer[key];
        set => Insert(ref key, value);
    }

    public static implicit operator string(GfcStringBuffer d) => d.m_stringBuffer;

    public GfcStringBuffer Concatenate(string aString)
    {
        int insertPosition = Length;
        Insert(ref insertPosition, aString);
        return this;
    }

    public GfcStringBuffer Concatenate(char aChar)
    {
        int insertPosition = Length;
        Insert(ref insertPosition, aChar);
        return this;
    }

    public GfcStringBuffer Concatenate(long aLong)
    {
        int insertPosition = Length;
        Insert(ref insertPosition, aLong);
        return this;
    }

    public GfcStringBuffer Concatenate(ulong aUlong)
    {
        int insertPosition = Length;
        Insert(ref insertPosition, aUlong);
        return this;
    }

    public GfcStringBuffer Concatenate(double aDouble, int aPrecission, char aDecimalPointSymbol = '.')
    {
        int insertPosition = Length;
        Insert(ref insertPosition, aDouble, aPrecission, aDecimalPointSymbol);
        return this;
    }

    public unsafe void CopySubstringTo(int aWritePosition, int aSubstringPosition, int aSubstringLength)
    {
        SetMinimumLength(aWritePosition + aSubstringLength);

        fixed (char* stringPtr = m_stringBuffer)
            for (int i = 0; i < aSubstringLength; i++)
                stringPtr[i + aWritePosition] = m_stringBuffer[i + aSubstringPosition];
    }

    public void CutToSubstring(int aSubstringPosition, int aSubstringLength)
    {
        CopySubstringTo(0, aSubstringPosition, aSubstringLength);
        SetLength(aSubstringLength);
    }

    public void SetMinimumLength(int aMinimumLength) { SetMinimumLength(ref m_stringBuffer, ref m_stringBufferCapacity, aMinimumLength); }

    public void SetLength(int aStringLength, bool aKeepData = true) { ResizeStringBufferLength(ref m_stringBuffer, ref m_stringBufferCapacity, aStringLength, aKeepData, true); }

    public void Clear() { SetLength(0); }

    public void TrimExcess()
    {
        int desiredLength = m_stringBuffer.Length;
        if (desiredLength != m_stringBufferCapacity)
        {
            m_stringBuffer = m_stringBuffer.Substring(0, desiredLength);
            m_stringBufferCapacity = desiredLength;
        }
    }

    public static unsafe void InvertString(string aString)
    {
        int lastIndex = aString.Length - 1;
        int halfWayPoint = (lastIndex + 1) >> 1;
        fixed (char* stringPtr = aString)
            for (int i = 0; i < halfWayPoint; i++)
                stringPtr[i] = stringPtr[lastIndex - i];
    }

    public string GetStringClone() { return string.Copy(m_stringBuffer); }

    public void Insert(ref int aCurrentPositionInBuffer, string aInsertString) { InsertInStringBufferExpand(ref m_stringBuffer, ref m_stringBufferCapacity, ref aCurrentPositionInBuffer, aInsertString, true); }

    public void Insert(ref int aCurrentPositionInBuffer, char aInsertChar) { InsertInStringBufferExpand(ref m_stringBuffer, ref m_stringBufferCapacity, ref aCurrentPositionInBuffer, aInsertChar, true); }

    public void Insert(ref int aCurrentPositionInBuffer, long aInsertLong) { InsertInStringBufferExpand(ref m_stringBuffer, ref m_stringBufferCapacity, ref aCurrentPositionInBuffer, aInsertLong, true); }

    public void Insert(ref int aCurrentPositionInBuffer, ulong aInsertUlong) { InsertInStringBufferExpand(ref m_stringBuffer, ref m_stringBufferCapacity, ref aCurrentPositionInBuffer, aInsertUlong, true); }

    public void Insert(ref int aCurrentPositionInBuffer, double aInsertDouble, int aPrecission, char aDecimalPointSymbol = '.') { InsertInStringBufferExpand(ref m_stringBuffer, ref m_stringBufferCapacity, ref aCurrentPositionInBuffer, aInsertDouble, aPrecission, aDecimalPointSymbol, true); }

    public static unsafe void ExpandStringBuffer(ref string aStringBuffer, ref int aStringBufferLength, int aRequiredLength, bool aKeepData, bool aMultipleOfTwoIfExpand = false)
    {
        if (aStringBufferLength < aRequiredLength)
        {
            int newStringBufferLength;

            if (aMultipleOfTwoIfExpand)
            {
                newStringBufferLength = 2;
                while ((newStringBufferLength - 1) < aRequiredLength)
                    newStringBufferLength <<= 1;

                newStringBufferLength--;
            }
            else
                newStringBufferLength = aRequiredLength;

            string newStringBuffer = new('F', newStringBufferLength);

            if (aKeepData)
                fixed (char* stringPtr = newStringBuffer)
                fixed (char* oldStringPtr = aStringBuffer)
                    for (int i = 0; i < aStringBufferLength; ++i)
                        stringPtr[i] = oldStringPtr[i];

            SetStringLength(newStringBuffer, aStringBuffer.Length);
            aStringBufferLength = newStringBufferLength;
            aStringBuffer = newStringBuffer;
        }
    }

    public static void SetMinimumLength(ref string aStringBuffer, ref int aStringBufferLength, int aMinimumLength) { if (aMinimumLength > aStringBuffer.Length) ResizeStringBufferLength(ref aStringBuffer, ref aStringBufferLength, aMinimumLength); }

    public static unsafe void SetStringLength(string aString, int aLength)
    {
        fixed (char* stringPtr = aString)
        {
            //the length of a string is encoded right before the string is defined in memory. Thanks Chris Fulstow on StackOverflow
            int* intPtr = (int*)stringPtr;
            intPtr[-1] = aLength;
            stringPtr[aLength] = '\0';
        }
    }

    public static unsafe void ResizeStringBufferLength(ref string aStringBuffer, ref int aStringBufferLength, int aRequiredLength, bool aKeepData = true, bool aMultipleOfTwoIfExpand = false)
    {
        ExpandStringBuffer(ref aStringBuffer, ref aStringBufferLength, aRequiredLength, aKeepData, aMultipleOfTwoIfExpand);
        SetStringLength(aStringBuffer, aRequiredLength);
    }

    public static unsafe void InsertInStringBufferExpand(ref string aStringBuffer, ref int aStringBufferLength, ref int aCurrentPositionInBuffer, string aInsertString, bool aMultipleOfTwoIfExpand = false)
    {
        ResizeStringBufferLength(ref aStringBuffer, ref aStringBufferLength, aCurrentPositionInBuffer + aInsertString.Length, true, aMultipleOfTwoIfExpand);
        InsertInStringBuffer(aStringBuffer, ref aCurrentPositionInBuffer, aInsertString);
    }

    public static unsafe void InsertInStringBufferExpand(ref string aStringBuffer, ref int aStringBufferLength, ref int aCurrentPositionInBuffer, char aInsertChar, bool aMultipleOfTwoIfExpand = false)
    {
        ExpandStringBuffer(ref aStringBuffer, ref aStringBufferLength, aCurrentPositionInBuffer + 1, true, aMultipleOfTwoIfExpand);
        InsertInStringBuffer(aStringBuffer, ref aCurrentPositionInBuffer, aInsertChar);
        SetMinimumLength(ref aStringBuffer, ref aStringBufferLength, aCurrentPositionInBuffer);
    }

    public static unsafe void InsertInStringBufferExpand(ref string aStringBuffer, ref int aStringBufferLength, ref int aCurrentPositionInBuffer, double aInsertDouble, int aPrecission, char aDecimalPointSymbol = '.', bool aMultipleOfTwoIfExpand = false)
    {
        aPrecission = min(aPrecission, 17); //doesn't work if [aPrecission] is more than 17. Double precission doesn't go over the 16th digit so this should be fine
        double absInsertDouble = abs(aInsertDouble);
        long wholeNumber = (long)absInsertDouble;
        long decicmalPart = (long)(pow(10.0, aPrecission) * (absInsertDouble - wholeNumber));

        if (aInsertDouble < 0)
            InsertInStringBufferExpand(ref aStringBuffer, ref aStringBufferLength, ref aCurrentPositionInBuffer, '-', aMultipleOfTwoIfExpand);

        InsertInStringBufferExpand(ref aStringBuffer, ref aStringBufferLength, ref aCurrentPositionInBuffer, wholeNumber, aMultipleOfTwoIfExpand);
        InsertInStringBufferExpand(ref aStringBuffer, ref aStringBufferLength, ref aCurrentPositionInBuffer, aDecimalPointSymbol, aMultipleOfTwoIfExpand);
        InsertInStringBufferExpand(ref aStringBuffer, ref aStringBufferLength, ref aCurrentPositionInBuffer, decicmalPart, aMultipleOfTwoIfExpand);
    }

    public static unsafe void InsertInStringBufferExpand(ref string aStringBuffer, ref int aStringBufferLength, ref int aCurrentPositionInBuffer, long aInsertLong, bool aDoubleInSizeIfExpand = false)
    {
        long auxInsertLong = aInsertLong;
        int countCharacters = auxInsertLong == 0 ? 1 : 0;
        while (auxInsertLong != 0)
        {
            countCharacters++;
            auxInsertLong /= 10;
        }

        if (aInsertLong < 0)
            countCharacters++;

        ExpandStringBuffer(ref aStringBuffer, ref aStringBufferLength, aCurrentPositionInBuffer + countCharacters, true, aDoubleInSizeIfExpand);
        InsertLongInStringBufferInternal(aStringBuffer, ref aCurrentPositionInBuffer, aInsertLong, countCharacters);
        SetMinimumLength(ref aStringBuffer, ref aStringBufferLength, aCurrentPositionInBuffer);
    }

    public static unsafe void InsertInStringBufferExpand(ref string aStringBuffer, ref int aStringBufferLength, ref int aCurrentPositionInBuffer, ulong aInsertUlong, bool aDoubleInSizeIfExpand = false)
    {
        ulong auxInsertLong = aInsertUlong;
        int countCharacters = auxInsertLong == 0 ? 1 : 0;
        while (auxInsertLong != 0)
        {
            countCharacters++;
            auxInsertLong /= 10;
        }

        ExpandStringBuffer(ref aStringBuffer, ref aStringBufferLength, aCurrentPositionInBuffer + countCharacters, true, aDoubleInSizeIfExpand);
        InsertLongInStringBufferInternal(aStringBuffer, ref aCurrentPositionInBuffer, aInsertUlong, countCharacters);
        SetMinimumLength(ref aStringBuffer, ref aStringBufferLength, aCurrentPositionInBuffer);
    }

    public static unsafe void InsertInStringBuffer(string aStringBuffer, ref int aCurrentPositionInBuffer, string aInsertString)
    {
        int insertStringLength = aInsertString.Length;

        fixed (char* stringPtr = aStringBuffer)
            for (int i = 0; i < insertStringLength; ++i)
                stringPtr[aCurrentPositionInBuffer + i] = aInsertString[i];

        aCurrentPositionInBuffer += insertStringLength;
    }

    public static unsafe void InsertInStringBuffer(string aStringBuffer, ref int aCurrentPositionInBuffer, char aInsertChar)
    {
        fixed (char* stringPtr = aStringBuffer)
            stringPtr[aCurrentPositionInBuffer] = aInsertChar;

        ++aCurrentPositionInBuffer;
    }

    protected static unsafe void InsertLongInStringBufferInternal(string aStringBuffer, ref int aCurrentPositionInBuffer, long aInsertLong, int aInsertLongLength)
    {
        fixed (char* stringPtr = aStringBuffer)
        {
            long auxNumber = abs(aInsertLong);
            int currentCharIndex = aCurrentPositionInBuffer + aInsertLongLength - 1;

            do
            {
                stringPtr[currentCharIndex] = (char)(auxNumber % 10 + 48);
                --currentCharIndex;
                auxNumber /= 10;
            } while (auxNumber != 0);

            if (aInsertLong < 0)
                stringPtr[currentCharIndex] = '-';
        }

        aCurrentPositionInBuffer += aInsertLongLength;
    }

    protected static unsafe void InsertLongInStringBufferInternal(string aStringBuffer, ref int aCurrentPositionInBuffer, ulong aInsertUlong, int aInsertLongLength)
    {
        fixed (char* stringPtr = aStringBuffer)
        {
            ulong auxNumber = aInsertUlong;
            int currentCharIndex = aCurrentPositionInBuffer + aInsertLongLength - 1;

            do
            {
                stringPtr[currentCharIndex] = (char)(auxNumber % 10 + 48);
                --currentCharIndex;
                auxNumber /= 10;
            } while (auxNumber != 0);
        }

        aCurrentPositionInBuffer += aInsertLongLength;
    }
}
