using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public struct Destination
{
    public Transform TransformDest { get; private set; }
    public bool IsEnemy { get; private set; }
    public bool CanLoseTrackOfTarget { get; private set; }

    private Vector3 vector3Dest;

    private bool destinationIsTransform;

    public bool HasDestination { get; private set; }

    public Destination(Transform destination = null, bool isEnemy = false, bool canLoseTrackOfTarget = false)
    {
        //Debug.Log("new destination TRANSFORM set!");

        TransformDest = destination;
        IsEnemy = isEnemy;
        CanLoseTrackOfTarget = canLoseTrackOfTarget;
        vector3Dest = destination != null ? destination.position : Vector3.zero;
        destinationIsTransform = true;
        HasDestination = destination != null;
    }

    public Destination(Vector3 position)
    {
        TransformDest = null;
        IsEnemy = false;
        CanLoseTrackOfTarget = false;
        vector3Dest = position;
        destinationIsTransform = false;
        HasDestination = true;
    }

    public void SetDestination(Vector3 position)
    {
        TransformDest = null;
        IsEnemy = false;
        CanLoseTrackOfTarget = false;
        vector3Dest = position;
        destinationIsTransform = false;
        HasDestination = true;
    }

    public void SetDestination(Transform destination = null, bool isEnemy = false, bool canLoseTrackOfTarget = false)
    {
        TransformDest = destination;
        IsEnemy = isEnemy;
        CanLoseTrackOfTarget = canLoseTrackOfTarget;
        vector3Dest = destination != null ? destination.position : Vector3.zero;
        destinationIsTransform = true;
        HasDestination = destination != null;
    }

    public void RemoveDestination()
    {
        HasDestination = false;
    }

    public Vector3 LastKnownPosition()
    {
        return (TransformDest != null && !CanLoseTrackOfTarget) ? TransformDest.position : vector3Dest;
    }

    public Vector3 RealPosition()
    {
        return TransformDest != null ? TransformDest.position : vector3Dest;
    }

    public bool WasDestroyed()
    {
        return destinationIsTransform && (TransformDest == null || !TransformDest.gameObject.activeSelf);
    }

    //updates transform known location
    //useless if the input of the object was a 
    //vector or if canLoseTrackOfTarget = false 
    public void UpdatePosition()
    {
        if (TransformDest != null) vector3Dest = TransformDest.position;
        //Debug.Log("Vector3 dest set to " + vector3Dest);
    }
}
