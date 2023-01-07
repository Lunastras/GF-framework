using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GfMovementTriggerable : MonoBehaviour
{
    private static Dictionary<GameObject, GfMovementTriggerable> m_triggerableObjects = null;
    // Start is called before the first frame update
    protected void Init()
    {
        if (null == m_triggerableObjects)
            m_triggerableObjects = new(17);

        if (!m_triggerableObjects.ContainsKey(gameObject))
            m_triggerableObjects.Add(gameObject, this);
    }

    private void Start()
    {
        Init();
    }

    public static bool InvokeTrigger(GameObject obj, MgCollisionStruct collision, GfMovementGeneric movement)
    {
        GfMovementTriggerable component = obj.GetComponent<GfMovementTriggerable>();
        // if (m_triggerableObjects.TryGetValue(obj, out GfMovementTriggerable component))
        if (component)
        {
            component.MgOnTrigger(collision, movement);
            return true;
        }

        return false;
    }

    protected abstract void MgOnTrigger(MgCollisionStruct collision, GfMovementGeneric movement);
}
