using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GfgCursorStandaloneInput : StandaloneInputModule
{
    [SerializeField] private GfcCursor m_cursor;

    protected override void Start()
    {
        Debug.Assert(m_cursor);
        inputOverride = m_cursor;
        base.Start();
    }
}