using UnityEngine;
using UnityEngine.Rendering;

public abstract class GfgCameraController : MonoBehaviour
{
    public static GfgCameraController Instance { get; protected set; }

    [SerializeField]
    protected Transform m_target = null;

    [SerializeField]
    protected float m_sensitivity = 1;

    [SerializeField]
    protected float m_targetFov = 90;

    protected PriorityValue<float> m_fovMultiplier = new(1);

    protected Camera m_camera = null;

    public Camera Camera { get { return m_camera; } }

    protected Transform m_transform;

    protected float m_fovRefSpeed = 0;

    protected float m_fovSmoothTime = 1f;

    // Start is called before the first frame update
    protected void Awake()
    {
        if (Instance != this) Destroy(Instance);
        Instance = this;

        m_transform = transform;

        m_camera = GetComponent<Camera>();
    }

    public virtual void RevertToDefault() { }

    public abstract void SnapToTarget();

    // Update is called once per frame
    public abstract void Move(float deltaTime);

    public PriorityValue<float> GetFovMultiplier() { return m_fovMultiplier; }

    public void SetFovMultiplier(float multiplier, float fovSmoothTime = 1, uint priority = 0, bool overridePriority = false)
    {
        if (m_fovMultiplier.SetValue(multiplier, priority, overridePriority))
        {
            m_fovSmoothTime = fovSmoothTime;
        }
    }

    public void SetMainTarget(Transform target)
    {
        m_target = target;
    }
}