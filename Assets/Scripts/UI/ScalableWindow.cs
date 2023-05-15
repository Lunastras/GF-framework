using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class ScalableWindow : MonoBehaviour
{
    [SerializeField]
    private float m_borderSelectionSize = 8f;

    [SerializeField]
    private float m_minimumWidth = 20;
    [SerializeField]
    private float m_minimumHeight = 20;

    [SerializeField]
    private bool m_canChangeSize = true;

    [SerializeField]
    private bool m_canMove = true;


    private RectTransform m_rectTransform;
    PointerEventData m_pointerEventData;

    private bool m_isDragging = false;
    private bool m_isDraggingRight = false;
    private bool m_isDraggingLeft = false;
    private bool m_isDraggingBot = false;
    private bool m_isDraggingTop = false;

    private bool m_wasDraggingWhenPressed = false;

    private bool m_isChangingSize = false;

    private List<RaycastResult> m_raycastResults = new(1);

    internal enum CursorType
    {
        NORMAL,
        DRAG_HORIZONTAL,
        DRAG_VERTICAL,
        DRAG_DIAGONAL_TOP_NORTH_EAST,
        DRAG_DIAGONAL_TOP_NORTH_WEST
    }

    // Start is called before the first frame update
    void Start()
    {
        m_rectTransform = GetComponent<RectTransform>();
        Vector2 size = m_rectTransform.sizeDelta;
        size.x = System.MathF.Max(size.x, m_minimumWidth);
        size.y = System.MathF.Max(size.y, m_minimumHeight);
        m_rectTransform.sizeDelta = size;

        m_pointerEventData = new PointerEventData(EventSystem.current);
    }



    private Vector3 m_lastMousePos = Vector3.zero;

    // Update is called once per frame
    void Update()
    {
        if (Cursor.visible && (IsMouseOverUI() || m_isDragging) && (m_canMove || m_canChangeSize))
        {
            bool mouseIsPressed = Input.GetMouseButton(0);

            Vector3 mousePos = Input.mousePosition;
            Vector3 position = m_rectTransform.position;
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

                if (m_isDraggingRight || m_isDraggingLeft)
                {
                    if (m_isDraggingLeft) mouseMovement.x *= -1;
                    float desiredWidth = currentSize.x + mouseMovement.x;

                    //if width is too small
                    if (desiredWidth < m_minimumWidth)
                    {
                        mouseMovement.x = m_minimumWidth - currentSize.x;
                        desiredWidth = m_minimumWidth;
                    }

                    currentSize.x = desiredWidth;
                    mouseMovement.x *= 0.5f;
                    if (m_isDraggingRight)
                        position.x += mouseMovement.x;
                    else //m_isDraggingLeft
                        position.x -= mouseMovement.x;
                }

                if (m_isDraggingTop || m_isDraggingBot)
                {
                    if (m_isDraggingBot) mouseMovement.y *= -1;
                    float desiredHeight = currentSize.y + mouseMovement.y;

                    //if width is too small
                    if (desiredHeight < m_minimumHeight)
                    {
                        mouseMovement.y = m_minimumHeight - currentSize.y;
                        desiredHeight = m_minimumHeight;
                    }

                    currentSize.y = desiredHeight;
                    mouseMovement.y *= 0.5f;
                    if (m_isDraggingTop)
                        position.y += mouseMovement.y;
                    else //m_isDraggingLeft
                        position.y -= mouseMovement.y;
                }

                if (m_isChangingSize)
                {
                    m_rectTransform.sizeDelta = currentSize;
                }
                else
                {
                    position.x += mouseMovement.x;
                    position.y += mouseMovement.y;
                }

                m_rectTransform.position = position;
                m_lastMousePos = mousePos;
            }
            else
            {
                m_isDraggingRight = m_borderSelectionSize > System.MathF.Abs(mousePos.x - right);
                //we make sure m_isDraggingRight is false to avoid having both values be true. Same is applied to m_isDraggingTop
                m_isDraggingLeft = !m_isDraggingRight && m_borderSelectionSize > System.MathF.Abs(mousePos.x - left);
                m_isDraggingBot = m_borderSelectionSize > System.MathF.Abs(mousePos.y - bottom);
                m_isDraggingTop = !m_isDraggingBot && m_borderSelectionSize > System.MathF.Abs(mousePos.y - top);

                bool dragValid = m_isDraggingRight || m_isDraggingLeft || m_isDraggingBot || m_isDraggingTop;
                bool canSizeDrag = m_canChangeSize && dragValid && MouseOverUICollision(mousePos);
                m_isChangingSize = canSizeDrag && Input.GetMouseButtonDown(0);

                m_lastMousePos = mousePos;
                m_isDragging = m_isChangingSize || (m_canMove && !dragValid && Input.GetMouseButtonDown(0) && MouseOverUICollision(mousePos));
            }

        }
    }


    private bool MouseOverUICollision(Vector3 mousePosition)
    {
        m_pointerEventData.position = mousePosition;
        EventSystem.current.RaycastAll(m_pointerEventData, m_raycastResults);

        int count = m_raycastResults.Count;
        int lowestIndex = -1;
        float lowestDepth = int.MaxValue;

        for (int i = 0; i < count; ++i)
        {
            float depth = m_raycastResults[i].distance;
            if (depth < lowestDepth)
            {
                lowestDepth = depth;
                lowestIndex = i;
            }
        }

        return m_raycastResults.Count > 0 && m_raycastResults[lowestIndex].gameObject == gameObject;
    }

    private bool IsMouseOverUI()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }
}
