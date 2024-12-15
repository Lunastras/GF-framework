using MEC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GfgRbLimitSpeed))]
public class IrisuBlock : MonoBehaviour
{
    public AnimationCurve m_scaleCurve;
    private bool m_isPlayerCube = false;
    private IrisuBlockState m_state = IrisuBlockState.PHANTOM;
    private IrisuColorType m_colorType;
    private Material m_material;
    private int m_playerCubesHitCountInHyper = 0;
    private Rigidbody2D m_rb;
    private Transform m_transform;

    const float FLOOR_SET_STATIC_SECONDS = 2;
    const float COL_COEF_STATIC = 0.6f;
    const float COL_COEF_PHANTOM = 0.7f;
    const float COL_COEF_HYPER = 1.5f;

    const float WEIGHT_PLAYER_CUBE = 7;
    const float WEIGHT_PHANTOM_BLOCK = 10;

    const int LAYER_SOLID = 18;
    const int LAYER_PHANTOM = 19;
    const int LAYER_FLOOR = 20;
    const int LAYER_WALL = 21;

    const float DEATH_Y = -10;

    const float SCALE_MAX = 4;
    const float SCALE_MIN = 0.5f;

    const float FALL_SPEED_VARIANCE = 0.2f;

    private GfcCoroutineHandle m_setStateDelayedHandle;

    private GfgRbLimitSpeed m_rbSpeedLimit;

    void Awake()
    {
        m_transform = transform;
        m_rb = GetComponent<Rigidbody2D>();
        m_material = GetComponent<Renderer>().material;
        m_rbSpeedLimit = GetComponent<GfgRbLimitSpeed>();
    }

    void OnEnable() { SetIsPlayerCube(false); }

    void FixedUpdate() { if (m_transform.position.y <= DEATH_Y) GfcPooling.Destroy(gameObject); }

    public void SetIsPlayerCube(bool anIsPlayerCube)
    {
        m_isPlayerCube = anIsPlayerCube;

        if (anIsPlayerCube)
        {
            m_rb.linearVelocity = new(0, 10);
            m_colorType = IrisuColorType.WHITE;
            SetState(IrisuBlockState.STATIC, true);
        }
        else
        {
            m_rb.linearVelocity = new(0, 0);
            //select random color
            m_colorType = (IrisuColorType)Random.Range(0, IrisuManagerGame.GetColorsCount());
            SetState(IrisuBlockState.PHANTOM, true);
            float scale = m_scaleCurve.Evaluate(Random.Range(0.0f, 1.0f));
            scale = SCALE_MIN + scale * (SCALE_MAX - SCALE_MIN);
            m_transform.localScale = new Vector3(scale, scale, scale);
            m_transform.rotation = Quaternion.AngleAxis(Random.Range(0.0f, 360.0f), Vector3.forward);
        }
    }

    private float GetFallSpeedPhantom()
    {
        float fallSpeed = IrisuManagerGame.GetFallingSpeed();//todo, scale with difficulty
        return fallSpeed * Random.Range(1 - FALL_SPEED_VARIANCE, 1 + FALL_SPEED_VARIANCE);
    }

    void OnCollision(GameObject aCollision)
    {
        IrisuBlock irisuBlock = aCollision.GetComponent<IrisuBlock>();

        if (irisuBlock)
        {
            if (m_isPlayerCube && irisuBlock.m_state != IrisuBlockState.STATIC)
                GfcPooling.Destroy(gameObject);

            switch (m_state)
            {
                case IrisuBlockState.PHANTOM:
                    if (irisuBlock.m_state != IrisuBlockState.STATIC && irisuBlock.m_colorType == m_colorType)
                        SetState(IrisuBlockState.HYPER);
                    else if (irisuBlock.m_state != IrisuBlockState.HYPER)
                    {
                        if (irisuBlock.m_colorType == m_colorType && irisuBlock.m_state == IrisuBlockState.STATIC)
                            ClearBlock();
                        else
                        {
                            SetState(IrisuBlockState.ACTIVE);

                            if (irisuBlock.m_state == IrisuBlockState.STATIC && !irisuBlock.m_isPlayerCube)
                                SetStaticDelayed();
                        }
                    }

                    break;
                case IrisuBlockState.STATIC:
                    if (irisuBlock.m_colorType == m_colorType && irisuBlock.m_state != IrisuBlockState.STATIC)
                        ClearBlock();

                    break;
                case IrisuBlockState.ACTIVE:
                    if (irisuBlock.m_colorType == m_colorType)
                    {
                        if (irisuBlock.m_state != IrisuBlockState.STATIC)
                            SetState(IrisuBlockState.HYPER);
                        else
                            ClearBlock();
                    }
                    else if (irisuBlock.m_state == IrisuBlockState.STATIC && !irisuBlock.m_isPlayerCube)
                        SetStaticDelayed();

                    break;
                case IrisuBlockState.HYPER:
                    if (irisuBlock.m_isPlayerCube)
                    {
                        m_playerCubesHitCountInHyper++;
                        if (m_playerCubesHitCountInHyper >= 2)
                        {
                            Destroy(gameObject);
                        }
                    }
                    else if (irisuBlock.m_state == IrisuBlockState.STATIC)
                        ClearBlock();

                    break;
            }
        }
        else
        {
            if ((aCollision.layer == LAYER_FLOOR || aCollision.layer == LAYER_WALL) && m_state != IrisuBlockState.STATIC)
                if (m_state == IrisuBlockState.HYPER)
                    ClearBlock();
                else if (aCollision.layer != LAYER_WALL)
                    SetStaticDelayed();
        }
    }

    void OnCollisionEnter2D(Collision2D aCollision2D) { OnCollision(aCollision2D.gameObject); }
    void OnCollisionStay2D(Collision2D aCollision2D) { OnCollision(aCollision2D.gameObject); }

    private void SetStaticDelayed()
    {
        SetStateDelayed(IrisuBlockState.STATIC, FLOOR_SET_STATIC_SECONDS);
    }

    private void SetStateDelayed(IrisuBlockState aState, float aSeconds)
    {
        m_setStateDelayedHandle.RunCoroutineIfNotRunning(_SetStateDelayed(IrisuBlockState.STATIC, FLOOR_SET_STATIC_SECONDS).CancelWith(gameObject));
    }

    private IEnumerator<float> _SetStateDelayed(IrisuBlockState aState, float aSeconds)
    {
        yield return Timing.WaitForSeconds(aSeconds);
        if (aState == IrisuBlockState.STATIC && m_state != IrisuBlockState.STATIC)
            IrisuManagerGame.ShapeWentStatic(this);
        SetState(aState);
    }

    public IrisuBlockState GetState() { return m_state; }

    public void SetState(IrisuBlockState aState, bool aForceSet = false)
    {
        if (aState != m_state || aForceSet)
        {
            m_state = aState;
            float maxFallSpeed = float.MaxValue;
            int layer = LAYER_SOLID;
            float mass = m_isPlayerCube ? WEIGHT_PLAYER_CUBE : 1;
            Color effectiveColor = GetColor(m_colorType);

            if (!m_isPlayerCube)
            {
                switch (aState)
                {
                    case IrisuBlockState.PHANTOM:
                        layer = LAYER_PHANTOM;
                        mass = WEIGHT_PHANTOM_BLOCK;
                        maxFallSpeed = GetFallSpeedPhantom();
                        effectiveColor.r *= COL_COEF_PHANTOM;
                        effectiveColor.g *= COL_COEF_PHANTOM;
                        effectiveColor.b *= COL_COEF_PHANTOM;
                        effectiveColor.a *= COL_COEF_PHANTOM;

                        break;
                    case IrisuBlockState.STATIC:
                        effectiveColor.r *= COL_COEF_STATIC;
                        effectiveColor.g *= COL_COEF_STATIC;
                        effectiveColor.b *= COL_COEF_STATIC;

                        break;
                    case IrisuBlockState.ACTIVE:
                        break;
                    case IrisuBlockState.HYPER:
                        effectiveColor.r *= COL_COEF_HYPER;
                        effectiveColor.g *= COL_COEF_HYPER;
                        effectiveColor.b *= COL_COEF_HYPER;
                        //m_material.SetColor("_EmissiveColor", effectiveColor);
                        //Set emissive
                        break;
                }
            }

            gameObject.layer = layer;
            m_rbSpeedLimit.MaxSpeed = maxFallSpeed;
            m_rb.mass = mass;
            SetColor(effectiveColor);
        }
    }

    private void SetColor(Color aColor)
    {
        m_material.SetColor("_BaseColor", aColor);
    }

    private static Color GetColor(IrisuColorType aColorType)
    {
        return aColorType switch
        {
            IrisuColorType.RED => Color.red,
            IrisuColorType.BLUE => Color.blue,
            IrisuColorType.YELLOW => Color.yellow,
            IrisuColorType.GREEN => Color.green,
            IrisuColorType.MAGENTA => Color.magenta,
            IrisuColorType.CYAN => Color.cyan,
            IrisuColorType.WHITE => Color.white,
            _ => throw new System.NotImplementedException(),
        };
    }

    private void ClearBlock()
    {
        IrisuManagerGame.ClearBlock(this);
    }
}

public enum IrisuBlockState
{
    PHANTOM,
    STATIC,
    ACTIVE,
    HYPER
}

public enum IrisuColorType
{
    RED,
    BLUE,
    YELLOW,
    GREEN,
    MAGENTA,
    CYAN,
    COUNT,
    WHITE,
}