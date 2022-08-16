using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MovementGeneric : MonoBehaviour
{
    [SerializeField]
    protected float speed = 7;
    [SerializeField]
    protected bool canFly;

    [SerializeField]
    protected bool canMove = true;
    [SerializeField]
    protected float mass = 35;
    protected Vector3 velocity;
    public Vector3 movementDir { get; protected set; }
    protected float movementDirMagnitude;

    public bool isGrounded { get; protected set; }

    public bool jumpTrigger { get; set; } = false;

    protected bool jumpTriggerReleased = true;

    private CharacterController controller;
    private new CapsuleCollider collider;

    //parent position movement
    protected Transform parentTransform;
    private bool hasToDetachFromParent = false;
    // whether or not the velocity was adjusted to that of the parent upon parenting
    private bool adjustedVelocityToParent;

    private Vector3 parentLastRot, parentRotMov, parentLastPos, parentPosMov;
    private float timeOfLastParentRotUpdate, timeOfLastParentPosUpdate;
    protected bool hasToAdjustVelocityWhenParenting = true;

    public abstract void CalculateMovement(float speedMultiplier = 1.0f);


    protected void Initialize()
    {
        controller = GetComponent<CharacterController>();
        collider = GetComponent<CapsuleCollider>();
    }

    public virtual void SetMovementDir(Vector3 dir)
    {
        if (!canFly)
            dir.y = 0;

        movementDirMagnitude = dir.magnitude;
        movementDir = dir;
    }

    public virtual void SetCanFly(bool canFly)
    {
        this.canFly = canFly;
    }

    public virtual bool CanFly()
    {
        return canFly;
    }

    public virtual void SetMovementSpeed(float speed)
    {
        this.speed = speed;
    }

    public float SlopeLimit()
    {
        return controller.slopeLimit;
    }

    public float Height()
    {
        return collider.height;
    }

    public float Radius()
    {
        return collider.radius;
    }
    public Vector3 GetVelocity()
    {
        return velocity;
    }

    public void SetVelocity(Vector3 velocity)
    {
        this.velocity = velocity;
    }


    public void SetParentTransform(Transform parent)
    {
        if (parent != parentTransform)
        {
            //  Debug.Log("set parent " + parent.name);
            adjustedVelocityToParent = false;
            timeOfLastParentPosUpdate = timeOfLastParentRotUpdate = Time.time;

            parentRotMov = parentPosMov = Vector3.zero;
            parentLastPos = parent.position;
            hasToDetachFromParent = false;
            parentLastRot = parent.rotation.eulerAngles;
            parentTransform = parent;
        }
    }

    private void PrivateDetachFromParent()
    {
        if (parentTransform != null)
        {
            Vector3 parentMovement = parentPosMov + parentRotMov;
            parentMovement.y = Mathf.Max(0, parentMovement.y);
            velocity += parentMovement;
            // Debug.Log(parentMovement + " added to velocity at time frame ");
            parentTransform = null;
        }

        hasToDetachFromParent = false;
    }

    public void DetachFromParent()
    {
        hasToDetachFromParent = true;
    }

    protected CollisionFlags Move(Vector3 movement)
    {
        if (controller.enabled)
            return controller.Move(movement);

        return default;
    }


    protected void CalculateParentMovement()
    {
        if (hasToDetachFromParent && !isGrounded)
        {
            PrivateDetachFromParent();
        }

        //Calculate the movement according to the parent's movement
        if (parentTransform == null) return;

        Vector3 frameParentMovement = Vector3.zero;

        Vector3 frameParentPosMov = Vector3.zero; //raw movement since last position change
        Vector3 currentPos = parentTransform.position;
        float currentTime = Time.time;

        if (parentLastPos != currentPos)
        {
            parentPosMov = currentPos - parentLastPos;

            frameParentPosMov = parentPosMov;

            float timeSinceLastFrame = currentTime - timeOfLastParentPosUpdate;

            if (timeSinceLastFrame < Time.deltaTime * 2.5f)
            {
                parentPosMov = (timeSinceLastFrame >= Time.deltaTime * 0.8f) ? (parentPosMov /= timeSinceLastFrame) : Vector3.zero;
            }

            timeOfLastParentPosUpdate = currentTime;
            parentLastPos = currentPos;
        }

        //Calculate the rotation according to the parent's rotation

        Vector3 frameParentRotMov = Vector3.zero; //raw movement since last position change
        Vector3 currentRot = parentTransform.rotation.eulerAngles;
        if (parentLastRot != currentRot)
        {
            Vector3 parentRotation = (currentRot - parentLastRot);
            float timeSinceLastFrame = currentTime - timeOfLastParentRotUpdate;

            if (timeSinceLastFrame < Time.deltaTime * 2.5f)
            {
                Vector3 rotMovement = Vector3.zero;
                Vector3 dif = transform.position - parentTransform.position;

                if (parentLastRot.x != currentRot.x)
                {
                    Quaternion xRot = Quaternion.AngleAxis(parentRotation.x, Vector3.right);
                    rotMovement += xRot * dif;
                }
                if (parentLastRot.y != currentRot.y)
                {
                    Quaternion yRot = Quaternion.AngleAxis(parentRotation.y, Vector3.up);
                    rotMovement += yRot * dif;
                }
                if (parentLastRot.z != currentRot.z)
                {
                    Quaternion zRot = Quaternion.AngleAxis(parentRotation.z, Vector3.forward);
                    rotMovement += zRot * dif;
                }

                parentRotMov = (parentTransform.position + rotMovement) - transform.position;

                // Debug.DrawRay(transform.position, parentRotMov * 20f, Color.green, 0.1f);

                if (movementDir == Vector3.zero)
                {
                    transform.Rotate(Vector3.up * parentRotation.y);
                }

                frameParentRotMov = parentRotMov;
                parentRotMov = (timeSinceLastFrame >= Time.deltaTime * 0.8f) ? (parentRotMov /= timeSinceLastFrame) : Vector3.zero;
            }

            timeOfLastParentRotUpdate = currentTime;
            parentLastRot = currentRot;
        }

        //adjust the player's velocity to the parent's and calculate the velocity of the parent object 
        frameParentMovement = frameParentRotMov + frameParentPosMov;
        if (frameParentMovement != Vector3.zero)
        {
            if (!adjustedVelocityToParent)
            {
                Vector3 parentVelocity = parentRotMov + parentPosMov;
                adjustedVelocityToParent = !hasToAdjustVelocityWhenParenting;

                if (parentVelocity != Vector3.zero && hasToAdjustVelocityWhenParenting)
                {
                    adjustedVelocityToParent = true;
                    Vector3 velocityNorm = velocity.normalized;
                    float velocityDot = Vector3.Dot(parentVelocity.normalized, velocityNorm);

                    if (velocityDot > 0)
                    {
                        float speedToDecrease = velocityDot * parentVelocity.magnitude;
                        velocity = (velocity.magnitude > speedToDecrease) ? (velocity - velocityNorm * speedToDecrease) : Vector3.zero;
                    }
                }
            }

            controller.Move(frameParentMovement);
        }


    }
}
