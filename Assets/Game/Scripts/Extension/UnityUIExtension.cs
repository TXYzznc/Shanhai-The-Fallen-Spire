using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Provides small value-type helpers used by reusable UI animation tooling.
/// </summary>
public static class UnityUIExtension
{
    public static void SetAnchoredPositionX(this RectTransform rectTransform, float value)
    {
        Vector2 position = rectTransform.anchoredPosition;
        position.x = value;
        rectTransform.anchoredPosition = position;
    }

    public static void SetAnchoredPositionY(this RectTransform rectTransform, float value)
    {
        Vector2 position = rectTransform.anchoredPosition;
        position.y = value;
        rectTransform.anchoredPosition = position;
    }

    public static void SetAnchoredPosition3DZ(this RectTransform rectTransform, float value)
    {
        Vector3 position = rectTransform.anchoredPosition3D;
        position.z = value;
        rectTransform.anchoredPosition3D = position;
    }

    public static void SetColorAlpha(this Graphic graphic, float value)
    {
        Color color = graphic.color;
        color.a = value;
        graphic.color = color;
    }

    public static void SetFlexibleSize(this LayoutElement layoutElement, Vector2 value)
    {
        layoutElement.flexibleWidth = value.x;
        layoutElement.flexibleHeight = value.y;
    }

    public static Vector2 GetFlexibleSize(this LayoutElement layoutElement)
    {
        return new Vector2(layoutElement.flexibleWidth, layoutElement.flexibleHeight);
    }

    public static void SetMinSize(this LayoutElement layoutElement, Vector2 value)
    {
        layoutElement.minWidth = value.x;
        layoutElement.minHeight = value.y;
    }

    public static Vector2 GetMinSize(this LayoutElement layoutElement)
    {
        return new Vector2(layoutElement.minWidth, layoutElement.minHeight);
    }

    public static void SetPreferredSize(this LayoutElement layoutElement, Vector2 value)
    {
        layoutElement.preferredWidth = value.x;
        layoutElement.preferredHeight = value.y;
    }

    public static Vector2 GetPreferredSize(this LayoutElement layoutElement)
    {
        return new Vector2(layoutElement.preferredWidth, layoutElement.preferredHeight);
    }
}
