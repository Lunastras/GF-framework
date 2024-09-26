using UnityEngine;
using static Unity.Mathematics.math;

//A memory allocation free string buffer. This is similar to c#'s StringBuilder, but StringBuilder does not allow you to directly access the memory buffer and use it, you need to create a new 
//string when you want to use it. GfcStringBuffer allows users to directly use the current string built without creating a new one. Because of this, users need to be careful when using the buffer 
//because the string can always change dynamically.
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
        set => Write(ref key, value);
    }

    public static implicit operator string(GfcStringBuffer d) => d.m_stringBuffer;

    public GfcStringBuffer Append(string aString)
    {
        int insertPosition = Length;
        Write(ref insertPosition, aString);
        return this;
    }

    public GfcStringBuffer Append(char aChar)
    {
        int insertPosition = Length;
        Write(ref insertPosition, aChar);
        return this;
    }

    public GfcStringBuffer Append(long aLong)
    {
        int insertPosition = Length;
        Write(ref insertPosition, aLong);
        return this;
    }

    public GfcStringBuffer Append(ulong aUlong)
    {
        int insertPosition = Length;
        Write(ref insertPosition, aUlong);
        return this;
    }

    public GfcStringBuffer Append(double aDouble, int aPrecission, char aDecimalPointSymbol = '.')
    {
        int insertPosition = Length;
        Write(ref insertPosition, aDouble, aPrecission, aDecimalPointSymbol);
        return this;
    }

    public unsafe void CopySubstringTo(int aWritePosition, int aSubstringPosition, int aSubstringLength) { CopySubstringTo(ref m_stringBuffer, ref m_stringBufferCapacity, aWritePosition, aSubstringPosition, aSubstringLength, true); }

    public void CutToSubstring(int aSubstringPosition, int aSubstringLength)
    {
        CopySubstringTo(0, aSubstringPosition, aSubstringLength);
        SetLength(aSubstringLength);
    }

    public void CutSubstring(int aSubstringPosition, int aSubstringLength)
    {
        CopySubstringTo(aSubstringPosition, aSubstringPosition + aSubstringLength, Length - aSubstringPosition + aSubstringLength);
        SetLength(Length - aSubstringLength);
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

    public string GetStringCopy() { return string.Copy(m_stringBuffer); }

    public void Insert(ref int aCurrentPositionInBuffer, string aInsertString) { InsertInStringBufferExpand(ref m_stringBuffer, ref m_stringBufferCapacity, ref aCurrentPositionInBuffer, aInsertString, true); }

    public void Insert(ref int aCurrentPositionInBuffer, char aInsertChar) { InsertInStringBufferExpand(ref m_stringBuffer, ref m_stringBufferCapacity, ref aCurrentPositionInBuffer, aInsertChar, true); }

    public void Insert(ref int aCurrentPositionInBuffer, long aInsertLong) { InsertInStringBufferExpand(ref m_stringBuffer, ref m_stringBufferCapacity, ref aCurrentPositionInBuffer, aInsertLong, true); }

    public void Insert(ref int aCurrentPositionInBuffer, ulong aInsertUlong) { InsertInStringBufferExpand(ref m_stringBuffer, ref m_stringBufferCapacity, ref aCurrentPositionInBuffer, aInsertUlong, true); }

    public void Insert(ref int aCurrentPositionInBuffer, double aInsertDouble, int aPrecission, char aDecimalPointSymbol = '.') { InsertInStringBufferExpand(ref m_stringBuffer, ref m_stringBufferCapacity, ref aCurrentPositionInBuffer, aInsertDouble, aPrecission, aDecimalPointSymbol, true); }


    public void Insert(int aCurrentPositionInBuffer, string aInsertString) { InsertInStringBufferExpand(ref m_stringBuffer, ref m_stringBufferCapacity, ref aCurrentPositionInBuffer, aInsertString, true); }

    public void Insert(int aCurrentPositionInBuffer, char aInsertChar) { InsertInStringBufferExpand(ref m_stringBuffer, ref m_stringBufferCapacity, ref aCurrentPositionInBuffer, aInsertChar, true); }

    public void Insert(int aCurrentPositionInBuffer, long aInsertLong) { InsertInStringBufferExpand(ref m_stringBuffer, ref m_stringBufferCapacity, ref aCurrentPositionInBuffer, aInsertLong, true); }

    public void Insert(int aCurrentPositionInBuffer, ulong aInsertUlong) { InsertInStringBufferExpand(ref m_stringBuffer, ref m_stringBufferCapacity, ref aCurrentPositionInBuffer, aInsertUlong, true); }

    public void Insert(int aCurrentPositionInBuffer, double aInsertDouble, int aPrecission, char aDecimalPointSymbol = '.') { InsertInStringBufferExpand(ref m_stringBuffer, ref m_stringBufferCapacity, ref aCurrentPositionInBuffer, aInsertDouble, aPrecission, aDecimalPointSymbol, true); }


    public void Write(ref int aCurrentPositionInBuffer, string aInsertString) { WriteInStringBufferExpand(ref m_stringBuffer, ref m_stringBufferCapacity, ref aCurrentPositionInBuffer, aInsertString, true); }

    public void Write(ref int aCurrentPositionInBuffer, char aInsertChar) { WriteInStringBufferExpand(ref m_stringBuffer, ref m_stringBufferCapacity, ref aCurrentPositionInBuffer, aInsertChar, true); }

    public void Write(ref int aCurrentPositionInBuffer, long aInsertLong) { WriteInStringBufferExpand(ref m_stringBuffer, ref m_stringBufferCapacity, ref aCurrentPositionInBuffer, aInsertLong, true); }

    public void Write(ref int aCurrentPositionInBuffer, ulong aInsertUlong) { WriteInStringBufferExpand(ref m_stringBuffer, ref m_stringBufferCapacity, ref aCurrentPositionInBuffer, aInsertUlong, true); }

    public void Write(ref int aCurrentPositionInBuffer, double aInsertDouble, int aPrecission, char aDecimalPointSymbol = '.') { WriteInStringBufferExpand(ref m_stringBuffer, ref m_stringBufferCapacity, ref aCurrentPositionInBuffer, aInsertDouble, aPrecission, aDecimalPointSymbol, true); }

    public void Write(int aCurrentPositionInBuffer, string aInsertString) { WriteInStringBufferExpand(ref m_stringBuffer, ref m_stringBufferCapacity, ref aCurrentPositionInBuffer, aInsertString, true); }

    public void Write(int aCurrentPositionInBuffer, char aInsertChar) { WriteInStringBufferExpand(ref m_stringBuffer, ref m_stringBufferCapacity, ref aCurrentPositionInBuffer, aInsertChar, true); }

    public void Write(int aCurrentPositionInBuffer, long aInsertLong) { WriteInStringBufferExpand(ref m_stringBuffer, ref m_stringBufferCapacity, ref aCurrentPositionInBuffer, aInsertLong, true); }

    public void Write(int aCurrentPositionInBuffer, ulong aInsertUlong) { WriteInStringBufferExpand(ref m_stringBuffer, ref m_stringBufferCapacity, ref aCurrentPositionInBuffer, aInsertUlong, true); }

    public void Write(int aCurrentPositionInBuffer, double aInsertDouble, int aPrecission, char aDecimalPointSymbol = '.') { WriteInStringBufferExpand(ref m_stringBuffer, ref m_stringBufferCapacity, ref aCurrentPositionInBuffer, aInsertDouble, aPrecission, aDecimalPointSymbol, true); }

    public static unsafe void ExpandStringBuffer(ref string aStringBuffer, ref int aStringBufferCapacity, int aRequiredLength, bool aKeepData, bool aMultipleOfTwoIfExpand = false)
    {
        if (aStringBufferCapacity < aRequiredLength)
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
                    for (int i = 0; i < aStringBufferCapacity; ++i)
                        stringPtr[i] = oldStringPtr[i];

            SetStringLength(newStringBuffer, aStringBuffer.Length);
            aStringBufferCapacity = newStringBufferLength;
            aStringBuffer = newStringBuffer;
        }
    }

    public static void SetMinimumLength(ref string aStringBuffer, ref int aStringBufferCapacity, int aMinimumLength) { if (aMinimumLength > aStringBuffer.Length) ResizeStringBufferLength(ref aStringBuffer, ref aStringBufferCapacity, aMinimumLength); }

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

    public static unsafe void OffsetData(ref string aStringBuffer, ref int aStringBufferCapacity, int aOffsetStart, int aOffset, bool aMultipleOfTwoIfExpand = false)
    {
        CopySubstringTo(ref aStringBuffer, ref aStringBufferCapacity, aOffsetStart + aOffset, aOffsetStart, aStringBufferCapacity - aOffsetStart, aMultipleOfTwoIfExpand);
    }

    public static unsafe void CopySubstringTo(ref string aStringBuffer, ref int aStringBufferCapacity, int aWritePosition, int aSubstringPosition, int aSubstringLength, bool aMultipleOfTwoIfExpand = false)
    {
        Debug.Assert(aSubstringPosition + aSubstringLength <= aStringBufferCapacity);
        SetMinimumLength(ref aStringBuffer, ref aStringBufferCapacity, aWritePosition + aSubstringLength);

        fixed (char* stringPtr = aStringBuffer)
        {//todo/ not sure if functional
            if (aSubstringPosition < aWritePosition)
            {
                for (int i = aSubstringLength - 1; i >= 0; i--)
                    stringPtr[i + aWritePosition] = stringPtr[i + aSubstringPosition];
            }
            else
            {
                for (int i = 0; i < aSubstringLength; i++)
                    stringPtr[i + aWritePosition] = stringPtr[i + aSubstringPosition];
            }
        }
    }

    public static unsafe void ResizeStringBufferLength(ref string aStringBuffer, ref int aStringBufferCapacity, int aRequiredLength, bool aKeepData = true, bool aMultipleOfTwoIfExpand = false)
    {
        ExpandStringBuffer(ref aStringBuffer, ref aStringBufferCapacity, aRequiredLength, aKeepData, aMultipleOfTwoIfExpand);
        SetStringLength(aStringBuffer, aRequiredLength);
    }

    public static unsafe void WriteInStringBufferExpand(ref string aStringBuffer, ref int aStringBufferCapacity, ref int aCurrentPositionInBuffer, string aInsertString, bool aMultipleOfTwoIfExpand = false)
    {
        SetMinimumLength(ref aStringBuffer, ref aStringBufferCapacity, aCurrentPositionInBuffer + aInsertString.Length);
        WriteInStringBuffer(aStringBuffer, ref aCurrentPositionInBuffer, aInsertString);
    }

    public static unsafe void WriteInStringBufferExpand(ref string aStringBuffer, ref int aStringBufferCapacity, ref int aCurrentPositionInBuffer, char aInsertChar, bool aMultipleOfTwoIfExpand = false)
    {
        ExpandStringBuffer(ref aStringBuffer, ref aStringBufferCapacity, aCurrentPositionInBuffer + 1, true, aMultipleOfTwoIfExpand);
        WriteInStringBuffer(aStringBuffer, ref aCurrentPositionInBuffer, aInsertChar);
        SetMinimumLength(ref aStringBuffer, ref aStringBufferCapacity, aCurrentPositionInBuffer);
    }

    public static unsafe void WriteInStringBufferExpand(ref string aStringBuffer, ref int aStringBufferCapacity, ref int aCurrentPositionInBuffer, double aInsertDouble, int aPrecission, char aDecimalPointSymbol = '.', bool aMultipleOfTwoIfExpand = false)
    {
        aPrecission = min(aPrecission, 17); //doesn't work if [aPrecission] is more than 17. Double precission doesn't go over the 16th digit so this should be fine
        double absInsertDouble = abs(aInsertDouble);
        long wholeNumber = (long)absInsertDouble;
        long decicmalPart = (long)(pow(10.0, aPrecission) * (absInsertDouble - wholeNumber));

        if (aInsertDouble < 0)
            WriteInStringBufferExpand(ref aStringBuffer, ref aStringBufferCapacity, ref aCurrentPositionInBuffer, '-', aMultipleOfTwoIfExpand);

        WriteInStringBufferExpand(ref aStringBuffer, ref aStringBufferCapacity, ref aCurrentPositionInBuffer, wholeNumber, aMultipleOfTwoIfExpand);
        if (aPrecission > 0)
        {
            WriteInStringBufferExpand(ref aStringBuffer, ref aStringBufferCapacity, ref aCurrentPositionInBuffer, aDecimalPointSymbol, aMultipleOfTwoIfExpand);
            WriteInStringBufferExpand(ref aStringBuffer, ref aStringBufferCapacity, ref aCurrentPositionInBuffer, decicmalPart, aMultipleOfTwoIfExpand);
        }
    }

    public static unsafe void WriteInStringBufferExpand(ref string aStringBuffer, ref int aStringBufferCapacity, ref int aCurrentPositionInBuffer, long aInsertLong, bool aDoubleInSizeIfExpand = false)
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

        ExpandStringBuffer(ref aStringBuffer, ref aStringBufferCapacity, aCurrentPositionInBuffer + countCharacters, true, aDoubleInSizeIfExpand);
        WriteLongInStringBufferInternal(aStringBuffer, ref aCurrentPositionInBuffer, aInsertLong, countCharacters);
        SetMinimumLength(ref aStringBuffer, ref aStringBufferCapacity, aCurrentPositionInBuffer);
    }

    public static unsafe void WriteInStringBufferExpand(ref string aStringBuffer, ref int aStringBufferCapacity, ref int aCurrentPositionInBuffer, ulong aInsertUlong, bool aDoubleInSizeIfExpand = false)
    {
        ulong auxInsertLong = aInsertUlong;
        int countCharacters = auxInsertLong == 0 ? 1 : 0;
        while (auxInsertLong != 0)
        {
            countCharacters++;
            auxInsertLong /= 10;
        }

        ExpandStringBuffer(ref aStringBuffer, ref aStringBufferCapacity, aCurrentPositionInBuffer + countCharacters, true, aDoubleInSizeIfExpand);
        WriteLongInStringBufferInternal(aStringBuffer, ref aCurrentPositionInBuffer, aInsertUlong, countCharacters);
        SetMinimumLength(ref aStringBuffer, ref aStringBufferCapacity, aCurrentPositionInBuffer);
    }

    public static unsafe void WriteInStringBuffer(string aStringBuffer, ref int aCurrentPositionInBuffer, string aInsertString)
    {
        int insertStringLength = aInsertString.Length;

        fixed (char* stringPtr = aStringBuffer)
            for (int i = 0; i < insertStringLength; ++i)
                stringPtr[aCurrentPositionInBuffer + i] = aInsertString[i];

        aCurrentPositionInBuffer += insertStringLength;
    }

    public static unsafe void WriteInStringBuffer(string aStringBuffer, ref int aCurrentPositionInBuffer, char aInsertChar)
    {
        fixed (char* stringPtr = aStringBuffer)
            stringPtr[aCurrentPositionInBuffer] = aInsertChar;

        ++aCurrentPositionInBuffer;
    }

    protected static unsafe void WriteLongInStringBufferInternal(string aStringBuffer, ref int aCurrentPositionInBuffer, long aInsertLong, int aInsertLongLength)
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

    protected static unsafe void WriteLongInStringBufferInternal(string aStringBuffer, ref int aCurrentPositionInBuffer, ulong aInsertUlong, int aInsertLongLength)
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

    //todo insert properly
    public static unsafe void InsertInStringBufferExpand(ref string aStringBuffer, ref int aStringBufferCapacity, ref int aCurrentPositionInBuffer, string aInsertString, bool aMultipleOfTwoIfExpand = false)
    {
        OffsetData(ref aStringBuffer, ref aStringBufferCapacity, aCurrentPositionInBuffer, aInsertString.Length, aMultipleOfTwoIfExpand);
        WriteInStringBuffer(aStringBuffer, ref aCurrentPositionInBuffer, aInsertString);
    }

    public static unsafe void InsertInStringBufferExpand(ref string aStringBuffer, ref int aStringBufferCapacity, ref int aCurrentPositionInBuffer, char aInsertChar, bool aMultipleOfTwoIfExpand = false)
    {
        OffsetData(ref aStringBuffer, ref aStringBufferCapacity, aCurrentPositionInBuffer, 1, aMultipleOfTwoIfExpand);
        WriteInStringBuffer(aStringBuffer, ref aCurrentPositionInBuffer, aInsertChar);
    }

    public static unsafe void InsertInStringBufferExpand(ref string aStringBuffer, ref int aStringBufferCapacity, ref int aCurrentPositionInBuffer, double aInsertDouble, int aPrecission, char aDecimalPointSymbol = '.', bool aMultipleOfTwoIfExpand = false)
    {
        aPrecission = min(aPrecission, 17); //doesn't work if [aPrecission] is more than 17. Double precission doesn't go over the 16th digit so this should be fine
        double absInsertDouble = abs(aInsertDouble);
        long wholeNumber = (long)absInsertDouble;
        long decicmalPart = (long)(pow(10.0, aPrecission) * (absInsertDouble - wholeNumber));

        if (aInsertDouble < 0)
            InsertInStringBufferExpand(ref aStringBuffer, ref aStringBufferCapacity, ref aCurrentPositionInBuffer, '-', aMultipleOfTwoIfExpand);

        InsertInStringBufferExpand(ref aStringBuffer, ref aStringBufferCapacity, ref aCurrentPositionInBuffer, wholeNumber, aMultipleOfTwoIfExpand);
        InsertInStringBufferExpand(ref aStringBuffer, ref aStringBufferCapacity, ref aCurrentPositionInBuffer, aDecimalPointSymbol, aMultipleOfTwoIfExpand);
        InsertInStringBufferExpand(ref aStringBuffer, ref aStringBufferCapacity, ref aCurrentPositionInBuffer, decicmalPart, aMultipleOfTwoIfExpand);
    }

    public static unsafe void InsertInStringBufferExpand(ref string aStringBuffer, ref int aStringBufferCapacity, ref int aCurrentPositionInBuffer, long aInsertLong, bool aDoubleInSizeIfExpand = false)
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

        OffsetData(ref aStringBuffer, ref aStringBufferCapacity, aCurrentPositionInBuffer, countCharacters, aDoubleInSizeIfExpand);
        WriteLongInStringBufferInternal(aStringBuffer, ref aCurrentPositionInBuffer, aInsertLong, countCharacters);
    }

    public static unsafe void InsertInStringBufferExpand(ref string aStringBuffer, ref int aStringBufferCapacity, ref int aCurrentPositionInBuffer, ulong aInsertUlong, bool aDoubleInSizeIfExpand = false)
    {
        ulong auxInsertLong = aInsertUlong;
        int countCharacters = auxInsertLong == 0 ? 1 : 0;
        while (auxInsertLong != 0)
        {
            countCharacters++;
            auxInsertLong /= 10;
        }

        OffsetData(ref aStringBuffer, ref aStringBufferCapacity, aCurrentPositionInBuffer, countCharacters, aDoubleInSizeIfExpand);
        WriteLongInStringBufferInternal(aStringBuffer, ref aCurrentPositionInBuffer, aInsertUlong, countCharacters);
    }
}