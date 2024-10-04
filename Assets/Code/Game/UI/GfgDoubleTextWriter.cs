using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using MEC;
using System;

public class GfgDoubleTextWriter : MonoBehaviour
{
    [SerializeField] private GfxRichTextWriter m_textWriterMain;

    private GfxRichTextWriter m_textWriter;

    private GfxRichTextWriter m_textEraser;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Assert(m_textWriterMain);
        Debug.Assert(m_textWriterMain.TextMeshPro);
        Debug.Assert(m_textWriterMain.gameObject != gameObject);

        m_textWriterMain.RemoveText();
        m_textWriter = m_textWriterMain;

        m_textEraser = Instantiate(m_textWriterMain.gameObject).GetComponent<GfxRichTextWriter>();
        m_textEraser.transform.SetParent(m_textWriterMain.transform.parent, false);
    }

    public void WriteString(string aString, float aWriteSpeedMultiplier = 1, float aEraseSpeedMultiplier = 1, bool aSkipEraseAnimation = false)
    {
        WriteString(aString, out _, out _, aWriteSpeedMultiplier, aEraseSpeedMultiplier, aSkipEraseAnimation);
    }

    public void WriteString(string aString, out CoroutineHandle aWriterHandle, out CoroutineHandle aEraserHandle, float aWriteSpeedMultiplier = 1, float aEraseSpeedMultiplier = 1, bool aSkipEraseAnimation = false)
    {
        (m_textWriter, m_textEraser) = (m_textEraser, m_textWriter); //swap
        aEraserHandle = m_textEraser.EraseText(aEraseSpeedMultiplier);
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
        bool writerWorking = m_textWriter.WritingText();
        bool eraserWorking = m_textEraser.WritingText();
        bool ret = writerWorking || eraserWorking;
        //if (ret) Debug.Log("DOUBLE WRITER: ERASER (" + eraserWorking + ") - WRITER(" + writerWorking + ")");
        return ret;
    }

    public CoroutineHandle WaitUntilTextFinishes(GfgInputType aSubmitInput = GfgInputType.NONE, GfgInputType aSkipInput = GfgInputType.RUN, float aFinishSpeedMultiplier = 1, bool aForceWriteOnSubmit = false)
    {
        return WaitUntilTextFinishes(new GfgInputTracker(aSubmitInput), new GfgInputTracker(aSkipInput), aFinishSpeedMultiplier, aForceWriteOnSubmit);
    }

    public CoroutineHandle WaitUntilTextFinishes(GfgInputTracker aSubmitInputTracker, GfgInputTracker aSkipInputTracker, float aFinishSpeedMultiplier = 1, bool aForceWriteOnSubmit = false)
    {
        return Timing.RunCoroutine(_WaitUntilTextFinishesInternal(aSubmitInputTracker, null, aSkipInputTracker, aFinishSpeedMultiplier, aForceWriteOnSubmit));
    }

    public CoroutineHandle WaitUntilTextFinishes(GfgInputTrackerShared aSubmitInputTracker, GfgInputTracker aSkipInputTracker, float aFinishSpeedMultiplier = 1, bool aForceWriteOnSubmit = false)
    {
        return Timing.RunCoroutine(_WaitUntilTextFinishesInternal(default, aSubmitInputTracker, aSkipInputTracker, aFinishSpeedMultiplier, aForceWriteOnSubmit));
    }

    protected IEnumerator<float> _WaitUntilTextFinishesInternal(GfgInputTracker aFallbackInputTracker, GfgInputTrackerShared aSubmitInputTrackerHeap, GfgInputTracker aSkipInputTracker, float aFinishSpeedMultiplier, bool aForceWriteOnSubmit)
    {
        bool finishTranslationPressed = false;

        while (WritingText())
        {
            if (aSkipInputTracker.Pressed())
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
