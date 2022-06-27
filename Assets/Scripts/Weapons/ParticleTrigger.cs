using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;

using System.Runtime.CompilerServices;

public abstract class ParticleTrigger : MonoBehaviour
{
    [SerializeField]
    protected new ParticleSystem particleSystem;

    [SerializeField]
    protected int layermask;

    private static List<ParticleSystem.Particle> particlesList = null;

    private static LinkedList<ParticleTrigger> instances;

    private bool hasCollision = false;

    private static readonly Vector3 destroyPosition = new(10000, -1000000, 100000);

    public static void AddParticleTrigger(ParticleTrigger newParticleTrigger)
    {
        instances.AddFirst(newParticleTrigger);

       ////find particle trigger with the same layermask
       //foreach(ParticleTrigger instance in instances)
       //{
       //    if(newParticleTrigger.layermask == instance.layermask)
       //    {
       //        int colliderCount = instance.
       //        foreach()
       //        break;
       //    }
       //}
    }

    public static void AddSceneToColliders(Scene scene)
    {
        List<GameObject> rootObjects = new();
        scene.GetRootGameObjects(rootObjects);

        foreach(GameObject obj in rootObjects)
        {
            AddEveryColliderInObject(obj.transform);
        }
    }

    public static void AddEveryColliderInObject(Transform obj)
    {
        AddCollider(obj);
        foreach (Transform child in obj.transform)
            AddEveryColliderInObject(child); //go through every child of the object
    }

    public static void AddCollider(Transform objectToAdd)
    {
        if(objectToAdd.GetComponent<Collider>() != null) //make sure it has a collider
            foreach (ParticleTrigger instance in instances)
            {
                instance.AddColliderInstance(objectToAdd);
            }
    }

    private void AddColliderInstance(Transform objectToAdd)
    {
        if (GfPhysics.LayerIsInMask(objectToAdd.gameObject.layer, layermask))
        {
            particleSystem.trigger.AddCollider(objectToAdd);
        }  
    }

    public static void RemoveCollider(Transform objectToRemove)
    {
        foreach (ParticleTrigger instance in instances)
        {
            instance.RemoveColliderInstance(objectToRemove);
        }
    }

    private void RemoveColliderInstance(Transform objectToRemove)
    {
        if (GfPhysics.LayerIsInMask(objectToRemove.gameObject.layer, layermask))
        {
            particleSystem.trigger.RemoveCollider(objectToRemove);        
        }
    }

    static bool initialisedCollisions = true;

    // Start is called before the first frame update
    void Awake()
    {       
        instances = null == instances ? new() : instances;

        particlesList = null == particlesList ? new(128) : particlesList;

        particleSystem = null == particlesList ? particleSystem : GetComponent<ParticleSystem>();

        AddParticleTrigger(this);

        InternalAwake();
    }

    private void Start()
    {
        if(initialisedCollisions)
        {
             int countScenes = SceneManager.sceneCount;
             for(int i = 0; i < countScenes; ++i)
                AddSceneToColliders(SceneManager.GetSceneAt(i));
        }

        InternalStart();
    }

    virtual protected void InternalAwake() { }
    virtual protected void InternalStart() { }


    void OnParticleTrigger()
    {
        if (particlesList == null)
            return;

        hasCollision = true;

        int numEnter = particleSystem.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, particlesList, out var colliderData);
        //Debug.Log("particle collisions: " + numEnter + "list capacity: " + particlesList.Capacity);
        //Debug.Log("called to calculate collisions");

        for (int i = 0; i < numEnter; ++i)
        {
            ParticleSystem.Particle particle = particlesList[i];
            if(colliderData.GetColliderCount(i) > 0)
            {
                GameObject hitObject = colliderData.GetCollider(i, 0).gameObject;
                //Debug.Log("I HIT " + hitObject.name);

 
                CollisionBehaviour(ref particle, hitObject);
                particle.position = destroyPosition;

                particlesList[i] = particle;
            }       
        }

        particleSystem.SetTriggerParticles(ParticleSystemTriggerEventType.Enter, particlesList);
    }

    protected static void DestroyParticle(ref ParticleSystem.Particle particle)
    {
        particle.position = destroyPosition;
    }

    private void CalculateCollision(int index)
    {
        int numEnter = particleSystem.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, particlesList, out var colliderData);
        //Debug.Log("particle collisions: " + numEnter + "list capacity: " + particlesList.Capacity);
       // Debug.Log("called to calculate collisions");

        for (int i = 0; i < numEnter; ++i)
        {
            ParticleSystem.Particle particle = particlesList[i];
            GameObject hitObject = colliderData.GetCollider(i, 0).gameObject;

            //Debug.Log("I HIT " + hitObject.name);

            CollisionBehaviour(ref particle, hitObject);
            particle.position = destroyPosition;

            particlesList[i] = particle;
        }

        particleSystem.SetTriggerParticles(ParticleSystemTriggerEventType.Enter, particlesList);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    abstract protected void CollisionBehaviour(ref ParticleSystem.Particle particle, GameObject hitObject);
}
