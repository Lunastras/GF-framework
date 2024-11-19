using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GfxScrollbar : Scrollbar
{
    public bool IsDragged { get; private set; }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        IsDragged = true;
        base.OnBeginDrag(eventData);
    }
}
