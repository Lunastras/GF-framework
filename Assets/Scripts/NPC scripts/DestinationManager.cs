using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestinationManager
{
    //destroyed old destination
    private bool destroyedOldDest = false;
    private Dequeue<Destination> destinationsQueue;

    public DestinationManager()
    {
        destinationsQueue = new Dequeue<Destination>();
    }

    public Destination GetCurrentDestination()
    {
        RemoveDestroyedDestinations();

        if (!destinationsQueue.isEmpty)
        {
            return destinationsQueue.PeekFront();
        }
        else
        {
            return default;
        }
    }

    public void SetDestination(Vector3 targetPosition)
    {
        destinationsQueue.Clear();
        destinationsQueue.EnqueueFront(new Destination(targetPosition));
    }

    public void SetDestination(Transform target, bool targetIsEnemy = false, bool canLoseTrackOfTarget = false)
    {
        destinationsQueue.Clear();
        Destination dest = new Destination(target, targetIsEnemy, canLoseTrackOfTarget);
        if (!dest.WasDestroyed())
        {
            destinationsQueue.EnqueueFront(dest);
        }
    }

    public void QueueDestionation(Vector3 position)
    {
        destinationsQueue.EnqueueBack(new Destination(position));
    }

    public void QueueDestionation(Transform position, bool targetIsEnemy = false, bool canLoseTrackOfTarget = false)
    {
        Destination dest = new Destination(position, targetIsEnemy, canLoseTrackOfTarget);
        if (!dest.WasDestroyed())
        {
            destinationsQueue.EnqueueBack(dest);
        }
    }

    public void GoToNextDestination()
    {
        if (!destinationsQueue.isEmpty)
        {
            destinationsQueue.PopFront();
            RemoveDestroyedDestinations();
        }
    }

    public void SlipDestionation(Vector3 position)
    {
        destinationsQueue.EnqueueFront(new Destination(position));
    }

    public void SlipDestionation(Transform position, bool targetIsEnemy = false, bool canLoseTrackOfTarget = false)
    {
        Destination dest = new Destination(position, targetIsEnemy, canLoseTrackOfTarget);
        if (!dest.WasDestroyed())
        {
            destinationsQueue.EnqueueFront(dest);
        }
    }

    public void EraseDestinations()
    {
        destinationsQueue.Clear();
    }

    //Cleans up destroyed destinations until it finds a valid one.
    private void RemoveDestroyedDestinations()
    {
        destroyedOldDest = !destinationsQueue.isEmpty && destinationsQueue.PeekFront().WasDestroyed();

        while (!destinationsQueue.isEmpty && destinationsQueue.PeekFront().WasDestroyed())
            destinationsQueue.PopFront();
    }

    // Start is called before the first frame update
    public bool IsEmpty()
    {
        RemoveDestroyedDestinations();
        return destinationsQueue.isEmpty;
    }

    public bool DestroyedOldDest()
    {
        if (destroyedOldDest)
        {
            destroyedOldDest = false;
            return true;
        }

        return false;
    }

}
