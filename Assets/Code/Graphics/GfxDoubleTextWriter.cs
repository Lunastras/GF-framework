using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using MEC;
using System;

public class GfxDoubleTextWriter : MonoBehaviour
{
    [SerializeField] private GfxRichTextWriter m_textWriterMain;

    private GfxRichTextWriter m_textWriter;

    private GfxRichTextWriter m_textEraser;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Assert(m_textWriterMain.TextMeshPro);
        Debug.Assert(m_textWriterMain.TextMeshPro.gameObject != gameObject);

        m_textWriterMain.RefreshValues();

        m_textWriterMain.RemoveText();
        m_textWriter = m_textWriterMain;

        GameObject secondTmpWriter = Instantiate(m_textWriterMain.TextMeshPro.gameObject);
        secondTmpWriter.transform.SetParent(m_textWriterMain.TextMeshPro.transform.parent, false);

        m_textEraser = new(m_textWriterMain);
        m_textEraser.TextMeshPro = secondTmpWriter.GetComponent<TextMeshProUGUI>();

        m_textEraser.RefreshValues();
    }

    public void WriteString(string aString, float aWriteSpeedMultiplier = 1, float aEraseSpeedMultiplier = 1, bool aSkipEraseAnimation = false)
    {
        WriteString(aString, out _, out _, aWriteSpeedMultiplier, aEraseSpeedMultiplier, aSkipEraseAnimation);
    }

    public void WriteString(string aString, out CoroutineHandle aWriterHandle, out CoroutineHandle aEraserHandle, float aWriteSpeedMultiplier = 1, float aEraseSpeedMultiplier = 1, bool aSkipEraseAnimation = false)
    {
        GfxRichTextWriter newTextWriter = m_textEraser;

        m_textEraser = m_textWriter;
        aEraserHandle = m_textEraser.EraseText(aEraseSpeedMultiplier);

        m_textWriter = newTextWriter;
        aWriterHandle = m_textWriter.WriteText(aString, aWriteSpeedMultiplier);

        m_textWriter.TextMeshPro.transform.SetAsFirstSibling();
    }

    public void FinishTranslation(float aFinishSpeedMultiplier = 1) { FinishTranslation(out _, out _, aFinishSpeedMultiplier); }

    public void FinishTranslation(out CoroutineHandle aWriterHandle, out CoroutineHandle aEraserHandle, float aFinishSpeedMultiplier = 1)
    {
        aWriterHandle = m_textWriter.FinishTranslation(aFinishSpeedMultiplier);
        aEraserHandle = m_textEraser.FinishTranslation(aFinishSpeedMultiplier);
    }

    public void ForceWriteText()
    {
        m_textWriter.WriteAllNoTransition();
        m_textEraser.RemoveText();
    }

    public void RemoveText()
    {
        m_textWriter.RemoveText();
        m_textEraser.RemoveText();
    }

    public bool WritingText()
    {
        return m_textWriter.WritingText() || m_textEraser.WritingText();
    }

    public CoroutineHandle WaitUntilTextFinishes(GfcInputType aSubmitInput = GfcInputType.NONE, GfcInputType aSkipInput = GfcInputType.RUN, float aFinishSpeedMultiplier = 1, bool aForceWriteOnSubmit = false)
    {
        return WaitUntilTextFinishes(new GfcInputTracker(aSubmitInput), aSkipInput, aFinishSpeedMultiplier, aForceWriteOnSubmit);
    }

    public CoroutineHandle WaitUntilTextFinishes(GfcInputTracker aSubmitInputTracker, GfcInputType aSkipInput = GfcInputType.RUN, float aFinishSpeedMultiplier = 1, bool aForceWriteOnSubmit = false)
    {
        return Timing.RunCoroutine(_WaitUntilTextFinishesInternal(aSubmitInputTracker, null, aSkipInput, aFinishSpeedMultiplier, aForceWriteOnSubmit));
    }

    public CoroutineHandle WaitUntilTextFinishes(GfcInputTrackerShared aSubmitInputTracker, GfcInputType aSkipInput = GfcInputType.RUN, float aFinishSpeedMultiplier = 1, bool aForceWriteOnSubmit = false)
    {
        return Timing.RunCoroutine(_WaitUntilTextFinishesInternal(default, aSubmitInputTracker, aSkipInput, aFinishSpeedMultiplier, aForceWriteOnSubmit));
    }

    protected IEnumerator<float> _WaitUntilTextFinishesInternal(GfcInputTracker aFallbackInputTracker, GfcInputTrackerShared aSubmitInputTrackerHeap, GfcInputType aSkipInput, float aFinishSpeedMultiplier, bool aForceWriteOnSubmit)
    {
        bool finishTranslationPressed = false;

        while (WritingText())
        {
            if (GfcInput.GetInput(aSkipInput))
            {
                ForceWriteText();
            }
            else if (aSubmitInputTrackerHeap.PressedSinceLastCheckFallback(ref aFallbackInputTracker, true))
            {
                if (!aForceWriteOnSubmit && !finishTranslationPressed)
                {
                    FinishTranslation(aFinishSpeedMultiplier);
                    finishTranslationPressed = true;
                }
                else //force write everything if submit was pressed again
                {
                    ForceWriteText();
                }
            }
            else
            {
                yield return Timing.WaitForOneFrame;
            }
        }

        yield break;
    }

    public CoroutineHandle EraseText(float aSpeedMultiplier = 1)
    {
        (m_textEraser, m_textWriter) = (m_textWriter, m_textEraser);
        m_textWriter.RemoveText();

        return m_textEraser.EraseText(aSpeedMultiplier);
    }
}
