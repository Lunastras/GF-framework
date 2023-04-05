

/*
 * This script takes care of basic platform movement from one point to another. Various options are offered for the user 
 * to do anything they want with the script. 
 * 
 * IMPORTANT The platform MUST have a child named "_MovementPositions" with various transforms as children. These will be the points
 * the script will use to move the platform.
 * 
 * @author Radu Bucurescu
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformMovement : MonoBehaviour
{
    public float waitTimeOnArrival = 0.1f;
    public bool startsOnInitialisation = true;
    public bool loops = true;
    public bool returnsToFirstPosition = false;
    public float speed = 10;
    public float smoothingDistance = 2f; //the range of the smoothing between points

    private List<Vector3> positions;
    public bool canMove;
    private int currentDestination;
    private bool isGoingBackWards = false;
    private float currentSpeed;
    public bool isWaiting = false;
    private bool arrivedAtDest = false;

    private Rigidbody m_rb;

    // Start is called before the first frame update
    void Awake()
    {
        positions = new List<Vector3>();
        Transform positionsParent = transform.Find("_MovementPositions");

        if (positionsParent != null)
        {
            foreach (Transform child in positionsParent)
            {
                positions.Add(child.position);
            }

            Destroy(positionsParent.gameObject);
        }

        m_rb = GetComponent<Rigidbody>();
        if (null == m_rb)
        {
            m_rb = gameObject.AddComponent<Rigidbody>();
        }

        m_rb.isKinematic = true;
        m_rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Start()
    {
        canMove = startsOnInitialisation;

        if (positions.Count > 1)
        {
            currentDestination = 1;
            transform.position = positions[0];
        }
        else
        {
            Destroy(this);
        }


    }


    public void StartMoving() { canMove = true; }

    public void StopMoving() { canMove = false; }


    private bool CanRun() { return canMove; }

    private float GetSpeed(float dist)
    {
        return speed;
    }

    void FixedUpdate()
    {
        // Debug.Log("uhuh ");

        if (!canMove || isWaiting) return;

        // Debug.Log("akaka I am getting here ");

        if (arrivedAtDest)
        {
            arrivedAtDest = false;

            if (isGoingBackWards)
            {
                currentDestination--;

                if (currentDestination < 0)
                {
                    isGoingBackWards = false;
                    currentDestination = 1;
                }
            }
            else //is not going backwards
            {
                currentDestination++;
                if (currentDestination >= positions.Count)
                {
                    if (returnsToFirstPosition)
                    {
                        currentDestination = 0;
                    }
                    else //will go backwards
                    {
                        currentDestination--;
                        isGoingBackWards = true;
                    }
                }
            }
        }

        //The vector between the current position to the destination
        Vector3 vecBetweenPosAndDest = positions[currentDestination] - transform.position;
        float distanceFromDestination = vecBetweenPosAndDest.magnitude;

        currentSpeed = GetSpeed(distanceFromDestination) * Time.deltaTime;

        if (currentSpeed > distanceFromDestination)
        {
            m_rb.MovePosition(positions[currentDestination]);

            // transform.position = positions[currentDestination];
            StartCoroutine(ArrivedAtDestination());
        }
        else
        {

            m_rb.MovePosition(transform.position + currentSpeed * vecBetweenPosAndDest.normalized);
            // transform.position += currentSpeed * vecBetweenPosAndDest.normalized;
            // Debug.Log("Movement of platform is : " + currentSpeed * vecBetweenPosAndDest.normalized);
            // Debug.Log("akaka I am getting here " + (currentSpeed * vecBetweenPosAndDest.normalized));

        }
    }

    private IEnumerator ArrivedAtDestination()
    {
        isWaiting = true;
        yield return new WaitForSeconds(waitTimeOnArrival);
        arrivedAtDest = true;
        isWaiting = false;
    }
    private IEnumerator PlatformMovementRoutine()
    {
        yield return new WaitUntil(() => canMove);

        while (this != null)
        {

            //The vector between the current position to the destination
            Vector3 vecBetweenPosAndDest = positions[currentDestination] - transform.position;
            float distanceFromDestination = vecBetweenPosAndDest.magnitude;

            currentSpeed = GetSpeed(distanceFromDestination) * Time.deltaTime;

            if (currentSpeed > distanceFromDestination)
            {
                yield return new WaitForSeconds(waitTimeOnArrival);

                if (isGoingBackWards)
                {
                    currentDestination--;

                    if (currentDestination < 0)
                    {
                        isGoingBackWards = false;
                        currentDestination = 1;
                    }

                }
                else //is not going backwards
                {
                    currentDestination++;
                    if (currentDestination >= positions.Count)
                    {
                        if (returnsToFirstPosition)
                        {
                            currentDestination = 0;
                        }
                        else //will go backwards
                        {
                            currentDestination--;
                            isGoingBackWards = true;
                        }
                    }
                }
            }
            yield return new WaitForFixedUpdate();
        }
    }
}
