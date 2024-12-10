using TMPro;
using UnityEngine;

public class GfxFont : MonoBehaviour
{
    private static GfxFont Instance;
    [SerializeField] private EnumSingletons<EnumSingletons<TMP_FontAsset, GfxFontType>, GfxFontFamily> m_fonts;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        this.SetSingleton(ref Instance);

        if (m_fonts.Elements[0].Singleton.Elements[0].Singleton == null)
            Debug.LogError("The default paragraph font cannot be null. Please assign it in the editor for the gameobject " + gameObject.name);

        m_fonts.Validate(GfxFontFamily.COUNT);
        int countElements = m_fonts.Length;
        for (int i = 0; i < countElements; i++)
            m_fonts[i].Validate(GfxFontType.COUNT);
    }

    public static TMP_FontAsset GetFont(GfxFontType aType = GfxFontType.PARAGRAPH, GfxFontFamily aFamily = GfxFontFamily.MAIN)
    {
        Debug.Assert(aType != GfxFontType.COUNT && aFamily != GfxFontFamily.COUNT, "The count types cannot be used");

        var font = Instance.m_fonts[aFamily][aType];
        return font == null && Instance.m_fonts.FirstElementIsFallback ? Instance.m_fonts[GfxFontFamily.MAIN][aType] : font;
    }
}

public enum GfxFontType
{
    PARAGRAPH, TITLE, HEADER, SPECIAL, COUNT
}

public enum GfxFontFamily
{
    MAIN, COUNT
}