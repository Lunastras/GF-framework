using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System;

public class ScalableWindow : MonoBehaviour
{
    [SerializeField]
    private float m_borderSelectionSize = 16f;

    [SerializeField]
    private float m_screenBorderOffset = 8f;

    [SerializeField]
    private float m_minimumWidth = 200;
    [SerializeField]
    private float m_minimumHeight = 200;

    [SerializeField]
    private bool m_canChangeSize = true;

    [SerializeField]
    private bool m_canMove = true;

    [SerializeField]
    private bool m_screenIsBorder = true;


    private RectTransform m_rectTransform;


    private bool m_isDragging = false;
    private bool m_isDraggingRight = false;
    private bool m_isDraggingLeft = false;
    private bool m_isDraggingDown = false;
    private bool m_isDraggingTop = false;

    private float m_boundOffsetVertical = 0;
    private float m_boundOffsetHorizontal = 0;

    private Vector2 m_screenResolution = Vector3.zero;


    private bool m_wasDraggingWhenPressed = false;

    private bool m_isChangingSize = false;

    private List<RaycastResult> m_raycastResults = new(1);

    public Action<bool> OnDragCallback;
    internal enum CursorType
    {
        NORMAL,
        DRAG_HORIZONTAL,
        DRAG_VERTICAL,
        DRAG_DIAGONAL_TOP_NORTH_EAST,
        DRAG_DIAGONAL_TOP_NORTH_WEST
    }

    private CursorType m_cursorType = CursorType.NORMAL;


    // Start is called before the first frame update
    void Start()
    {
        m_rectTransform = GetComponent<RectTransform>();
        Vector2 size = m_rectTransform.sizeDelta;
        size.x = System.MathF.Max(size.x, System.MathF.Min(m_minimumWidth, Screen.width));
        size.y = System.MathF.Max(size.y, System.MathF.Min(m_minimumHeight, Screen.height));
        m_rectTransform.sizeDelta = size;



        m_screenResolution.x = Screen.width;
        m_screenResolution.y = Screen.height;
    }

    private Vector3 m_lastMousePos = Vector3.zero;
    private Vector3 m_initialPosition = Vector3.zero;

    private static void CalculateLength(float newBound, float oldBound, float oppositeBound, float minimumLength, float offset, float sign, float maxValue, ref float position, out float length)
    {
        newBound += offset * sign;
        newBound = System.MathF.Min(newBound * sign, maxValue * sign) * sign; //this weird line actually works to clamp the border, unbelievable

        length = newBound - oppositeBound;
        length *= sign;
        float lengthCorrection = System.MathF.Min(0, length - minimumLength);
        length -= lengthCorrection;
        newBound -= lengthCorrection * sign;
        position += 0.5f * (newBound - oldBound);
    }

    // Update is called once per frame
    void Update()
    {
        if (Screen.width != m_screenResolution.x || Screen.height != m_screenResolution.y)
            OnResolutionChanged();

        if (Cursor.visible && (GfUITools.IsMouseOverUI() || m_isDragging) && (m_canMove || m_canChangeSize))
        {
            bool mouseIsPressed = Input.GetMouseButton(0);

            Vector3 mousePos = Input.mousePosition;

            if (m_screenIsBorder && false)
            {
                float halfScreenBorderOffset = m_screenBorderOffset;
                mousePos.x = System.MathF.Max(halfScreenBorderOffset, System.MathF.Min(Screen.width - halfScreenBorderOffset, mousePos.x + m_boundOffsetHorizontal));
                mousePos.y = System.MathF.Max(halfScreenBorderOffset, System.MathF.Min(Screen.height - halfScreenBorderOffset, mousePos.y + m_boundOffsetVertical));
            }

            Vector3 position = (m_isDragging && !m_isChangingSize) ? m_initialPosition : m_rectTransform.position;
            Rect rect = m_rectTransform.rect;

            float left = position.x + rect.x;
            float right = left + rect.width;

            float bottom = position.y + rect.y;
            float top = bottom + rect.height;

            m_isDragging &= mouseIsPressed;

            if (m_isDragging)
            {
                Vector2 currentSize = m_rectTransform.sizeDelta;
                Vector2 mouseMovement = mousePos - m_lastMousePos;

                float effectiveMinWidth = System.MathF.Min(m_minimumWidth, m_screenResolution.x);
                float effectiveMinHeight = System.MathF.Min(m_minimumHeight, m_screenResolution.y);

                if (m_isDraggingRight)
                    CalculateLength(mousePos.x, right, left, effectiveMinWidth, m_boundOffsetHorizontal, 1, m_screenResolution.x + m_screenBorderOffset, ref position.x, out currentSize.x);
                else if (m_isDraggingLeft)
                    CalculateLength(mousePos.x, left, right, effectiveMinWidth, m_boundOffsetHorizontal, -1, -m_screenBorderOffset, ref position.x, out currentSize.x);

                if (m_isDraggingDown)
                    CalculateLength(mousePos.y, bottom, top, effectiveMinHeight, m_boundOffsetVertical, -1, -m_screenBorderOffset, ref position.y, out currentSize.y);
                else if (m_isDraggingTop)
                    CalculateLength(mousePos.y, top, bottom, effectiveMinHeight, m_boundOffsetVertical, 1, m_screenResolution.y + m_screenBorderOffset, ref position.y, out currentSize.y);

                if (m_isChangingSize)
                {
                    m_rectTransform.sizeDelta = currentSize;
                    m_lastMousePos = mousePos;
                }
                else
                {
                    if (m_screenIsBorder)
                    {
                        mouseMovement.x -= System.MathF.Max(0, right + mouseMovement.x - (m_screenBorderOffset + m_screenResolution.x));
                        mouseMovement.x -= System.MathF.Min(0, left + mouseMovement.x + m_screenBorderOffset);

                        mouseMovement.y -= System.MathF.Max(0, top + mouseMovement.y - (m_screenBorderOffset + m_screenResolution.y));
                        mouseMovement.y -= System.MathF.Min(0, bottom + mouseMovement.y + m_screenBorderOffset);
                    }

                    position.x = mouseMovement.x + m_initialPosition.x;
                    position.y = mouseMovement.y + m_initialPosition.y;
                }

                m_rectTransform.position = position;
                if (OnDragCallback != null) OnDragCallback(m_isChangingSize);
            }
            else
            {
                m_cursorType = CursorType.NORMAL;
                m_isDraggingRight = m_borderSelectionSize > System.MathF.Abs(mousePos.x - right);
                //we make sure m_isDraggingRight is false to avoid having both values be true. Same is applied to m_isDraggingTop
                m_isDraggingLeft = !m_isDraggingRight && m_borderSelectionSize > System.MathF.Abs(mousePos.x - left);
                m_isDraggingDown = m_borderSelectionSize > System.MathF.Abs(mousePos.y - bottom);
                m_isDraggingTop = !m_isDraggingDown && m_borderSelectionSize > System.MathF.Abs(mousePos.y - top);

                bool dragValid = m_isDraggingRight || m_isDraggingLeft || m_isDraggingDown || m_isDraggingTop;
                bool canSizeDrag = m_canChangeSize && dragValid && GfUITools.IsMouseOverUICollision(mousePos, gameObject);
                m_isChangingSize = canSizeDrag && Input.GetMouseButtonDown(0);

                m_lastMousePos = mousePos;
                m_isDragging = m_isChangingSize || (m_canMove && !dragValid && Input.GetMouseButtonDown(0) && GfUITools.IsMouseOverUICollision(mousePos, gameObject));
                m_initialPosition = position;

                if (m_isChangingSize)
                {
                    if (m_isDraggingRight)
                    {
                        m_boundOffsetHorizontal = right - mousePos.x;
                    }
                    else if (m_isDraggingLeft)
                    {
                        m_boundOffsetHorizontal = mousePos.x - left;
                    }

                    if (m_isDraggingDown)
                    {
                        m_boundOffsetVertical = mousePos.y - bottom;
                    }
                    else if (m_isDraggingTop)
                    {
                        m_boundOffsetVertical = top - mousePos.y;
                    }

                    bool verticleDrag = m_isDraggingDown || m_isDraggingTop;
                    bool horizontalDrag = m_isDraggingRight || m_isDraggingLeft;

                    if (horizontalDrag && !verticleDrag)
                        m_cursorType = CursorType.DRAG_HORIZONTAL;
                    else if (verticleDrag && !horizontalDrag)
                        m_cursorType = CursorType.DRAG_VERTICAL;
                    else if ((m_isDraggingLeft && m_isDraggingDown) || (m_isDraggingRight && m_isDraggingTop))
                        m_cursorType = CursorType.DRAG_DIAGONAL_TOP_NORTH_EAST;
                    else // if ((m_isDraggingLeft && m_isDraggingTop) || (m_isDraggingRight && m_isDraggingBot)) 
                        m_cursorType = CursorType.DRAG_DIAGONAL_TOP_NORTH_WEST;
                }

                /*
                switch (m_cursorType)
                {
                    case (CursorType.NORMAL):
                        break;

                    case (CursorType.DRAG_HORIZONTAL):
                        break;

                    case (CursorType.DRAG_VERTICAL):
                        break;

                    case (CursorType.DRAG_DIAGONAL_TOP_NORTH_EAST):
                        break;

                    case (CursorType.DRAG_DIAGONAL_TOP_NORTH_WEST):
                        break;
                }
                */
            }
        }
    }

    private void OnResolutionChanged()
    {
        //resize window
        m_screenResolution.x = Screen.width;
        m_screenResolution.y = Screen.height;

        Vector3 position = (m_isDragging && !m_isChangingSize) ? m_initialPosition : m_rectTransform.position;
        Rect rect = m_rectTransform.rect;

        float left = System.MathF.Max(-m_screenBorderOffset, position.x + rect.x);
        float right = System.MathF.Min(m_screenResolution.x + m_screenBorderOffset, left + rect.width);

        float bottom = System.MathF.Max(-m_screenBorderOffset, position.y + rect.y);
        float top = System.MathF.Min(m_screenResolution.y + m_screenBorderOffset, bottom + rect.height);

        float width = right - left;
        float height = top - bottom;

        m_rectTransform.sizeDelta = new Vector2(width, height);
        m_rectTransform.position = new Vector3(left + width * 0.5f, bottom + height * 0.5f, 0);
    }



    public bool IsDraggingRight() { return m_isDraggingRight; }

    public bool IsDraggingLeft() { return m_isDraggingLeft; }

    public bool IsDraggingTop() { return m_isDraggingTop; }

    public bool IsDraggingDown() { return m_isDraggingDown; }


}

