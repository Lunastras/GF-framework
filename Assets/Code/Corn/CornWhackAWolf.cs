using MEC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CornWhackAWolf : MonoBehaviour
{
    public static CornWhackAWolf Instance { get { return PrivateInstance; } }
    private static CornWhackAWolf PrivateInstance;

    [SerializeField] private Transform m_playerHammer;
    [SerializeField] private float m_hitChargePoints = 0.1f;
    [SerializeField] private float m_hitChargePointsLostPerSecond = 1f;
    [SerializeField] private GfcInputType m_hitChargeInput = GfcInputType.SUBMIT;

    [SerializeField] private float m_rotationMaxSpeed = 90.0f;
    [SerializeField] private float m_rotationAcc = 30.0f;
    [SerializeField] private int m_countObjects = 4;
    [SerializeField] private float m_circleRadius = 5.0f;
    [SerializeField] private GameObject m_prefabSheep;
    [SerializeField] private GameObject m_prefabWolf;

    [SerializeField] private Image m_hitChargeImage;
    [SerializeField] private Transform m_rotationParent;

    GfcCoroutineHandle m_gameCoroutineHandle;
    public System.Action<CornWhackAWolfGameResult> OnGameEnd;
    private bool m_playAgain = true;
    private GfcInputTracker m_hitChargeTracker;

    private float m_currentRotationSpeed = 0;
    private bool m_coroutineHandleFetched = false;

    private bool ObserversPresent { get { return m_coroutineHandleFetched || OnGameEnd != null; } }

    private Quaternion m_hammerSmoothRef;
    private Vector3 m_scaleSmoothRef;

    void Awake()
    {
        Debug.Assert(m_countObjects > 1, "The object count is too small, it must be at least 2");
        Debug.Assert(m_hitChargeImage);
        Debug.Assert(m_rotationParent);
        Debug.Assert(m_prefabSheep);
        Debug.Assert(m_prefabWolf);

        this.SetSingleton(ref PrivateInstance);
        m_hitChargeTracker = new(m_hitChargeInput)
        {
            DisplayPromptString = new GfcLocalizedString("Charge Hit")
        };
        m_gameCoroutineHandle.RunCoroutineIfNotRunning(_WhackAWolfLoop());
    }

    private IEnumerator<float> _WhackAWolfLoop()
    {
        Debug.Log("Whack a wolf start");

        yield return Timing.WaitForOneFrame;

        int numWins = 0;
        float objectStepRadians = Mathf.PI * 2 / m_countObjects;
        while (m_playAgain || !ObserversPresent) //If there are no observers and the scene was loaded independently, then repeat game endlessly.
        {
            yield return Timing.WaitForSeconds(1.0f);

            GfcPooling.DestroyChildren(m_rotationParent);

            int wolvesLeft = (numWins + 1).Min(m_countObjects - 1);
            for (int i = 0; i < m_countObjects; i++)
            {
                float angleRad = objectStepRadians * i;
                Transform spawnedObject;
                GameObject prefab = m_prefabSheep;

                if (wolvesLeft > 0 && (Random.Range(0F, 1F) >= 0.5 || wolvesLeft + i >= m_countObjects))
                {
                    wolvesLeft--;
                    prefab = m_prefabWolf;
                }

                spawnedObject = GfcPooling.Instantiate(prefab).transform;
                spawnedObject.SetParent(m_rotationParent, false);
                spawnedObject.SetLocalPositionAndRotation(new(angleRad.Cos() * m_circleRadius, 0, angleRad.Sin() * m_circleRadius)
                                                        , Quaternion.AngleAxis(objectStepRadians * Mathf.Rad2Deg, Vector3.up));
            }

            Quaternion desiredHammerRotation;
            float hitPercent = 0;
            m_currentRotationSpeed = 0;
            while (hitPercent < 1)
            {
                yield return Timing.WaitForOneFrame;

                m_currentRotationSpeed += Time.deltaTime * m_rotationAcc;
                m_currentRotationSpeed.MinSelf(m_rotationMaxSpeed);

                hitPercent -= Time.deltaTime * m_hitChargePointsLostPerSecond;
                hitPercent.MaxSelf(0);

                if (m_hitChargeTracker.PressedSinceLastCheck())
                    hitPercent += m_hitChargePoints;

                hitPercent.MinSelf(1);
                m_hitChargeImage.fillAmount = hitPercent;

                m_rotationParent.Rotate(Vector3.up, m_currentRotationSpeed * Time.deltaTime);

                desiredHammerRotation = Quaternion.AngleAxis(hitPercent * 90 - 90, Vector3.right);
                m_playerHammer.localRotation = GfcTools.QuatSmoothDamp(m_playerHammer.localRotation, desiredHammerRotation, ref m_hammerSmoothRef, 0.05f);
            }

            Transform closestGameObject = null;
            float smallestSqrDistance = 0;
            Vector3 position = m_playerHammer.position;
            int closestGameObjectIndex = 0;

            int objectChildIndex = 0;
            foreach (Transform child in m_rotationParent)
            {
                float sqrDistance = (position - child.position).sqrMagnitude;
                if (closestGameObject == null || smallestSqrDistance > sqrDistance)
                {
                    closestGameObjectIndex = objectChildIndex;
                    closestGameObject = child;
                    smallestSqrDistance = sqrDistance;
                }

                objectChildIndex++;
            }

            m_rotationParent.rotation = Quaternion.AngleAxis(90 + closestGameObjectIndex * objectStepRadians * Mathf.Rad2Deg, Vector3.up);
            float elapsedSeconds = 0;

            desiredHammerRotation = Quaternion.AngleAxis(90, Vector3.right);
            while (elapsedSeconds <= 1)
            {
                yield return Timing.WaitForOneFrame;
                elapsedSeconds += Time.deltaTime;
                m_playerHammer.localRotation = GfcTools.QuatSmoothDamp(m_playerHammer.localRotation, desiredHammerRotation, ref m_hammerSmoothRef, 0.05f);
                closestGameObject.localScale = Vector3.SmoothDamp(closestGameObject.localScale, new(3, 0.1f, 3), ref m_scaleSmoothRef, 0.05f);
            }

            CornWhackAWolfGameResult result = CornWhackAWolfGameResult.WIN;
            if (closestGameObject.name == m_prefabWolf.name)
                result = CornWhackAWolfGameResult.LOST;

            if (result == CornWhackAWolfGameResult.WIN)
                numWins++;

            m_playAgain = false;
            OnGameEnd?.Invoke(result);
        }

        Debug.Log("Whack a wolf over");
    }

    public static void RequestOneMoreGame() { Instance.m_playAgain = true; }
    public static CoroutineHandle GetCoroutineHandle()
    {
        Instance.m_coroutineHandleFetched = true;
        return Instance.m_gameCoroutineHandle;
    }
}

public enum CornWhackAWolfGameResult
{
    WIN,
    LOST,
}