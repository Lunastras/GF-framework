public class GfxTransitions : GfcSingletonContainer<GfxTransitionGeneric, GfxTransitionType>
{
    public static GfxTransitions Instance { get { return GetInstance<GfxTransitions>(); } }
}

public enum GfxTransitionType
{
    BLACK_FADE,
}
