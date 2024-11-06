using UnityEngine;

//made by Eldoir from the Unity forums
public static class GfcRectTransformExtensions
{
    public static void SetLeft(this RectTransform aRt, float aLeft) { aRt.offsetMin = new Vector2(aLeft, aRt.offsetMin.y); }

    public static void SetRight(this RectTransform aRt, float aRight) { aRt.offsetMax = new Vector2(-aRight, aRt.offsetMax.y); }

    public static void SetTop(this RectTransform aRt, float aTop) { aRt.offsetMax = new Vector2(aRt.offsetMax.x, -aTop); }

    public static void SetBottom(this RectTransform aRt, float aBottom) { aRt.offsetMin = new Vector2(aRt.offsetMin.x, aBottom); }

    public static void SetPosX(this RectTransform aRt, float aPosX) { aRt.anchoredPosition = new Vector2(aPosX, aRt.anchoredPosition.y); }

    public static void SetPosY(this RectTransform aRt, float aPosY) { aRt.anchoredPosition = new Vector2(aRt.anchoredPosition.x, aPosY); }

    public static void SetPos(this RectTransform aRt, float aPosX, float aPosY) { aRt.anchoredPosition = new Vector2(aPosX, aPosY); }

    public static void SetPos(this RectTransform aRt, Vector2 aAnchoredPosition) { aRt.anchoredPosition = aAnchoredPosition; }

    public static void SetSizeDelta(this RectTransform aRt, float aLength, float aHeight) { aRt.sizeDelta = new Vector2(aLength, aHeight); }

    public static void SetSizeDeltaX(this RectTransform aRt, float aLength) { aRt.sizeDelta = new Vector2(aLength, aRt.sizeDelta.y); }

    public static void SetSizeDeltaY(this RectTransform aRt, float aHeight) { aRt.sizeDelta = new Vector2(aRt.sizeDelta.x, aHeight); }

    public static void SetSizeWithCurrentAnchorsX(this RectTransform aRt, float aLength) { aRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, aLength); }

    public static void SetSizeWithCurrentAnchorsY(this RectTransform aRt, float aHeight) { aRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, aHeight); }

    public static void SetOffsets(this RectTransform aRt, float aLeft, float aRight, float aTop, float aBottom)
    {
        aRt.offsetMin = new Vector2(aLeft, aBottom);
        aRt.offsetMax = new Vector2(-aRight, -aTop);
    }

    public static void SetOffsets(this RectTransform aRt, float aLength) { aRt.SetOffsets(aLength, aLength, aLength, aLength); }

    public static void SetOffsets(this RectTransform aRt, float aOffsetX, float aOffsetY) { aRt.SetOffsets(aOffsetX, aOffsetX, aOffsetY, aOffsetY); }

    public static void SetOffsets(this RectTransform aRt, Vector2 aOffset) { aRt.SetOffsets(aOffset.x, aOffset.y); }

    public static void SetSizeWithCurrentAnchors(this RectTransform aRt, Vector2 aSize)
    {
        aRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, aSize.x);
        aRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, aSize.y);
    }

    public static void SetSizeWithCurrentAnchors(this RectTransform aRt, float aLength, float aHeight)
    {
        aRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, aLength);
        aRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, aHeight);
    }
}