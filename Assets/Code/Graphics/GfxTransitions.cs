public class GfxTransitions : GfcSingletonContainer<GfcTransitionParent, GfxTransitionType>
{
    public static GfxTransitions Instance { get { return GetInstance<GfxTransitions>(); } }
}

public enum GfxTransitionType
{
    BLACK_FADE,
}
