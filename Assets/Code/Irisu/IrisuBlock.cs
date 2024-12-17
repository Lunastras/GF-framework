using MEC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GfgRbLimitSpeed))]
public class IrisuBlock : MonoBehaviour
{
    [SerializeField] private bool m_rainbowClearOrb = false;
    public bool PrintCollisionMessages;
    public AnimationCurve m_scaleCurve;
    private bool m_isPlayerCube = false;
    public IrisuBlockState m_state = IrisuBlockState.PHANTOM;
    private IrisuColorType m_colorType;
    private Material m_material;
    private int m_playerCubesHitCountInHyper = 0;
    private GfcRigidbody m_rb;
    private Transform m_transform;

    const float FLOOR_SET_STATIC_SECONDS = 3;
    const float COL_COEF_STATIC = 0.6f;
    const float COL_COEF_PHANTOM = 0.7f;
    const float COL_COEF_HYPER = 1.5f;

    const float PLAYER_CUBE_SPAWN_YSPEED = 10;
    const float WEIGHT_PLAYER_CUBE = 0.7f;
    const float WEIGHT_SOLID_BLOCK = 0.1f;
    const float WEIGHT_PHANTOM_BLOCK = 0.5f;

    const float MAX_FALL_SPEED_SOLID_BLOCK = 6;

    const int LAYER_SOLID = 18;
    const int LAYER_PHANTOM = 19;
    const int LAYER_FLOOR = 20;
    const int LAYER_WALL = 21;
    const int LAYER_CEIL = 22;

    const float DEATH_Y = -10;
    const float SCALE_MAX = 2.0f;
    const float SCALE_MIN = 0.75f;

    const float FALL_SPEED_VARIANCE = 0.2f;

    private GfcCoroutineHandle m_setStateDelayedHandle;
    private GfgRbLimitSpeed m_rbSpeedLimit;
    private SpriteRenderer m_spriteRenderer;

    private ColorHsv m_colorGradient;

    void Awake()
    {
        m_transform = transform;
        m_rb = GetComponent<GfcRigidbody>();
        m_material = GetComponent<Renderer>().material;
        m_rbSpeedLimit = GetComponent<GfgRbLimitSpeed>();
        m_spriteRenderer = GetComponent<SpriteRenderer>();
        m_colorGradient = new(Color.red);
    }

    void OnEnable()
    {
        m_setStateDelayedHandle.Finished();
        SetIsPlayerCube(false);
    }

    void FixedUpdate()
    {
        if (m_transform.position.y <= DEATH_Y)
            GfcPooling.Destroy(gameObject);

        if (m_colorType == IrisuColorType.RAINBOW)
        {
            m_colorGradient.Hue += Time.deltaTime;
            m_colorGradient.Value = 3;
            SetColor(m_colorGradient);
        }
    }

    public void SetIsPlayerCube(bool anIsPlayerCube)
    {
        m_isPlayerCube = anIsPlayerCube;

        if (anIsPlayerCube)
        {
            m_rb.linearVelocity = new(0, PLAYER_CUBE_SPAWN_YSPEED, 0);
            m_colorType = IrisuColorType.WHITE;
            SetState(IrisuBlockState.STATIC, true);
        }
        else
        {
            m_rb.linearVelocity = new(0, 0, 0);
            m_rb.angularVelocity = new(0, 0, 0);

            //select random color
            m_colorType = m_rainbowClearOrb ? IrisuColorType.RAINBOW : (IrisuColorType)Random.Range(0, IrisuManagerGame.GetColorsCount());
            SetState(IrisuBlockState.PHANTOM, true);

            if (!m_rainbowClearOrb)
            {
                float scale = m_scaleCurve.Evaluate(Random.Range(0.0f, 1.0f));
                scale = SCALE_MIN + scale * (SCALE_MAX - SCALE_MIN);
                m_transform.localScale = new Vector3(scale, scale, scale);
                m_transform.rotation = Quaternion.AngleAxis(Random.Range(0.0f, 360.0f), Vector3.forward);
            }
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

        if (PrintCollisionMessages) Debug.Log("Collided with " + aCollision.name);

        if (irisuBlock)
        {
            if (m_isPlayerCube && irisuBlock.m_state != IrisuBlockState.STATIC)
                GfcPooling.Destroy(gameObject);
            else if (m_colorType == IrisuColorType.RAINBOW)
            {
                IrisuManagerGame.ClearAllBlocksOfColor(irisuBlock.m_colorType);
                GfcPooling.Destroy(gameObject);
            }
            else
            {
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
                        {
                            if (irisuBlock.m_state == IrisuBlockState.HYPER)
                                m_state = IrisuBlockState.HYPER;
                            ClearBlock();
                        }

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
                            if (m_playerCubesHitCountInHyper >= 3)
                            {
                                GfcPooling.Destroy(gameObject);
                            }
                        }
                        else if (irisuBlock.m_state == IrisuBlockState.STATIC)
                            ClearBlock();

                        break;
                }
            }
        }
        else
        {
            if (aCollision.layer == LAYER_CEIL)
                m_rb.MovePosition(new Vector3(0, -0.1f, 0));
            else if (aCollision.layer == LAYER_FLOOR && m_state != IrisuBlockState.STATIC)
                if (m_state == IrisuBlockState.HYPER)
                    ClearBlock();
                else if (m_state == IrisuBlockState.PHANTOM)
                    SetState(IrisuBlockState.ACTIVE);
                else
                    SetStaticDelayed();
        }
    }

    void OnCollisionEnter2D(Collision2D aCollision2D) { OnCollision(aCollision2D.gameObject); }
    void OnCollisionStay2D(Collision2D aCollision2D) { OnCollision(aCollision2D.gameObject); }

    private void SetStaticDelayed()
    {
        if (PrintCollisionMessages) Debug.Log("TURNING STATIC");
        SetStateDelayed(IrisuBlockState.STATIC, FLOOR_SET_STATIC_SECONDS);
    }

    private void SetStateDelayed(IrisuBlockState aState, float aSeconds)
    {
        m_setStateDelayedHandle.RunCoroutineIfNotRunning(_SetStateDelayed(IrisuBlockState.STATIC, FLOOR_SET_STATIC_SECONDS).CancelWith(gameObject));
    }

    private IEnumerator<float> _SetStateDelayed(IrisuBlockState aState, float aSeconds)
    {
        yield return Timing.WaitForSeconds(aSeconds);
        if (aState != IrisuBlockState.STATIC || m_state != IrisuBlockState.HYPER)
            SetState(aState);
        m_setStateDelayedHandle.Finished();
    }

    public IrisuBlockState GetState() { return m_state; }
    public IrisuColorType GetColor() { return m_colorType; }

    public void SetState(IrisuBlockState aState, bool aForceSet = false, bool aShapeWentStaticCall = true)
    {
        if (aState != m_state || aForceSet)
        {
            int layer = LAYER_SOLID;
            float maxFallSpeed = MAX_FALL_SPEED_SOLID_BLOCK;
            float mass = m_isPlayerCube ? WEIGHT_PLAYER_CUBE : WEIGHT_SOLID_BLOCK;
            Color effectiveColor = GetColor(m_colorType);

            if (!m_isPlayerCube)
            {
                if (aShapeWentStaticCall && aState == IrisuBlockState.STATIC && m_state != IrisuBlockState.STATIC)
                    IrisuManagerGame.ShapeWentStatic(this);

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
                        break;
                }
            }

            m_rb.mass = mass;
            m_state = aState;

            gameObject.layer = layer;
            m_rbSpeedLimit.MaxSpeed = maxFallSpeed;
            SetColor(effectiveColor);
        }
    }

    private void SetColor(Color aColor)
    {
        m_material.SetColor("_BaseColor", aColor);
        if (m_spriteRenderer) m_spriteRenderer.color = aColor;
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
            IrisuColorType.RAINBOW => Color.white,
            _ => throw new System.NotImplementedException(),
        };
    }

    private void ClearBlock()
    {
        IrisuManagerGame.ClearBlock(this);
        m_setStateDelayedHandle.Finished();
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
    RAINBOW,
}