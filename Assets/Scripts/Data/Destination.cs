using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Destination
{
    public Transform TransformDest { get; private set; }
    public bool IsEnemy { get; private set; }
    public bool CanLoseTrackOfTarget { get; private set; }

    private Vector3 vector3Dest;

    private bool destinationIsTransform;

    public bool HasDestination { get; private set; }

    public StatsCharacter TargetStatsCharacter = null;

    public StatsCharacter SelfStatsCharacter = null;

    public Destination(StatsCharacter selfStats, Transform destination = null, bool isEnemy = false, bool canLoseTrackOfTarget = false)
    {
        //Debug.Log("new destination TRANSFORM set!");

        TransformDest = destination;
        IsEnemy = isEnemy;
        CanLoseTrackOfTarget = canLoseTrackOfTarget;
        vector3Dest = destination != null ? destination.position : Vector3.zero;
        destinationIsTransform = true;
        HasDestination = destination != null;
        SelfStatsCharacter = selfStats;

        if (destination && isEnemy)
        {
            TargetStatsCharacter = destination.GetComponent<StatsCharacter>();

            if (TargetStatsCharacter)
                TargetStatsCharacter.NotifyEnemyEngaging(TargetStatsCharacter.NetworkObjectId);
        }
    }

    public Destination(Vector3 position)
    {
        TransformDest = null;
        IsEnemy = false;
        CanLoseTrackOfTarget = false;
        vector3Dest = position;
        destinationIsTransform = false;
        HasDestination = true;
        TargetStatsCharacter = null;
    }

    public void SetDestination(Vector3 position)
    {
        if (TargetStatsCharacter && IsEnemy)
            TargetStatsCharacter.NotifyEnemyDisengaged(SelfStatsCharacter);

        TransformDest = null;
        IsEnemy = false;
        CanLoseTrackOfTarget = false;
        vector3Dest = position;
        destinationIsTransform = false;
        HasDestination = true;
        TargetStatsCharacter = null;
    }

    public void SetDestination(Transform destination = null, bool isEnemy = false, bool canLoseTrackOfTarget = false)
    {
        bool differentTarget = destination != TransformDest;

        if (TargetStatsCharacter && differentTarget && IsEnemy)
            TargetStatsCharacter.NotifyEnemyDisengaged(SelfStatsCharacter);

        TransformDest = destination;
        IsEnemy = isEnemy;
        CanLoseTrackOfTarget = canLoseTrackOfTarget;
        vector3Dest = destination != null ? destination.position : Vector3.zero;
        destinationIsTransform = true;
        HasDestination = destination != null;

        if (destination && differentTarget && IsEnemy)
        {
            TargetStatsCharacter = destination.GetComponent<StatsCharacter>();

            if (TargetStatsCharacter)
                TargetStatsCharacter.NotifyEnemyEngaging(SelfStatsCharacter.NetworkObjectId);
        }
    }

    public void RemoveDestination()
    {
        if (TargetStatsCharacter && IsEnemy)
            TargetStatsCharacter.NotifyEnemyDisengaged(SelfStatsCharacter);

        HasDestination = false;
        TargetStatsCharacter = null;
        TransformDest = null;
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
