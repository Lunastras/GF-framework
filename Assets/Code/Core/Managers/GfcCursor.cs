using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GfcCursor : BaseInput
{
    private Vector2 m_cursorPosition = Vector2.zero;

    [SerializeField] private RectTransform m_cursorTransform;

    [SerializeField] private float m_sensitivity = 100;

    public override Vector2 mousePosition
    {
        get { return this.m_cursorPosition; }
    }

    //placeholder for when we will have custom cursor that can be controlled by mouse and controller
    public static Vector2 MousePosition { get { return Input.mousePosition; } }

    new void Awake()
    {
        base.Awake();
        this.GetComponentIfNull(ref m_cursorTransform);
        Debug.Assert(m_cursorTransform);
    }

    /*
    void Update()
    {
        Vector2 movement = new(GfgInput.GetAxisRaw(GfgInputType.CAMERA_X), GfgInput.GetAxisRaw(GfgInputType.CAMERA_Y));
        m_cursorPosition += m_sensitivity * movement;
        m_cursorTransform.position = m_cursorPosition;
    }*/
}
